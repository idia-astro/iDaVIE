/*
 * iDaVIE — Refactoring Proposal
 * Sub-team 3, Rendering Engine, Team Alpha
 * Sprint 2 — Worked Example E1: Render Pipeline Abstraction
 *
 * STUB STATUS: Method bodies are not yet implemented.
 * This file demonstrates the correct HDRP architecture and documents
 * what each method must do when fully implemented.
 *
 * NOTE ON PRIORITY:
 * iDaVIE's primary Unity 6 migration target is URP (better VR performance,
 * lower overhead, XR support). HDRP is documented here for completeness
 * and future-proofing — it may become relevant for desktop non-VR use cases
 * (higher fidelity rendering for presentation/publication mode).
 *
 * The key architectural point is that UrpRenderPipeline and HdrpRenderPipeline
 * are interchangeable behind IRenderPipeline. Switching pipelines requires
 * only swapping which concrete implementation is registered — no callers change.
 *
 * WHAT THIS REPLACES (same as URP stub — same callsites, different mechanism):
 *
 *   BEFORE (Built-In RP — does not work in HDRP):
 *   ──────────────────────────────────────────────
 *   void OnRenderObject()                    ← not called by HDRP
 *   {
 *       Graphics.DrawProceduralNow(...)      ← not available in HDRP RenderGraph
 *   }
 *
 *   AFTER (HDRP — this file):
 *   ──────────────────────────────────────────────
 *   HdrpRenderPipeline.SubmitProceduralDraw(
 *       material, buffer, count, modelMatrix);
 *   // Executed inside CustomPassVolume at the correct HDRP frame point
 */

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace VolumeData.Rendering
{
    /// <summary>
    /// High Definition Render Pipeline implementation of IRenderPipeline.
    ///
    /// Architecture:
    ///   HDRP uses CustomPass (not ScriptableRenderPass like URP).
    ///   This class registers two CustomPass instances with a CustomPassVolume:
    ///
    ///   1. MaskPointCloudCustomPass  — replaces OnRenderObject + DrawProceduralNow
    ///   2. BlitWatermarkCustomPass   — replaces OnRenderImage + Graphics.Blit
    ///
    /// Key HDRP difference from URP:
    ///   URP:  ScriptableRenderPass → injected via ScriptableRendererFeature
    ///   HDRP: CustomPass           → injected via CustomPassVolume component
    ///
    /// Both result in draw calls executing at the correct point in the frame.
    /// The IRenderPipeline interface hides this difference from all callers.
    ///
    /// Frame execution order (HDRP):
    ///   BeforeRendering
    ///   BeforePreRefraction
    ///   BeforeTransparent
    ///     └─► MaskPointCloudCustomPass.Execute()  ← injected here
    ///   BeforePostProcess
    ///     └─► BlitWatermarkCustomPass.Execute()   ← injected here
    ///   AfterPostProcess
    /// </summary>
    public class HdrpRenderPipeline : MonoBehaviour, IRenderPipeline
    {
        // ── State ─────────────────────────────────────────────────────

        private CustomPassVolume _passVolume;
        private MaskPointCloudCustomPass _maskPass;
        private BlitWatermarkCustomPass  _blitPass;
        private bool _initialised = false;

        // ── IRenderPipeline ───────────────────────────────────────────

        /// <inheritdoc/>
        public bool IsReady => _initialised;

        /// <inheritdoc/>
        public string PipelineName => "High Definition Render Pipeline";

        /// <inheritdoc/>
        /// <remarks>
        /// STUB — not yet implemented.
        ///
        /// IMPLEMENTATION NOTES:
        /// Queue the draw call into _maskPass's pending draw list.
        /// MaskPointCloudCustomPass.Execute() will consume it via:
        ///
        ///   var cmd = ctx.cmd;
        ///   cmd.DrawProcedural(
        ///       buffer,
        ///       modelMatrix,
        ///       material,
        ///       shaderPass: 0,
        ///       topology:   MeshTopology.Points,
        ///       vertexCount: count);
        ///
        /// Note: in HDRP 16+ (Unity 6) use RenderGraph API:
        ///   using (var builder = renderGraph.AddRenderPass(...))
        ///   { builder.SetRenderFunc(...) }
        ///
        /// CommandBuffer is obtained from CustomPassContext.cmd
        /// inside CustomPass.Execute(). Do NOT use CommandBufferPool
        /// in HDRP — the context provides the buffer directly.
        /// </remarks>
        public void SubmitProceduralDraw(Material material, ComputeBuffer buffer,
            int count, Matrix4x4 modelMatrix)
        {
            // TODO: implement
            // _maskPass.EnqueueDraw(material, buffer, count, modelMatrix);
            throw new System.NotImplementedException(
                "HdrpRenderPipeline.SubmitProceduralDraw — stub only. " +
                "Implement using ctx.cmd.DrawProcedural inside MaskPointCloudCustomPass.Execute().");
        }

        /// <inheritdoc/>
        /// <remarks>
        /// STUB — not yet implemented.
        ///
        /// IMPLEMENTATION NOTES:
        /// Queue the blit into _blitPass's pending request.
        /// BlitWatermarkCustomPass.Execute() will consume it via:
        ///
        ///   HDUtils.BlitCameraTexture(ctx.cmd, source, destination, material, pass: 0);
        ///
        /// HDUtils.BlitCameraTexture is the HDRP equivalent of URP's
        /// Blitter.BlitCameraTexture. It handles HDRP's y-flip conventions
        /// and render target state correctly.
        ///
        /// Do NOT use Graphics.Blit inside a CustomPass — it bypasses
        /// HDRP's render target management and causes visual corruption.
        /// </remarks>
        public void SubmitBlit(RenderTexture source, RenderTexture destination,
            Material material)
        {
            // TODO: implement
            // _blitPass.EnqueueBlit(source, destination, material);
            throw new System.NotImplementedException(
                "HdrpRenderPipeline.SubmitBlit — stub only. " +
                "Implement using HDUtils.BlitCameraTexture inside BlitWatermarkCustomPass.Execute().");
        }

        // ── Unity Lifecycle ───────────────────────────────────────────

        private void Awake()
        {
            // TODO: implement
            // 1. Verify HDRenderPipeline is the active pipeline asset
            // 2. Add or retrieve CustomPassVolume on this GameObject
            // 3. Create MaskPointCloudCustomPass and BlitWatermarkCustomPass
            // 4. Register both with _passVolume.customPasses list
            // 5. Set injection points:
            //      _maskPass.injectionPoint = CustomPassInjectionPoint.BeforeTransparent
            //      _blitPass.injectionPoint = CustomPassInjectionPoint.BeforePostProcess
            // 6. Set _initialised = true

            Debug.Log($"[{PipelineName}] Awake — stub, not yet initialised.");
        }

        private void OnDestroy()
        {
            // TODO: implement
            // Remove passes from _passVolume.customPasses
        }

        // ── Inner pass stubs ──────────────────────────────────────────

        /// <summary>
        /// STUB — HDRP CustomPass for mask/catalogue/feature point clouds.
        ///
        /// IMPLEMENTATION NOTES:
        /// Inherits CustomPass.
        /// injectionPoint = CustomPassInjectionPoint.BeforeTransparent
        /// Execute() drains pending draw list using ctx.cmd.DrawProcedural().
        ///
        /// Key HDRP difference: setup() must declare which render targets
        /// are needed. For point cloud drawing, request:
        ///   builder.ReadWriteTexture(ctx.cameraColorBuffer)
        ///   builder.ReadWriteTexture(ctx.cameraDepthBuffer)
        /// </summary>
        private class MaskPointCloudCustomPass : CustomPass
        {
            protected override void Execute(CustomPassContext ctx)
            {
                // TODO: implement
                // foreach (var draw in _pendingDraws)
                //     ctx.cmd.DrawProcedural(draw.Buffer, draw.Matrix, draw.Material,
                //         0, MeshTopology.Points, draw.Count);
                // _pendingDraws.Clear();
            }
        }

        /// <summary>
        /// STUB — HDRP CustomPass for fullscreen blit (video watermark).
        ///
        /// IMPLEMENTATION NOTES:
        /// Inherits CustomPass.
        /// injectionPoint = CustomPassInjectionPoint.BeforePostProcess
        /// Execute() calls HDUtils.BlitCameraTexture() with queued blit.
        ///
        /// HDRP-specific: must use HDUtils, NOT Blitter (which is URP-only).
        /// </summary>
        private class BlitWatermarkCustomPass : CustomPass
        {
            protected override void Execute(CustomPassContext ctx)
            {
                // TODO: implement
                // HDUtils.BlitCameraTexture(ctx.cmd, _source, _destination, _material, 0);
            }
        }
    }
}
