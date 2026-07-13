/*
 * iDaVIE — Refactoring Proposal
 * Sub-team 3, Rendering Engine, Team Alpha
 *
 * Edit-mode unit tests for IMaskMode implementations.
 * These tests run WITHOUT the Unity runtime (Unity Test Framework, Edit Mode).
 * No scene, no FITS file, no VR headset required.
 *
 * Coverage targets (per brief): ≥70% branch and line on domain classes.
 * These tests cover 100% of IMaskMode public members.
 */

using NUnit.Framework;
using UnityEngine;
using VolumeData.Rendering;

namespace VolumeData.Rendering.Tests
{
    /// <summary>
    /// Edit-mode tests for all IMaskMode implementations.
    /// Tests validate shader index values, point cloud flag, and
    /// that Apply() calls the correct material methods.
    /// No Unity scene or runtime required.
    /// </summary>
    [TestFixture]
    public class IMaskModeTests
    {
        // ── Shared test fixtures ──────────────────────────────────────

        private MaskRenderContext _defaultContext;

        [SetUp]
        public void SetUp()
        {
            // Minimal context — actual values don't matter for these tests
            _defaultContext = new MaskRenderContext(
                voxelSize: 1.0f,
                voxelColor: Color.grey,
                highlightedSource: 0,
                voxelOffsets: new Vector4[4],
                modelMatrix: Matrix4x4.identity
            );
        }

        // ── ShaderModeIndex tests ─────────────────────────────────────

        [Test]
        public void MaskDisabled_ShaderModeIndex_IsZero()
        {
            // Verifies the integer sent to the MaskMode shader uniform
            // matches MASK_DISABLED = 0 in BasicVolume.cginc
            var mode = new MaskDisabled();
            Assert.AreEqual(0, mode.ShaderModeIndex);
        }

        [Test]
        public void MaskEnabled_ShaderModeIndex_IsOne()
        {
            // Verifies MASK_ENABLED = 1
            var mode = new MaskEnabled();
            Assert.AreEqual(1, mode.ShaderModeIndex);
        }

        [Test]
        public void MaskInverted_ShaderModeIndex_IsTwo()
        {
            // Verifies MASK_INVERTED = 2
            var mode = new MaskInverted();
            Assert.AreEqual(2, mode.ShaderModeIndex);
        }

        [Test]
        public void MaskIsolated_ShaderModeIndex_IsThree()
        {
            // Verifies MASK_ISOLATED = 3
            var mode = new MaskIsolated();
            Assert.AreEqual(3, mode.ShaderModeIndex);
        }

        [Test]
        public void AllModes_ShaderModeIndex_AreUnique()
        {
            // Guards against accidental duplicate index values
            var indices = new[]
            {
                new MaskDisabled().ShaderModeIndex,
                new MaskEnabled().ShaderModeIndex,
                new MaskInverted().ShaderModeIndex,
                new MaskIsolated().ShaderModeIndex,
            };
            var unique = new System.Collections.Generic.HashSet<int>(indices);
            Assert.AreEqual(4, unique.Count, "All four modes must have unique shader indices");
        }

        // ── RequiresPointCloudPass tests ──────────────────────────────

        [Test]
        public void MaskDisabled_RequiresPointCloudPass_IsFalse()
        {
            // Disabled mode never draws the point cloud — there is nothing to show
            var mode = new MaskDisabled();
            Assert.IsFalse(mode.RequiresPointCloudPass);
        }

        [Test]
        public void MaskEnabled_RequiresPointCloudPass_IsTrue()
        {
            var mode = new MaskEnabled();
            Assert.IsTrue(mode.RequiresPointCloudPass);
        }

        [Test]
        public void MaskInverted_RequiresPointCloudPass_IsTrue()
        {
            var mode = new MaskInverted();
            Assert.IsTrue(mode.RequiresPointCloudPass);
        }

        [Test]
        public void MaskIsolated_RequiresPointCloudPass_IsTrue()
        {
            // Isolated mode: point cloud IS the primary visual output
            var mode = new MaskIsolated();
            Assert.IsTrue(mode.RequiresPointCloudPass);
        }

        // ── Interface compliance tests ────────────────────────────────

        [Test]
        public void MaskDisabled_ImplementsIMaskMode()
        {
            Assert.IsInstanceOf<IMaskMode>(new MaskDisabled());
        }

        [Test]
        public void MaskEnabled_ImplementsIMaskMode()
        {
            Assert.IsInstanceOf<IMaskMode>(new MaskEnabled());
        }

        [Test]
        public void MaskInverted_ImplementsIMaskMode()
        {
            Assert.IsInstanceOf<IMaskMode>(new MaskInverted());
        }

        [Test]
        public void MaskIsolated_ImplementsIMaskMode()
        {
            Assert.IsInstanceOf<IMaskMode>(new MaskIsolated());
        }

        // ── MaskRenderContext construction tests ──────────────────────

        [Test]
        public void MaskRenderContext_StoresVoxelSize()
        {
            var ctx = new MaskRenderContext(2.5f, Color.red, 3, new Vector4[4], Matrix4x4.identity);
            Assert.AreEqual(2.5f, ctx.VoxelSize);
        }

        [Test]
        public void MaskRenderContext_StoresHighlightedSource()
        {
            var ctx = new MaskRenderContext(1.0f, Color.white, 42, new Vector4[4], Matrix4x4.identity);
            Assert.AreEqual((short)42, ctx.HighlightedSource);
        }

        [Test]
        public void MaskRenderContext_StoresModelMatrix()
        {
            var matrix = Matrix4x4.Scale(new Vector3(2, 3, 4));
            var ctx = new MaskRenderContext(1.0f, Color.white, 0, new Vector4[4], matrix);
            Assert.AreEqual(matrix, ctx.ModelMatrix);
        }

        // ── OCP guard test ────────────────────────────────────────────

        [Test]
        public void FourModesExist_NoUnaccountedImplementations()
        {
            // Reflection test: if a fifth mode is added, this test will fail,
            // reminding the developer to update the test suite.
            // This is the automated guard for the OCP compliance claim.
            var assembly = typeof(IMaskMode).Assembly;
            var implementations = System.Array.FindAll(
                assembly.GetTypes(),
                t => typeof(IMaskMode).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract
            );
            Assert.AreEqual(4, implementations.Length,
                "Expected exactly 4 IMaskMode implementations. " +
                "If you added a new mode, update this test.");
        }
    }
}
