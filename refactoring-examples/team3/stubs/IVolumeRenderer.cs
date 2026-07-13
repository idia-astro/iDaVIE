// ══════════════════════════════════════════════════════════════════════════════
// IVolumeRenderer.cs
// Sub-team 3 — Volume Rendering Interface Contract
//
// ALIGNMENT WITH SUB-TEAM 5 (Feature Rendering)
// ─────────────────────────────────────────────
// This interface mirrors IFeatureRenderer's architecture:
// - Renderer is a CONSUMER of domain state, never a MUTATOR
// - Event-driven dirty notification (domain → adapter → renderer)
// - No domain types cross the rendering boundary
// - Enables test doubles without GPU context
//
// Both systems follow the same pattern:
//   Domain aggregate (Feature/FeatureSet or VolumeDataSet)
//   ↓ events
//   Adapter (FeatureVisualiser or VolumeRendererBehaviour)
//   ↓ subscription wiring
//   Renderer interface (IFeatureRenderer or IVolumeRenderer)
//   ↓ implementation
//   GPU layer (FeatureSetRenderer or VolumeRenderCoordinator + four classes)
//
// See: docs/team3/integration/WP3_IFeatureRenderer_Interface_Contract.pdf
// ══════════════════════════════════════════════════════════════════════════════

namespace iDaVIE.Rendering
{
    /// <summary>
    /// Rendering boundary for a single volume data set.
    /// Implemented by the coordinator + four domain classes (VolumeMaterialBinder,
    /// VolumeTextureManager, VolumeCameraDriver, FoveatedSamplingPolicy).
    /// Called only by VolumeRendererBehaviour — never by domain objects.
    ///
    /// This interface defines the contract between the pure-C# domain layer
    /// (which owns volume data state) and the GPU rendering layer (which manages
    /// shaders, textures, and frame buffer updates).
    ///
    /// [§ Alignment with Sub-Team 5 IFeatureRenderer contract v1.0 (2026-05-28)]
    /// </summary>
    public interface IVolumeRenderer
    {
        /// <summary>
        /// Loads a volume data set into the renderer.
        /// Called once per session when a FITS file is opened.
        /// Must NOT mutate the domain VolumeDataSet state.
        /// </summary>
        void LoadDataSet(VolumeDataSet dataSet);

        /// <summary>
        /// Marks the texture for a specific cube as needing a GPU upload on the next frame.
        /// Pass -1 to mark all textures dirty (full rebuild).
        /// Subscribed directly to VolumeDataSet texture-change events.
        /// </summary>
        void SetTextureAsDirty(int cubeIndex);

        /// <summary>
        /// Marks camera state (matrices, clip planes) as needing refresh on the next frame.
        /// Subscribed directly to VolumeDataSet or VolumeCameraDriver state-change events.
        /// </summary>
        void SetCameraAsDirty();

        /// <summary>
        /// Marks foveation parameters (sample rates, LOD bias) as needing refresh.
        /// Subscribed to IGazeProvider or FoveatedSamplingPolicy state-change events.
        /// </summary>
        void SetFoveationAsDirty();

        /// <summary>
        /// Applies a colour-map operation to the next frame.
        /// Uses domain-owned ColorMapData struct — no Unity dependency.
        /// The renderer converts to shader-compatible format internally.
        /// </summary>
        void ApplyColorMap(ColorMapData colorMap);
    }
}
