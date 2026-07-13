/*
 * iDaVIE — Refactoring Proposal
 * Sub-team 3, Rendering Engine, Team Alpha
 * Sprint 2 — Worked Example E1: Render Pipeline Abstraction
 *
 * STUB STATUS: Method bodies are not yet implemented.
 * This file demonstrates the correct URP architecture and documents
 * what each method must do when fully implemented.
 *
 * WHAT THIS REPLACES:
 *
 *   BEFORE (Built-In RP — does not work in URP):
 *   ──────────────────────────────────────────────
 *   void OnRenderObject()                          ← not called by URP
 *   {
 *       _maskMaterialInstance.SetPass(0);
 *       Graphics.DrawProceduralNow(                ← removed in URP RenderGraph
 *           MeshTopology.Points,
 *           _maskDataSet.ExistingMaskBuffer.count);
 *   }
 *
 *   AFTER (URP — this file):
 *   ──────────────────────────────────────────────
 *   UrpRenderPipeline.SubmitProceduralDraw(
 *       _maskMaterialInstance,
 *       _maskDataSet.ExistingMaskBuffer,
 *       _maskDataSet.ExistingMaskBuffer.count,
 *       transform.localToWorldMatrix);
 *   // Executed inside MaskPointCloudPass.Execute() at the correct URP frame point
 */

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VolumeData.Rendering
{
    /// <summary>
    /// Universal Render Pipeline implementation of IRenderPipeline.
    ///
    /// Architecture:
    ///   This MonoBehaviour attaches a ScriptableRendererFeature to the
    ///   active URP renderer asset. The feature injects two ScriptableRenderPasses:
    ///
    ///   1. MaskPointCloudPass  — replaces OnRenderObject + DrawProceduralNow
    ///                            for mask, catalogue, and feature geometry
    ///   2. BlitWatermarkPass   — replaces OnRenderImage + Graphics.Blit
    ///                            for video frame watermark compositing
    ///
    /// Frame execution order (URP):
    ///   BeforeRenderingOpaques
    ///   AfterRenderingOpaques
    ///     └─► MaskPointCloudPass.Execute()  ← injected here
    ///   AfterRenderingTransparents
    ///     └─► BlitWatermarkPass.Execute()   ← injected here
    ///   AfterRendering
    ///
    /// MANDATORY CONSTRAINT (brief Section 4.2 point 3):
    /// "Domain code must not transitively depend on UnityEngine or SteamVR types."
    /// This class IS the URP dependency. Nothing outside this class imports
    /// UnityEngine.Rendering.Universal. The interface keeps callers clean.
    /// </summary>
    public class UrpRenderPipeline : MonoBehaviour, IRenderPipeline
    {
        // ── State ─────────────────────────────────────────────────────

        private MaskPointCloudPass _maskPass;
        private BlitWatermarkPass  _blitPass;
        private bool _initialised = false;

        // ── IRenderPipeline ───────────────────────────────────────────

        /// <inheritdoc/>
        public bool IsReady => _initialised;

        /// <inheritdoc/>
        public string PipelineName => "Universal Render Pipeline";

        /// <inheritdoc/>
        /// <remarks>
        /// STUB — not yet implemented.
        ///
        /// IMPLEMENTATION NOTES:
        /// Queue the draw call into _maskPass's pending draw list.
        /// MaskPointCloudPass.Execute() will consume it via:
        ///
        ///   cmd.DrawProcedural(
        ///       buffer,
        ///       modelMatrix,
        ///       material,
        ///       pass:        0,
        ///       topology:    MeshTopology.Points,
        ///       vertexCount: count);
        ///
        /// CommandBuffer replaces Graphics.DrawProceduralNow.
        /// cmd is obtained from context.commandBuffer inside Execute().
        ///
        /// Note: in URP 17+ (Unity 6) use RenderGraph API instead:
        ///   using (var builder = renderGraph.AddRasterRenderPass(...))
        ///   { builder.SetRenderFunc(...) }
        /// </remarks>
        public void SubmitProceduralDraw(Material material, ComputeBuffer buffer,
            int count, Matrix4x4 modelMatrix)
        {
            // TODO: implement
            // _maskPass.EnqueueDraw(material, buffer, count, modelMatrix);
            throw new System.NotImplementedException(
                "UrpRenderPipeline.SubmitProceduralDraw — stub only. " +
                "Implement using CommandBuffer.DrawProcedural inside MaskPointCloudPass.Execute().");
        }

        /// <inheritdoc/>
        /// <remarks>
        /// STUB — not yet implemented.
        ///
        /// IMPLEMENTATION NOTES:
        /// Queue the blit into _blitPass's pending request.
        /// BlitWatermarkPass.Execute() will consume it via:
        ///
        ///   Blitter.BlitCameraTexture(cmd, source, destination, material, pass: 0);
        ///
        /// Blitter is the URP-provided utility that replaces Graphics.Blit.
        /// It handles y-flip, correct UV space, and SRP compatibility.
        ///
        /// Replaces:
        ///   void OnRenderImage(RenderTexture src, RenderTexture dst) — line 511
        ///   Graphics.Blit(source, destination, _logoMaterial)        — line 513
        /// both in VideoCameraController.cs
        /// </remarks>
        public void SubmitBlit(RenderTexture source, RenderTexture destination,
            Material material)
        {
            // TODO: implement
            // _blitPass.EnqueueBlit(source, destination, material);
            throw new System.NotImplementedException(
                "UrpRenderPipeline.SubmitBlit — stub only. " +
                "Implement using Blitter.BlitCameraTexture inside BlitWatermarkPass.Execute().");
        }

        // ── Unity Lifecycle ───────────────────────────────────────────

        private void Awake()
        {
            // TODO: implement
            // 1. Verify UniversalRenderPipeline.asset is set as the active pipeline
            // 2. Create MaskPointCloudPass and BlitWatermarkPass instances
            // 3. Register both passes with the active ScriptableRendererFeature
            // 4. Set _initialised = true

            Debug.Log($"[{PipelineName}] Awake — stub, not yet initialised.");
        }

        private void OnDestroy()
        {
            // TODO: implement
            // Deregister render passes from the renderer feature
        }

        // ── Inner pass stubs ──────────────────────────────────────────

        /// <summary>
        /// STUB — URP ScriptableRenderPass for mask/catalogue/feature point clouds.
        ///
        /// IMPLEMENTATION NOTES:
        /// Inherits ScriptableRenderPass.
        /// Injected at RenderPassEvent.AfterRenderingOpaques.
        /// Maintains a List of pending draw calls queued via EnqueueDraw().
        /// Execute() drains the list using cmd.DrawProcedural().
        /// </summary>
        private class MaskPointCloudPass : ScriptableRenderPass
        {
            public override void Execute(ScriptableRenderContext context,
                ref RenderingData renderingData)
            {
                // TODO: implement
                // var cmd = CommandBufferPool.Get("MaskPointCloud");
                // foreach (var draw in _pendingDraws)
                //     cmd.DrawProcedural(draw.Buffer, draw.Matrix, draw.Material, 0,
                //         MeshTopology.Points, draw.Count);
                // context.ExecuteCommandBuffer(cmd);
                // CommandBufferPool.Release(cmd);
                // _pendingDraws.Clear();
            }
        }

        /// <summary>
        /// STUB — URP ScriptableRenderPass for fullscreen blit (video watermark).
        ///
        /// IMPLEMENTATION NOTES:
        /// Inherits ScriptableRenderPass.
        /// Injected at RenderPassEvent.AfterRenderingTransparents.
        /// Execute() calls Blitter.BlitCameraTexture() with queued blit request.
        /// </summary>
        private class BlitWatermarkPass : ScriptableRenderPass
        {
            public override void Execute(ScriptableRenderContext context,
                ref RenderingData renderingData)
            {
                // TODO: implement
                // var cmd = CommandBufferPool.Get("BlitWatermark");
                // Blitter.BlitCameraTexture(cmd, _source, _destination, _material, 0);
                // context.ExecuteCommandBuffer(cmd);
                // CommandBufferPool.Release(cmd);
            }
        }
    }
}
