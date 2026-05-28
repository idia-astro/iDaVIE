# Refactor Plan — VolumeDataSetRenderer & FeatureSetRenderer

Per-method violation hotspots plus refactoring justification. Evidence anchored in **T2 Baseline Report v5** (21 May 2026) and **Assignment Specification v FINAL_1**.

Ownership note: `VolumeDataSetRenderer` is **ST3** (Rendering Engine — Team Beta sub-team *kameel-case*). `FeatureSetRenderer` is **ST5** (Feature System and Domain Model — Team Beta sub-team *kebab_case*). The 60% temporal coupling between FeatureSetRenderer ↔ FeatureSetManager and the 21% coupling between VolumeDataSetRenderer ↔ CanvassDesktop make both files cross-sub-team integration risks; the ST3/ST5 boundary will be specified by the ST5 interface contract (`ST5_interface.md`).

---

## 1. Justification for refactoring

### 1.1 Assignment mandate

- **LO4** requires applying SOLID and GRASP "at the class and component level to produce loosely coupled, highly cohesive units of code with clear separation of concerns."
- **§4.2 Mandatory Architectural Constraints**:
  - **§4.2.1** — "No unit of code (class or component) may violate SOLID or GRASP. Violations must be flagged and refactored, or justified as a documented trade-off."
  - **§4.2.3** — "Domain code, i.e. rendering math, FITS parsing, feature analysis etc. must not transitively depend on UnityEngine or SteamVR types."
  - **§4.2.4** — "Every public API boundary between layers/components must be expressed as an interface and covered by at least one test double."
- **§6.1 ST1 deliverables** explicitly name `VolumeDataSetRenderer` as the worked-refactoring example for the god-class pattern: "Annotated before/after UML class diagram for one current god-like class (e.g., VolumeDataSetRenderer)."

Both classes therefore *must* be refactored or each violation justified as a trade-off. Trade-off justification is not credible at their current scale (see §1.2).

### 1.2 Metric evidence (T2 §3, §4, §6.2, §6.3)

| Class | WMC | CBO | LCOM% | RFC | LOC | Assess. | CodeScene | Strongest temporal coupling |
|---|---|---|---|---|---|---|---|---|
| VolumeDataSetRenderer | 142 / lim 40 | 44 / lim 25 | 96% / lim 50% | 0¹ | 1089 | **CRITICAL** | 5.90 Problematic — "Low Cohesion · Bumpy Road · Complex Method · Complex Conditional" | 48% with VolumeDataSet over 103 revisions (T2 §6.3 *strongest finding* — "boundary already broken in practice") |
| FeatureSetRenderer | 90 / lim 40 | 26 / lim 25 | 91% / lim 50% | 2¹ | 497 | **CRITICAL** | 5.83 Problematic — "Bumpy Road · Deep Nesting · Many Conditionals · Complex Method" | 60% with FeatureSetManager over 38 revisions (T2 §6.3 *new hidden dependency* — "interface boundary should be introduced before Sprint 2") |

¹ Understand RFC=0 at DIT=5 is an underestimate for MonoBehaviour subclasses (T2 §3 note 2); real RFC is much higher.

Convergent evidence across **four independent tools** (SonarQube, Understand CK, NDepend, CodeScene) confirms both classes breach every CK target except DIT. They are also the top P1 items implicated in NDepend critical rule **ND1000** ("types too big") and **ND1003** ("methods too big, too complex"); `VolumeDataSetRenderer.ThresholdMax` is a named offender for **ND1901** (non-readonly static field). They sit in the **"Zone of Pain"** of the Abstractness/Instability diagram (Assembly-CSharp I=0.98, A=0.04, T2 §5.4), which makes any further additions cost-multiply.

### 1.3 Maintainability sub-characteristics impacted (T2 §7)

- **Modularity** — CRITICAL: CBO 44/26 over the 25 limit; NDepend ND1400 confirms 119 members mutually depend across CatalogData ↔ VolumeData.
- **Analysability** — HIGH risk: CodeScene flags both files for Complex Method / Complex Conditional; max method cyclomatic complexity in the assembly is 53.
- **Modifiability** — HIGH risk: LCOM ≥ 91% on every "domain" method group ⇒ high change-risk per method.
- **Testability** — HIGH risk: 0% coverage; CBO 44 on `VolumeDataSetRenderer` means ~44 mocked dependencies for a unit test — "effectively untestable without Unity scene." Constraint §4.2.4 cannot be satisfied without splitting these classes.

### 1.4 Strategic drivers (§1.2 Spec)

The Unity 5 → Unity 6 platform migration (new Input System, scriptable render pipelines, package-based architecture) lands directly on both classes: Update-method shader binding, ComputeBuffer ownership, MonoBehaviour lifecycle. Each god-class concern that survives the refactor multiplies the migration surface. The refactor is *preventive maintenance* keyed to migration cost containment.

---

## 2. VolumeDataSetRenderer — per-method hotspots

File: `Assets/Scripts/VolumeData/VolumeDataSetRenderer.cs` (1402 LOC, 40+ public members). Concern groups identified from method clusters; LCOM 96% means these groups share almost no state.

### 2.1 Lifecycle / bootstrap

| Method | Line | Primary violations | Notes |
|---|---|---|---|
| `Start` / `_startFunc` (coroutine) | 353 / 358 | **SRP, DIP, GRASP Low Cohesion** | 185-line coroutine performs: config read (`Config.Instance`), FITS load (`VolumeDataSet.LoadDataFromFitsFile`), `FindObjectOfType<VolumeInputController>`, `GetComponentInChildren<FeatureSetManager>`, downsampling, material instantiation, WCS attribute probing, `MomentMapRenderer` injection via `AddComponent`, full-cube crop, shader warmup. Six unrelated responsibilities. Direct service-locator coupling to four singletons / Unity APIs. |
| `updateStatus` | 560 | SRP (UI in renderer) | Drives `TextMeshProUGUI` + `Slider` — presentation logic on a domain class. |
| `PopulateRestFrequenyList` | 549 | **DIP** (Config.Instance) | Should be injected as `IRestFrequencyCatalogue`. |
| `OnDestroy` | 1390 | OK in isolation; but couples to ComputeBuffer/Texture3D lifetime not owned here. |

### 2.2 Shader-parameter binding (per-frame render loop)

| Method | Line | Primary violations | Notes |
|---|---|---|---|
| `Update` | 1022 | **SRP, OCP, GRASP Low Cohesion, Don't Talk to Strangers** | ~100-line method binds ~25 shader parameters, runs conditional foveation/mask/projection/rest-frequency branches. CodeScene "Complex Method · Complex Conditional" tag points here. `Shader.EnableKeyword("SHADER_AIP")` inline-switches behaviour by `ProjectionMode` — OCP failure (adding a projection mode edits this class). |
| `OnRenderObject` | 1142 | SRP | GPU draw call (`Graphics.DrawProceduralNow`) inside the same class that owns paint state and persistence. Belongs in a dedicated `MaskOverlayRenderer`. |

### 2.3 Coordinate-system maths (domain logic, should be Unity-free)

| Method | Line | Primary violations | Notes |
|---|---|---|---|
| `ConvertWorldPositionToDataCubePosition` | 616 | **SRP, GRASP Information Expert (misplaced)** | Information expert is the `VolumeDataSet` (which owns `XDim/YDim/ZDim/subsetBounds`) plus the WCS frame. |
| `ConvertWorldRotationToDatacubeRotation` | 627 | as above | |
| `GetVoxelPositionDataSpace` (×2) | 740, 751 | DIP (UnityEngine on domain math), Information Expert | `Vector3Int` math depends transitively on UnityEngine — violates **§4.2.3**. |
| `GetVoxelPositionWorldSpace` | 762 | as above | |
| `VolumePositionToLocalPosition`, `LocalPositionToVolumePosition` | 1246, 1254 | as above | These six methods together are the candidate for a Unity-free `VolumeCoordinateService` (Information Expert / Pure Fabrication). |

### 2.4 Cursor / region / video-cursor state

| Method | Line | Primary violations | Notes |
|---|---|---|---|
| `SetCursorPosition` | 639 | **SRP** | Mixes input semantics (cursor position), data sampling (`_dataSet.GetDataValue`), mask lookup (`_maskDataSet.GetMaskValue`), and `CuboidLine` rendering primitive updates. 55 lines, four responsibilities. |
| `SetVideoCursorLocPosition` / `DeactivateVideoCursorLocPosition` | 699 / 730 | SRP, parallel duplication of `SetCursorPosition` shape | Two near-duplicate cursor pipelines maintained side-by-side. |
| `SetRegionPosition`, `SetRegionBounds`, `UpdateRegionBounds`, `ClearRegion`, `ClearMeasure` | 794, 824, 832, 867, 872 | SRP, GRASP Low Cohesion | Region-of-interest state machine; should be a dedicated `RegionSelection` aggregate. |

### 2.5 Mask editing & undo

| Method | Line | Primary violations | Notes |
|---|---|---|---|
| `InitialiseMask` | 1158 | **SRP, Creator misplaced** | Creates `_maskDataSet`, allocates GPU texture, mutates feature set, re-crops. Mixes Creator and Controller roles. |
| `PaintMask` (private) | 1183 | SRP, Don't Talk to Strangers | Reaches deep into `_maskDataSet.RegionCube.width/height/depth`. Should call a tell-don't-ask method on the mask. |
| `PaintCursor` | 1213 | OK shape; but classifies as **paint-controller** logic alien to a renderer. |
| `FinishBrushStroke` | 1230 | SRP | Cross-couples to `_momentMapRenderer.CalculateMomentMaps()` — moment-map recomputation should be event-driven, not pushed by the renderer. |

### 2.6 Feature & selection cross-cuts (ST5 boundary)

| Method | Line | Primary violations | Notes |
|---|---|---|---|
| `SelectFeature(Vector3)` / `SelectFeature(Feature)` | 877 / 886 | SRP, ISP, **§4.2.4 (no interface)** | Direct dependency on `Feature` and `_featureManager` — should be `IFeatureSelector` injected by ST5. |
| `CropToFeature` | 896 | Don't Talk to Strangers | `_featureManager.SelectedFeature.CornerMin/Max` — two-level reach. |
| `TeleportToRegion` | 1011 | SRP (input-controller logic in renderer) | Calls `volumeInputController.Teleport(...)` — locomotion concern. |

### 2.7 Persistence

| Method | Line | Primary violations | Notes |
|---|---|---|---|
| `SaveSubCube` | 1261 | **SRP, DIP** | File-format & CFITSIO concerns on a renderer. Embeds size-limit policy and toast notifications. |
| `SaveMask` | 1290 | **SRP, OCP, DIP** | ~90 lines, three branches (new / save-copy / overwrite); each branch news a CFITSIO file handle, builds a path with `Regex`, calls `_maskDataSet.SaveMask(...)`, raises a `ToastNotification`. Adding a fourth save mode edits this method (OCP). Direct dependency on `FitsReader` static (DIP). |
| `GetMaskSavedFilePath` | 1379 | Trivial; would live on a `MaskRepository`. |

### 2.8 Misc

| Method | Line | Primary violations | Notes |
|---|---|---|---|
| `ShiftColorMap` | 603 | SRP (presentation policy) | |
| `ResetRestFrequency`, `ResetThresholds` | 1121, 1136 | Acceptable; but rest-frequency state is duplicated between Renderer and DataSet (48% temporal coupling). |
| `RegenerateCubes` | 580 | DIP (Unity Texture3D) | Owns texture regeneration that the DataSet already exposes. |

### 2.9 Concrete NDepend findings localised here

- **ND1901** non-readonly static — `VolumeDataSetRenderer.ThresholdMax` (line 99 is the instance field; report cites a static variant on the same class).
- **ND1000 / ND1003 / ND1004** — types too big, methods too complex, methods with too many parameters — both `Update` and `_startFunc` are exemplars.

---

## 3. FeatureSetRenderer — per-method hotspots

File: `Assets/Scripts/FeatureData/FeatureSetRenderer.cs` (616 LOC, ~15 public methods).

### 3.1 Lifecycle & GPU buffer

| Method | Line | Primary violations | Notes |
|---|---|---|---|
| `Awake` | 93 | SRP, **§4.2.3** | News `ComputeBuffer`, `FeatureVertex[]`, `Material.Instantiate`. Domain class owning Unity GPU primitives directly. |
| `Initialize` | 104 | DIP, Don't Talk to Strangers | `FeatureManager = GetComponentInParent<FeatureSetManager>(); VolumeRenderer = FeatureManager.VolumeRenderer;` — service-locator + Demeter chain. |
| `Update` | 113 | **SRP, GRASP Low Cohesion** | Buffer growth, dirty-set diff, vertex marshalling, GPU upload (`_computeBufferVertices.SetData`). Four concerns. |
| `OnRenderObject` | 555 | SRP (rendering on collection manager) | `Graphics.DrawProceduralNow` belongs on a `FeatureRenderer` Pure Fabrication; this class also owns the feature collection. |
| `OnDestroy` | 566 | OK; cleanup ties to ComputeBuffer ownership. |
| `MakeAxisAlignedCube` (static, private) | 571 | Acceptable as a utility; will move with the rendering split. |

### 3.2 Collection management

| Method | Line | Primary violations | Notes |
|---|---|---|---|
| `AddFeature` | 158 | **SRP, GRASP Creator confusion** | Mutates `FeatureList`, constructs `FeatureMenuListItemInfo` (UI DTO), sets `featureToAdd.FeatureSetParent = this` (back-pointer that creates a circular ownership), marks dirty. Three responsibilities; the UI-DTO build is a presentation concern leaking in. |
| `RemoveFeature` | 169 | SRP | Calls `FeatureMenuScrollerDataSource.InitData()` — UI refresh from a domain method (Demeter violation). |
| `ClearFeatures` | 175 | OK shape; same UI-coupling risk. |
| `SetFeatureAsDirty` | 197 | OK; rendering-side concern that will move to the renderer fabrication. |
| `ToggleVisibility`, `SetVisibilityOn`, `SetVisibilityOff` | 184, 220, 234 | SRP, OCP | Triplicate code — single `SetVisibility(bool)` after split. |
| `UpdateColor` | 257 | OK shape; but mutates each `Feature.CubeColor` — Tell-Don't-Ask candidate on `Feature`. |
| `SelectFeature` | 245 | DIP, Demeter | `FeatureManager.SelectedFeature = feature;` reaches across; selection state should be owned by a `FeatureSelection` value. |

### 3.3 Import (the single biggest hotspot)

| Method | Line | Primary violations | Notes |
|---|---|---|---|
| `SpawnFeaturesFromSourceStats` | 265 | **SRP, DIP, Information Expert** | Builds raw-data schema, walks a dictionary of `DataAnalysis.SourceStats` (native-plugin DTO leaking into ST5), creates `Feature` instances, calls `FeatureMenuScrollerDataSource.InitData()`. Hard-codes column names that should live in a schema descriptor. |
| `SpawnFeaturesFromTable` | 285 | **SRP, OCP, GRASP Low Cohesion, §4.2.3 (UnityEngine + native AstTool leak)** | **~260 lines, single method** — direct driver of CodeScene "Complex Method · Complex Conditional · Many Conditionals · Deep Nesting." Concerns: VOTable column lookup; coordinate-type detection (cartesian/velz/freqz/redz — OCP failure: new coord type edits this branch); WCS frame creation via `AstTool.GetAltSpecSet` / `AstTool.Invert` (static native call); per-row parsing with `CultureInfo`; box extraction; degree→radian conversion; `AstTool.Transform3D` invocation; `Feature` construction; in-volume filtering; final reshuffling of parallel arrays (`FeatureNames`, `FeaturePositions`, `BoxMinPositions`, `BoxMaxPositions`). At least 6 SRP axes in one method. |

### 3.4 Cross-class queries & export

| Method | Line | Primary violations | Notes |
|---|---|---|---|
| `FeatureIsWithinVolume(Feature, VolumeDataSetRenderer)` (static) | 548 | **SRP, GRASP Information Expert misplaced, §4.2.3** | Belongs on the volume bounds abstraction; takes a Unity MonoBehaviour as a parameter, anchoring ST5 to ST3's concrete type. *Also has a latent bug — the predicate name is "within" but the body returns true if the centre is **outside** the bounds.* Refactor pass should fix or rename. |
| `SaveAsVoTable` | 612 | SRP, DIP | Delegates to `VoTableSaver.SaveFeatureSetAsVoTable(this, filePath)`. The static helper is fine; the seam to choose a writer (CSV, VOTable, IPAC) is missing — OCP risk when a second format is added. |

### 3.5 Public-field surface

`FeatureSetType FeatureSetType`, `Color FeatureColor`, `Material LineRenderingMaterial`, `bool featureSetVisible`, plus eight `{ get; private set; }` parallel arrays (`FeatureNames`, `FeaturePositions`, `BoxMinPositions`, `BoxMaxPositions`, `RawDataKeys`, `RawDataTypes`, `Flags`, `FileName`) are read by `FeatureSetManager`, `FeatureMenuController`, and `VoTableSaver`. This is the empirical 60%-coupling channel and the **ISP** failure — three clients depend on the entire surface because no interface segments it.

---

## 4. Cross-class evidence of broken boundaries (T2 §6.3)

| File A | File B | Coupling | Revisions | Interpretation |
|---|---|---|---|---|
| FeatureSetManager.cs | FeatureSetRenderer.cs | 60% | 38 | Hidden ST5-internal coupling. Missing `IFeatureCollection`/`IFeatureRenderer` interfaces. |
| Feature.cs | FeatureSetRenderer.cs | 56% | 30 | `Feature.FeatureSetParent = this` back-pointer creates bidirectional coupling. |
| Feature.cs | FeatureSetManager.cs | 54% | 31 | Same root cause. |
| VolumeDataSet.cs | VolumeDataSetRenderer.cs | 48% | 103 | ST3 god-class pair — strongest finding in the codebase. |
| VolumeDataSetRenderer.cs | VolumeInputController.cs | 38% | 97 | ST3 ↔ ST4 leakage (cursor/region/teleport mixed into renderer). |
| CanvassDesktop.cs | VolumeDataSetRenderer.cs | 21% | 95 | ST6 ↔ ST3 leakage (desktop UI binds to the whole renderer). |

Each pair is a missing `interface` boundary required by **§4.2.4**.

---

## 5. Refactoring direction (high-level — full ST5 design in `ST5_refactoring_proposal.md`)

The diagnosis above translates into the following splits. ST5 owns only the FeatureSetRenderer column; the VolumeDataSetRenderer column is documented to inform the ST5/ST3 interface and is not ST5's deliverable.

**VolumeDataSetRenderer → (ST3 work):**

The four named splits below come straight from brief §6.3 ("Split VolumeDataSetRenderer into VolumeMaterialBinder, VolumeTextureManager, VolumeCameraDriver, FoveatedSamplingPolicy"). The remaining ST3 services cover concerns the §6.3 list does not name but the legacy class still owns (cursor/region state, mask brush orchestration, save dispatch, coordinate maths, rest-frequency catalogue); each is justified by the per-method hotspot table in §2 above.

§6.3 mandated splits:

1. `VolumeMaterialBinder` (Pure Fabrication, Unity-light) — owns the per-frame push of threshold / scaling / colour-map / projection / vignette uniforms from `IRenderSettings`.
2. `VolumeTextureManager` (Pure Fabrication) — owns the ray + mask `Material` instances, the volume `Texture3D` re-uploaded on `IRawVoxelAccess.CurrentGeneration` changes, and the `IMaskGpuBuffers` handle. Single owner of every Unity GPU resource the renderer uses.
3. `VolumeCameraDriver` (Pure Fabrication) — owns transform-derived shader state (model matrix, region highlight bounds, mask voxel offsets). The only ST3 collaborator that reads `Transform`.
4. `FoveatedSamplingPolicy` (Pure Fabrication, stateless) — owns the foveation step-budget decision. Pinned to `MaxRayMarchSteps` when foveation is disabled so the shader needs no conditional.

§6.3 mandated Strategy:

5. `IMaskMode` interface + `DisabledMaskMode` / `EnabledMaskMode` / `InvertedMaskMode` / `IsolatedMaskMode` strategies behind `MaskModeRegistry`. The cross-team `MaskMode` enum (shared_interfaces.md §3.1, resolution line 9) stays as the public dispatch key; each enum member maps to one strategy. Adding a fifth mode is a new enum value + a new `IMaskMode` class + a registry entry — no edit to `VolumeDataSetRenderer`. **OCP satisfied for the modes the brief calls out.**

Other ST3 services (concerns §6.3 does not name but the legacy class owns):

6. `VolumeCoordinateService` (Pure Fabrication, Unity-free) — owns all `Convert*` / `GetVoxelPosition*` / `VolumePositionToLocalPosition` maths. Satisfies §4.2.3.
7. `RegionSelection` (aggregate) — region start/end voxels, `UpdateRegionBounds`, cursor/video-cursor state. Consumed by `VolumeCameraDriver` for the highlight uniforms.
8. `MaskEditingService` — `InitialiseMask`, `PaintMask`, `PaintCursor`, `FinishBrushStroke`, undo/redo handoff through ST2's `IMaskMutationService`.
9. `VolumePersistenceService` — `SaveSubCube`, `SaveMask` with a Strategy for new/copy/overwrite modes.
10. `IRestFrequencyCatalogue` — replaces direct `Config.Instance.restFrequenciesGHz` use.
11. `IFeatureSelector` (consumes ST5's interface) — replaces direct `_featureManager` references.

`VolumeDataSetRenderer.cs` itself remains as a thin `MonoBehaviour` (~120 LOC) that composes the four §6.3 splits and the `MaskModeRegistry`. Its only behaviour is the Unity lifecycle (`Awake` / `Update` / `OnRenderObject` / `OnDestroy`) and a per-frame dirty-flag pull.

**FeatureSetRenderer → (ST5 work, this sub-team):**

1. `FeatureCollection` (Unity-free domain aggregate) — owns `List<Feature>`, `Add`, `Remove`, `Clear`, `SetVisibility(bool)`, selection. Implements `IFeatureCollection` (ST5 interface).
2. `FeatureRenderer` (Pure Fabrication, Unity MonoBehaviour) — owns `ComputeBuffer`, `_vertices`, `_dirtyFeatures`, `Update` GPU upload, `OnRenderObject` draw. Subscribes to `FeatureCollection` change events.
3. `FeatureImporter` — Strategy pattern over `ICoordinateSystem` (`CartesianCoord`, `VelocityCoord`, `FrequencyCoord`, `RedshiftCoord`). Decomposes the 260-line `SpawnFeaturesFromTable`. Replaces the static `AstTool` reach with an injected `IWcsTransform`.
4. `FeatureExporter` — Strategy pattern over `IFeatureWriter` (VOTable, future formats). Replaces direct `VoTableSaver` call.
5. `IVolumeBounds` (consumed from ST3 via `ST5_interface.md`) — replaces the static `FeatureIsWithinVolume(... , VolumeDataSetRenderer)` predicate with a method on `Feature` (Information Expert) that takes `IVolumeBounds`. Fix the inverted-predicate bug at the same time.
6. Decouple `FeatureMenuScrollerDataSource` / `FeatureMenuListItemInfo` from collection methods by raising domain events; ST5 menu controllers subscribe.

Each new unit gets a public interface (per §4.2.4) and a test double. After the split, projected CK deltas (per T2 §9, P3 due Day 13):
- ST5: WMC of any single class ≤ 40; LCOM ≤ 50%; FeatureSetManager ↔ FeatureSetRenderer temporal coupling drops because the boundary becomes an interface change-event rather than a shared field surface.
- §4.2.2 (no circular deps) achievable because the `FeatureSetParent` back-pointer is removed.
- §4.2.3 satisfied because `FeatureCollection`, `FeatureImporter`, and `FeatureExporter` no longer reference UnityEngine.

---

## 6. Open assumptions / questions

- `FeatureIsWithinVolume` (FeatureSetRenderer.cs:548) returns true when the centre is *outside* the bounds; the call site at line 502 (`if (!(excludeExternal && FeatureIsWithinVolume(...)))`) reads as if the original author intended "within = inside." Assumption: refactor renames it `IsFeatureOutsideVolume` and verifies all call sites, rather than silently flipping the logic. To be confirmed with ST3.
- `ThresholdMax` is currently a public field (line 99). NDepend ND1901 names a static variant; this plan assumes the static is generated by Unity tooling — to verify against the NDepend project file before claiming ND1901 closed.
- Whether `MomentMapRenderer` belongs in ST3 (Rendering Engine) or in a separate moment-map sub-system is not specified; this plan leaves it with ST3 by default. To confirm at the next Architecture Guild stand-up.
