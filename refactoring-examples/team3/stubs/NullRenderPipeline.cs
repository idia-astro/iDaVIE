// NullRenderPipeline.cs
// Test double — a do-nothing implementation of IRenderPipeline.
// Inject this in edit-mode unit tests so the rendering core can be tested
// without a running Unity player loop or a real GPU pipeline.

using UnityEngine;
using UnityEngine.Rendering;

namespace iDaVIE.Rendering.Tests
{
    public sealed class NullRenderPipeline : IRenderPipeline
    {
        // Reports depth as always available so tests that branch on this don't need extra setup.
        public bool DepthTextureAvailable => true;

        // All operations are intentional no-ops — there is no real pipeline to talk to.
        public void AddCommandBuffer(Camera camera, CameraEvent evt, CommandBuffer buffer) { }
        public void RemoveCommandBuffer(Camera camera, CameraEvent evt, CommandBuffer buffer) { }
        public void SetPipelineKeyword(string keyword, bool enabled) { }
        public void Initialise() { }
        public void Dispose() { }
    }
}
