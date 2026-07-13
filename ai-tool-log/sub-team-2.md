# Sub-team 2 — Persistence & Data — AI tool usage log

See [`README.md`](README.md) for schema. Newest entries on top.

---

## 2026-06-02 — Conformance test plan for native plug-ins

- **Author:** Conor Healy
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** `refactoring-examples/data-plugins/tests/ConformancePlan.md`
- **Prompt summary:** Asked Claude to draft a conformance test plan table covering each ABI function, input type, and pass criteria for the three native plug-ins.
- **Where it helped:** Generated the table structure and populated entries for FITS file ops and WCS transforms quickly.
- **Where it failed / was wrong:** Several pass criteria for WCS round-trip tolerance were placeholder values — had to research and replace with values from the Starlink AST documentation.
- **Human reviewer:** Conor Healy

---

## 2026-06-02 — CK metrics analysis for plugin layer (LCOM, DIT, NOC, RFC, CBO)

- **Author:** Conor Healy
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** Verbal/session analysis — figures fed into design document and pitch preparation
- **Prompt summary:** Asked Claude to calculate LCOM HS, DIT, NOC, RFC, and CBO for `FitsReader`, `DataAnalysis`, and `AstTool` in the pre-recast state. Also asked it to explain each metric and the adapter/domain threshold distinction.
- **Where it helped:** Correctly calculated LCOM HS = 1.0 for FitsReader and DataAnalysis; identified RFC = 74 for FitsReader as a threshold failure; confirmed NOC = 0 via grep; explained DIT N/A for static classes.
- **Where it failed / was wrong:** Initially calculated DIT = 0 when NDepend reports 1 (it counts System.Object). Also initially claimed LCOM 1.0 for FitsReader was just an adapter artefact and not a design flaw — had to push back on this, it is a genuine flaw.
- **Human reviewer:** Conor Healy

---

## 2026-06-02 — Worked refactoring examples (FitsReader memory ownership + WCS Unity removal)

- **Author:** Conor Healy
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** `refactoring-examples/data-plugins/worked-examples/fits-memory/before.cs`, `after.cs`; `refactoring-examples/data-plugins/worked-examples/wcs-plugin/before.cs`, `after.cs`
- **Prompt summary:** Asked Claude to produce before/after worked examples for FitsReadSubImageFloat memory ownership and AstTool Unity dependency removal, using actual git history as source material.
- **Where it helped:** Spotted the inverted-condition memory leak bug in VolumeDataSet.cs lines 431–434. Accurately reconstructed the before/after from git commits. AstTool example correctly showed the DLL rename from `idavie_native` to `idavie_ast`.
- **Where it failed / was wrong:** The AstTool before.cs included a `TryTransform(Vector3)` helper that never existed in the original — fabricated as an illustration. Kept as a pattern example but not a direct quote.
- **Human reviewer:** Conor Healy

---

## 2026-06-02 — Plugin design document (honest reflection of actual recast)

- **Author:** Conor Healy
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** `refactoring-examples/data-plugins/plugin-design-document.md`
- **Prompt summary:** Gave Claude our draft design doc (written in future tense) and asked it to rewrite it as a past-tense reflection of what was actually built, grounded in the git commit history.
- **Where it helped:** Rewrote in past tense accurately. Caught the two-pattern inconsistency between `[DllImport]` direct and `NativePluginLoader` attribute approaches. Produced the interaction diagram.
- **Where it failed / was wrong:** An earlier version of this doc (plugin-registry.md) invented `IFitsPlugin`, `IAstPlugin`, `IDataPlugin` interfaces and a `PluginRegistry` class that don't exist anywhere in the codebase. Had to be completely discarded.
- **Human reviewer:** Conor Healy

---

## 2026-06-02 — Architecture document — kernel ABI (superseded)

- **Author:** Conor Healy
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** `refactoring-examples/data-plugins/architecture/plugin-registry.md` (superseded)
- **Prompt summary:** Asked Claude to produce an architecture document describing the kernel ABI and plug-in boundary before we had confirmed what was actually built.
- **Where it helped:** The two-boundary diagram and the GetSourceStats AST entanglement explanation were accurate and carried forward into the final doc.
- **Where it failed / was wrong:** Generated `IFitsPlugin`, `IAstPlugin`, `IDataPlugin` interfaces and a `PluginRegistry` class as if they existed — none of them do. The whole document had to be replaced once we checked the actual code.
- **Human reviewer:** Conor Healy

---

## 2026-05-30 — Property-based test examples for FITS round-trip

- **Author:** Conor Healy
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** `refactoring-examples/data-plugins/tests/FitsRoundTripTests.cs`
- **Prompt summary:** Asked for property-based tests using FsCheck covering FITS read/write round-trips with synthetic header and pixel data.
- **Where it helped:** FsCheck generator scaffolding and test structure were good, saved a lot of setup time.
- **Where it failed / was wrong:** Tests assumed an in-memory FITS buffer, which CFITSIO does not support — rewrote to use temp files on disk.
- **Human reviewer:** Conor Healy

---

## 2026-05-28 — SRP split design for FitsReader

- **Author:** Conor Healy
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** `refactoring-examples/data-plugins/fits-reader/srp-split.md`
- **Prompt summary:** Asked Claude to propose how to split the 59-method FitsReader monolith based on responsibility boundaries in the source.
- **Where it helped:** Identified the five boundaries (file lifecycle, header, image, table, mask) that matched what we implemented. Descriptions were accurate.
- **Human reviewer:** Conor Healy

---

## 2026-05-26 — Isolation strategy for plug-in tests without Unity

- **Author:** Conor Healy
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** `refactoring-examples/data-plugins/tests/PluginIsolationStrategy.md`
- **Prompt summary:** Asked how to load and test the native DLLs in a plain .NET test runner with no Unity installation.
- **Where it helped:** Correct explanation of `SetDllDirectory` and `NativeLibrary.SetDllImportResolver`. Useful starting point for the strategy doc.
- **Where it failed / was wrong:** Boilerplate assumed NUnit — had to replace with the actual test runner we're using.
- **Human reviewer:** Conor Healy

---

## 2026-05-22 — Strategy pattern design for downsampling

- **Author:** Conor Healy
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** `refactoring-examples/data-plugins/data-analysis/patterns.md`
- **Prompt summary:** Asked Claude to refactor the `bool maxDownsampling` parameter in `DataCropAndDownsample` into a Strategy pattern and show the before/after interface design.
- **Where it helped:** `IDownsampleStrategy` interface and the two concrete implementations were clean and used directly.
- **Human reviewer:** Conor Healy

---

## 2026-05-20 — CK metric explanations for baseline

- **Author:** Conor Healy
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** Design document intro section
- **Prompt summary:** Asked Claude to explain each CK metric in plain language to help interpret the NDepend baseline output.
- **Where it helped:** Clear explanations, used almost verbatim in the doc intro.
- **Where it failed / was wrong:** Gave slightly different threshold values to what Section 7.1 of the spec states — cross-checked and corrected.
- **Human reviewer:** Conor Healy

---

## 2026-05-19 — Initial codebase exploration

- **Author:** Conor Healy
- **Tool:** Claude Code (claude-sonnet-4-6)
- **Where used:** Sprint planning notes
- **Prompt summary:** Asked Claude to read the plugin interface directory and summarise what each class does, method counts, and external dependencies.
- **Where it helped:** Quick summary of FitsReader (59 methods, CFITSIO, Unity), DataAnalysis (delegate pattern), AstTool (Starlink AST). Saved manually reading ~800 lines.
- **Human reviewer:** Conor Healy
