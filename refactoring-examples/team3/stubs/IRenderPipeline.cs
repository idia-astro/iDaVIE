// IRenderPipeline.cs
// Stub interface — defines the contract between the volume rendering core
// and whichever Unity render pipeline (URP / HDRP) is active at runtime.
//
// The core rendering classes (VolumeMaterialBinder, VolumeCameraDriver, etc.)
// depend on THIS interface only — they never import URP or HDRP namespaces directly.
// Concrete implementations live in separate assemblies:
//   iDaVIE.Rendering.URP  →  UrpRenderPipeline.cs
//   iDaVIE.Rendering.HDRP →  HdrpRenderPipeline.cs
// A NullRenderPipeline (also in this folder) implements it for unit tests.

using UnityEngine;
using UnityEngine.Rendering;

namespace iDaVIE.Rendering
{
    public interface IRenderPipeline
    {
        // -------------------------------------------------------------------------
        // Command buffer injection
        // A command buffer is a list of GPU instructions.
        // These two methods let the renderer add/remove its ray-march work
        // at a specific point in Unity's frame (e.g. just before transparents).
        // -------------------------------------------------------------------------

        /// <summary>Register a command buffer to run every frame at the given camera event.</summary>
        void AddCommandBuffer(Camera camera, CameraEvent evt, CommandBuffer buffer);

        /// <summary>Remove a previously registered command buffer.</summary>
        void RemoveCommandBuffer(Camera camera, CameraEvent evt, CommandBuffer buffer);

        // -------------------------------------------------------------------------
        // Shader keyword control
        // Shader keywords are compile-time switches inside the GPU shader.
        // URP and HDRP expose the depth texture under different keywords,
        // so the active pipeline sets the right one here.
        // -------------------------------------------------------------------------

        /// <summary>Enable or disable a global shader keyword required by this pipeline.</summary>
        void SetPipelineKeyword(string keyword, bool enabled);

        // -------------------------------------------------------------------------
        // Depth texture availability
        // The ray-march shader needs the depth buffer to know where to stop
        // marching (so it doesn't render through solid objects).
        // Whether that texture is exposed depends on pipeline configuration.
        // -------------------------------------------------------------------------

        /// <summary>
        /// True when the pipeline is writing a depth texture accessible
        /// in the shader as _CameraDepthTexture.
        /// </summary>
        bool DepthTextureAvailable { get; }

        // -------------------------------------------------------------------------
        // Lifecycle
        // Called once when the pipeline is set up, and once when it is torn down
        // (e.g. on app quit or when the user switches pipeline in settings).
        // -------------------------------------------------------------------------

        /// <summary>Called once after the pipeline asset is initialised.</summary>
        void Initialise();

        /// <summary>Called on application quit or pipeline switch. Release any held resources.</summary>
        void Dispose();
    }
}
