// Example2_MaskModeTests.cs
// Unit tests for the refactored Mask Mode Strategy pattern (Example 2).
//
// Covers:
//   · IMaskMode interface contract (ShaderKeyword correctness, LSP substitutability)
//   · ApplyMaskMode    — keyword mutual exclusion, _MaskAlpha = 1.0
//   · InverseMaskMode  — keyword mutual exclusion, _MaskAlpha = 1.0
//   · IsolateMaskMode  — keyword mutual exclusion, _MaskAlpha = 0.15
//   · DisabledMaskMode — all keywords off, null-texture safe
//   · NullMaskMode     — test-double identity check
//
// Test runner: Unity Test Runner, Edit Mode.
//
// Tier 1 tests (no Material required):
//   All ShaderKeyword property tests and NullMaskMode tests run with no Unity
//   runtime dependency — ShaderKeyword is a pure C# string property.
//
// Tier 2 tests (Material required):
//   Apply() tests create a Material in Unity Edit Mode using a built-in shader.
//   In the Unity Test Runner these are [Test] methods (not [UnityTest]), because
//   Material.EnableKeyword/DisableKeyword does not require a running player loop —
//   it only requires the Unity Editor context (available in Edit Mode tests).
//
//   Required asmdef: iDaVIE.Rendering.Tests.asmdef, Editor-only, testable assembly.
//
// File placement:
//   refactoring-examples/team3/tests/Example2_MaskModeTests.cs

using NUnit.Framework;
using UnityEngine;
using iDaVIE.Rendering;
using iDaVIE.Rendering.Tests;

namespace iDaVIE.Rendering.Editor.Tests
{
    // =========================================================================
    // ShaderKeywordTests — Tier 1 (no Material, no GPU)
    // Verifies that each IMaskMode implementation returns the correct HLSL
    // keyword constant. These strings must match the #pragma multi_compile in
    // VolumeRender.shader — a mismatch silently produces a no-op.
    // =========================================================================

    [TestFixture]
    public class ShaderKeywordTests
    {
        [Test]
        public void ApplyMaskMode_ShaderKeyword_IsMaskApply()
        {
            IMaskMode mode = new ApplyMaskMode();
            Assert.AreEqual("_MASK_APPLY", mode.ShaderKeyword);
        }

        [Test]
        public void InverseMaskMode_ShaderKeyword_IsMaskInverse()
        {
            IMaskMode mode = new InverseMaskMode();
            Assert.AreEqual("_MASK_INVERSE", mode.ShaderKeyword);
        }

        [Test]
        public void IsolateMaskMode_ShaderKeyword_IsMaskIsolate()
        {
            IMaskMode mode = new IsolateMaskMode();
            Assert.AreEqual("_MASK_ISOLATE", mode.ShaderKeyword);
        }

        [Test]
        public void DisabledMaskMode_ShaderKeyword_IsMaskDisabled()
        {
            IMaskMode mode = new DisabledMaskMode();
            Assert.AreEqual("_MASK_DISABLED", mode.ShaderKeyword);
        }

        [Test]
        public void NullMaskMode_ShaderKeyword_IsMaskNull()
        {
            // NullMaskMode is a test double; its sentinel must not match any real keyword.
            IMaskMode mode = new NullMaskMode();
            Assert.AreEqual("_MASK_NULL", mode.ShaderKeyword);
            Assert.AreNotEqual("_MASK_APPLY",    mode.ShaderKeyword, "NullMaskMode must not alias a real keyword");
            Assert.AreNotEqual("_MASK_INVERSE",  mode.ShaderKeyword);
            Assert.AreNotEqual("_MASK_ISOLATE",  mode.ShaderKeyword);
            Assert.AreNotEqual("_MASK_DISABLED", mode.ShaderKeyword);
        }

        [Test]
        public void AllKeywords_AreDistinct()
        {
            // All keywords must be unique — multiple enabled keywords from the same
            // multi_compile group cause undefined shader variant selection.
            var keywords = new[]
            {
                new ApplyMaskMode().ShaderKeyword,
                new InverseMaskMode().ShaderKeyword,
                new IsolateMaskMode().ShaderKeyword,
                new DisabledMaskMode().ShaderKeyword,
            };
            var unique = new System.Collections.Generic.HashSet<string>(keywords);
            Assert.AreEqual(keywords.Length, unique.Count,
                "Every IMaskMode implementation must have a unique ShaderKeyword");
        }
    }

    // =========================================================================
    // ApplyMaskModeTests — Tier 2 (Material required, Unity Edit Mode)
    // =========================================================================

    [TestFixture]
    public class ApplyMaskModeTests
    {
        private Material _mat;

        [SetUp]
        public void SetUp()
        {
            // Any shader with keyword support works here. "Standard" is always present
            // in Unity projects. Use "Hidden/InternalErrorShader" as a fallback.
            var shader = Shader.Find("Standard") ?? Shader.Find("Hidden/InternalErrorShader");
            _mat = new Material(shader);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_mat);
        }

        [Test]
        public void Apply_EnablesMaskApplyKeyword()
        {
            var mode = new ApplyMaskMode();
            mode.Apply(_mat, null);
            Assert.IsTrue(_mat.IsKeywordEnabled("_MASK_APPLY"),
                "ApplyMaskMode must enable _MASK_APPLY");
        }

        [Test]
        public void Apply_DisablesMaskInverseKeyword()
        {
            // Pre-enable to confirm mutual exclusion is active (not just a no-op).
            _mat.EnableKeyword("_MASK_INVERSE");
            var mode = new ApplyMaskMode();
            mode.Apply(_mat, null);
            Assert.IsFalse(_mat.IsKeywordEnabled("_MASK_INVERSE"),
                "ApplyMaskMode must disable _MASK_INVERSE (mutual exclusion)");
        }

        [Test]
        public void Apply_DisablesMaskIsolateKeyword()
        {
            _mat.EnableKeyword("_MASK_ISOLATE");
            var mode = new ApplyMaskMode();
            mode.Apply(_mat, null);
            Assert.IsFalse(_mat.IsKeywordEnabled("_MASK_ISOLATE"),
                "ApplyMaskMode must disable _MASK_ISOLATE");
        }

        [Test]
        public void Apply_DisablesMaskDisabledKeyword()
        {
            _mat.EnableKeyword("_MASK_DISABLED");
            var mode = new ApplyMaskMode();
            mode.Apply(_mat, null);
            Assert.IsFalse(_mat.IsKeywordEnabled("_MASK_DISABLED"),
                "ApplyMaskMode must disable _MASK_DISABLED");
        }

        [Test]
        public void Apply_SetsMaskAlphaToOne()
        {
            var mode = new ApplyMaskMode();
            mode.Apply(_mat, null);
            Assert.AreEqual(1.0f, _mat.GetFloat("_MaskAlpha"), 0.0001f,
                "ApplyMaskMode must set _MaskAlpha to 1.0 (full opacity inside mask)");
        }

        [Test]
        public void Apply_WithNullTexture_DoesNotThrow()
        {
            // maskTexture == null is the normal call site (BindMaskTexture already bound it).
            var mode = new ApplyMaskMode();
            Assert.DoesNotThrow(() => mode.Apply(_mat, null),
                "Apply() must be safe with a null maskTexture parameter");
        }

        [Test]
        public void Apply_OnlyOneMaskKeywordEnabled()
        {
            // Simulate a material where multiple keywords were previously enabled.
            _mat.EnableKeyword("_MASK_INVERSE");
            _mat.EnableKeyword("_MASK_ISOLATE");
            var mode = new ApplyMaskMode();
            mode.Apply(_mat, null);

            int enabledCount = 0;
            if (_mat.IsKeywordEnabled("_MASK_APPLY"))    enabledCount++;
            if (_mat.IsKeywordEnabled("_MASK_INVERSE"))  enabledCount++;
            if (_mat.IsKeywordEnabled("_MASK_ISOLATE"))  enabledCount++;
            if (_mat.IsKeywordEnabled("_MASK_DISABLED")) enabledCount++;

            Assert.AreEqual(1, enabledCount,
                "Exactly one mask keyword must be enabled after Apply() — prevents undefined shader variant");
        }
    }

    // =========================================================================
    // InverseMaskModeTests — Tier 2
    // =========================================================================

    [TestFixture]
    public class InverseMaskModeTests
    {
        private Material _mat;

        [SetUp]
        public void SetUp()
        {
            var shader = Shader.Find("Standard") ?? Shader.Find("Hidden/InternalErrorShader");
            _mat = new Material(shader);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_mat);
        }

        [Test]
        public void Apply_EnablesMaskInverseKeyword()
        {
            var mode = new InverseMaskMode();
            mode.Apply(_mat, null);
            Assert.IsTrue(_mat.IsKeywordEnabled("_MASK_INVERSE"),
                "InverseMaskMode must enable _MASK_INVERSE");
        }

        [Test]
        public void Apply_DisablesMaskApplyKeyword()
        {
            _mat.EnableKeyword("_MASK_APPLY");
            var mode = new InverseMaskMode();
            mode.Apply(_mat, null);
            Assert.IsFalse(_mat.IsKeywordEnabled("_MASK_APPLY"),
                "InverseMaskMode must disable _MASK_APPLY (mutual exclusion)");
        }

        [Test]
        public void Apply_DisablesMaskIsolateKeyword()
        {
            _mat.EnableKeyword("_MASK_ISOLATE");
            new InverseMaskMode().Apply(_mat, null);
            Assert.IsFalse(_mat.IsKeywordEnabled("_MASK_ISOLATE"));
        }

        [Test]
        public void Apply_DisablesMaskDisabledKeyword()
        {
            _mat.EnableKeyword("_MASK_DISABLED");
            new InverseMaskMode().Apply(_mat, null);
            Assert.IsFalse(_mat.IsKeywordEnabled("_MASK_DISABLED"));
        }

        [Test]
        public void Apply_SetsMaskAlphaToOne()
        {
            // InverseMaskMode: outside-mask voxels render at full opacity.
            var mode = new InverseMaskMode();
            mode.Apply(_mat, null);
            Assert.AreEqual(1.0f, _mat.GetFloat("_MaskAlpha"), 0.0001f,
                "InverseMaskMode must set _MaskAlpha to 1.0 (full opacity outside mask)");
        }

        [Test]
        public void Apply_OnlyOneMaskKeywordEnabled()
        {
            _mat.EnableKeyword("_MASK_APPLY");
            _mat.EnableKeyword("_MASK_ISOLATE");
            new InverseMaskMode().Apply(_mat, null);

            int enabledCount = 0;
            if (_mat.IsKeywordEnabled("_MASK_APPLY"))    enabledCount++;
            if (_mat.IsKeywordEnabled("_MASK_INVERSE"))  enabledCount++;
            if (_mat.IsKeywordEnabled("_MASK_ISOLATE"))  enabledCount++;
            if (_mat.IsKeywordEnabled("_MASK_DISABLED")) enabledCount++;

            Assert.AreEqual(1, enabledCount);
        }
    }

    // =========================================================================
    // IsolateMaskModeTests — Tier 2
    // IsolateMaskMode is the only active mode where _MaskAlpha != 1.0.
    // =========================================================================

    [TestFixture]
    public class IsolateMaskModeTests
    {
        private Material _mat;

        [SetUp]
        public void SetUp()
        {
            var shader = Shader.Find("Standard") ?? Shader.Find("Hidden/InternalErrorShader");
            _mat = new Material(shader);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_mat);
        }

        [Test]
        public void Apply_EnablesMaskIsolateKeyword()
        {
            var mode = new IsolateMaskMode();
            mode.Apply(_mat, null);
            Assert.IsTrue(_mat.IsKeywordEnabled("_MASK_ISOLATE"),
                "IsolateMaskMode must enable _MASK_ISOLATE");
        }

        [Test]
        public void Apply_DisablesMaskApplyKeyword()
        {
            _mat.EnableKeyword("_MASK_APPLY");
            new IsolateMaskMode().Apply(_mat, null);
            Assert.IsFalse(_mat.IsKeywordEnabled("_MASK_APPLY"));
        }

        [Test]
        public void Apply_DisablesMaskInverseKeyword()
        {
            _mat.EnableKeyword("_MASK_INVERSE");
            new IsolateMaskMode().Apply(_mat, null);
            Assert.IsFalse(_mat.IsKeywordEnabled("_MASK_INVERSE"));
        }

        [Test]
        public void Apply_DisablesMaskDisabledKeyword()
        {
            _mat.EnableKeyword("_MASK_DISABLED");
            new IsolateMaskMode().Apply(_mat, null);
            Assert.IsFalse(_mat.IsKeywordEnabled("_MASK_DISABLED"));
        }

        [Test]
        public void Apply_SetsMaskAlphaToFifteenPercent()
        {
            // IsolateMaskMode uses 0.15 — the named constant OutsideAlpha extracted
            // from the literal in the before/ if-branch. This is the only active mode
            // where _MaskAlpha is not 1.0.
            var mode = new IsolateMaskMode();
            mode.Apply(_mat, null);
            Assert.AreEqual(0.15f, _mat.GetFloat("_MaskAlpha"), 0.0001f,
                "IsolateMaskMode must set _MaskAlpha to 0.15 (15% opacity for outside-mask voxels)");
        }

        [Test]
        public void Apply_MaskAlphaDifferentFromApplyAndInverseModes()
        {
            // Regression guard: IsolateMaskMode's _MaskAlpha must be distinct from
            // the value used by ApplyMaskMode and InverseMaskMode (1.0).
            // If someone copies Apply() and forgets to change the float, this test catches it.
            var isolate = new IsolateMaskMode();
            isolate.Apply(_mat, null);
            float isolateAlpha = _mat.GetFloat("_MaskAlpha");

            new ApplyMaskMode().Apply(_mat, null);
            float applyAlpha = _mat.GetFloat("_MaskAlpha");

            Assert.AreNotEqual(isolateAlpha, applyAlpha,
                "IsolateMaskMode must use a different _MaskAlpha than ApplyMaskMode");
        }
    }

    // =========================================================================
    // DisabledMaskModeTests — Tier 2
    // DisabledMaskMode implements the Null Object pattern: a valid IMaskMode that
    // safely does nothing when no mask is loaded.
    // =========================================================================

    [TestFixture]
    public class DisabledMaskModeTests
    {
        private Material _mat;

        [SetUp]
        public void SetUp()
        {
            var shader = Shader.Find("Standard") ?? Shader.Find("Hidden/InternalErrorShader");
            _mat = new Material(shader);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_mat);
        }

        [Test]
        public void Apply_DisablesAllMaskKeywords()
        {
            // Pre-enable all keywords to confirm disable is active, not just a no-op.
            _mat.EnableKeyword("_MASK_APPLY");
            _mat.EnableKeyword("_MASK_INVERSE");
            _mat.EnableKeyword("_MASK_ISOLATE");
            _mat.EnableKeyword("_MASK_DISABLED");

            new DisabledMaskMode().Apply(_mat, null);

            Assert.IsFalse(_mat.IsKeywordEnabled("_MASK_APPLY"),    "Must disable _MASK_APPLY");
            Assert.IsFalse(_mat.IsKeywordEnabled("_MASK_INVERSE"),  "Must disable _MASK_INVERSE");
            Assert.IsFalse(_mat.IsKeywordEnabled("_MASK_ISOLATE"),  "Must disable _MASK_ISOLATE");
            Assert.IsFalse(_mat.IsKeywordEnabled("_MASK_DISABLED"), "Must disable _MASK_DISABLED");
        }

        [Test]
        public void Apply_WithNullTexture_DoesNotThrow()
        {
            // This is the ONLY mode that is valid when maskTexture == null (no mask loaded).
            // Calling SetTexture(null) in Unity triggers a warning, so DisabledMaskMode
            // must NOT call SetTexture at all.
            Assert.DoesNotThrow(() => new DisabledMaskMode().Apply(_mat, null),
                "DisabledMaskMode.Apply() must be safe with null maskTexture");
        }

        [Test]
        public void Apply_CalledTwice_IsIdempotent()
        {
            // DisabledMaskMode has no state; calling it twice must produce the same result.
            var mode = new DisabledMaskMode();
            mode.Apply(_mat, null);
            mode.Apply(_mat, null);
            Assert.IsFalse(_mat.IsKeywordEnabled("_MASK_APPLY"),    "Second call must not re-enable any keyword");
            Assert.IsFalse(_mat.IsKeywordEnabled("_MASK_INVERSE"),  "Second call must not re-enable any keyword");
            Assert.IsFalse(_mat.IsKeywordEnabled("_MASK_ISOLATE"),  "Second call must not re-enable any keyword");
            Assert.IsFalse(_mat.IsKeywordEnabled("_MASK_DISABLED"), "Second call must not re-enable any keyword");
        }
    }

    // =========================================================================
    // StrategySubstitutabilityTests (LSP)
    // Demonstrates that all IMaskMode implementations are interchangeable through
    // the interface — no caller ever needs to know the concrete type.
    // =========================================================================

    [TestFixture]
    public class StrategySubstitutabilityTests
    {
        private Material _mat;

        [SetUp]
        public void SetUp()
        {
            var shader = Shader.Find("Standard") ?? Shader.Find("Hidden/InternalErrorShader");
            _mat = new Material(shader);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_mat);
        }

        [Test]
        public void AllConcreteImplementations_CanBeHeldAsIMaskMode()
        {
            // Verify each concrete type is assignable to IMaskMode (compile-time proof,
            // documented here as a runtime assertion for the test report).
            IMaskMode[] modes = new IMaskMode[]
            {
                new ApplyMaskMode(),
                new InverseMaskMode(),
                new IsolateMaskMode(),
                new DisabledMaskMode(),
                new NullMaskMode(),
            };
            Assert.AreEqual(5, modes.Length, "All expected implementations must be registered here");
        }

        [Test]
        public void CallerNeverNeedsConcreteType_ApplyDispatchesPolymorphically()
        {
            // This mirrors the VolumeMaterialBinder.Tick() call:
            //   _activeMaskMode.Apply(_material, null);
            // The caller holds IMaskMode; it never checks the concrete type.
            IMaskMode mode = new ApplyMaskMode();  // injected externally
            Assert.DoesNotThrow(() => mode.Apply(_mat, null),
                "IMaskMode.Apply() must dispatch correctly without casting");
        }

        [Test]
        public void ModeSwap_AfterCallingSetActiveMaskMode_NewModeApplied()
        {
            // Simulates the VolumeMaterialBinder.SetActiveMaskMode(mode) call.
            // Start with ApplyMaskMode, switch to InverseMaskMode at runtime.
            IMaskMode active = new ApplyMaskMode();
            active.Apply(_mat, null);
            Assert.IsTrue(_mat.IsKeywordEnabled("_MASK_APPLY"), "Initially apply mode should be set");

            // Swap (simulating SetActiveMaskMode).
            active = new InverseMaskMode();
            active.Apply(_mat, null);

            Assert.IsTrue(_mat.IsKeywordEnabled("_MASK_INVERSE"),  "After swap, inverse must be enabled");
            Assert.IsFalse(_mat.IsKeywordEnabled("_MASK_APPLY"),   "After swap, apply must be disabled");
        }

        [Test]
        public void SwitchingBetweenAllModes_AllTransitionsLeaveExactlyOneKeywordEnabled()
        {
            // Exhaustive round-robin swap test. Every transition must leave exactly one
            // keyword enabled — the core invariant of the mutual-exclusion design.
            IMaskMode[] sequence = new IMaskMode[]
            {
                new ApplyMaskMode(),
                new InverseMaskMode(),
                new IsolateMaskMode(),
                new ApplyMaskMode(),
                new DisabledMaskMode(),
                new IsolateMaskMode(),
            };

            string[] keywords = { "_MASK_APPLY", "_MASK_INVERSE", "_MASK_ISOLATE", "_MASK_DISABLED" };

            foreach (var mode in sequence)
            {
                mode.Apply(_mat, null);

                if (mode is DisabledMaskMode)
                {
                    // DisabledMaskMode is a special case: it clears all keywords (0 active).
                    int activeCount = CountEnabled(keywords);
                    Assert.AreEqual(0, activeCount,
                        $"DisabledMaskMode must leave 0 keywords enabled");
                }
                else
                {
                    int activeCount = CountEnabled(keywords);
                    Assert.AreEqual(1, activeCount,
                        $"After applying {mode.GetType().Name}, exactly 1 keyword must be enabled");
                }
            }
        }

        private int CountEnabled(string[] keywords)
        {
            int count = 0;
            foreach (var kw in keywords)
                if (_mat.IsKeywordEnabled(kw)) count++;
            return count;
        }
    }
}
