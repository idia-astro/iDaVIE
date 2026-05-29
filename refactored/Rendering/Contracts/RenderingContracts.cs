// SPDX-License-Identifier: LGPL-3.0-or-later
// ST3 cross-team contracts: render settings + mask mode + projection / scaling
// enums + IMomentMapRenderer + IRenderStateCapture. Canonical declaration site
// for everything in `iDaVIE.Rendering.Contracts` referenced by other refactored/
// files. Per shared_interfaces.md §3.1–§3.4.
//
// ColorMapEnum relocates from Assets/Scripts/Tools/ColorMapEnum.cs into this
// namespace with no API change.

using System;
using System.Threading;
using System.Threading.Tasks;
using iDaVIE.Kernel.Contracts.Types;       // FeatureColour

namespace iDaVIE.Rendering.Contracts
{
    // ── Enums (resolution line 9) ───────────────────────────────────────────
    public enum MaskMode       { Disabled = 0, Enabled = 1, Inverted = 2, Isolated = 3 }
    public enum ScalingType    { Linear = 0, Log = 1, Sqrt = 2, Square = 3, Power = 4, Gamma = 5 }
    public enum ProjectionMode { MaximumIntensityProjection = 0, AverageIntensityProjection = 1 }
    public enum ColorMapEnum   { Inferno = 0, Viridis, Plasma, Magma, Turbo, Greyscale, Cubehelix, Rainbow, Spectral, Cool, Hot, Jet }

    // ── IRenderSettings / IRenderSettingsMutator (resolution lines 19, 20) ──
    public interface IRenderSettings
    {
        float ThresholdMin { get; }
        float ThresholdMax { get; }

        ScalingType ScalingType     { get; }
        float       ScalingBias     { get; }
        float       ScalingContrast { get; }
        float       ScalingAlpha    { get; }
        float       ScalingGamma    { get; }

        ColorMapEnum ColorMap { get; }
        ProjectionMode ProjectionMode { get; }

        float ZAxisFactor    { get; }
        float ZAxisMinFactor { get; }
        float ZAxisMaxFactor { get; }

        bool IsFullResolution { get; }
        int  MaxRayMarchSteps { get; }

        float         VignetteIntensity { get; }
        float         VignetteFadeStart { get; }
        float         VignetteFadeEnd   { get; }
        FeatureColour VignetteColor     { get; }

        bool  FoveatedRendering { get; }
        float FoveationStart    { get; }
        float FoveationEnd      { get; }
        float FoveationJitter   { get; }
        int   FoveatedStepsLow  { get; }
        int   FoveatedStepsHigh { get; }

        MaskMode MaskMode      { get; }
        bool     DisplayMask   { get; }
        float    MaskVoxelSize { get; }

        event Action SettingsChanged;
    }

    public interface IRenderSettingsMutator
    {
        void SetThreshold(float min, float max);
        void ResetThreshold();

        void SetScaling(ScalingType type,
            float bias = 0f, float contrast = 1f, float alpha = 1000f, float gamma = 1f);

        void SetColorMap(ColorMapEnum colorMap);
        void ShiftColorMap(int delta);

        void SetFoveationJitter(float jitter);
        void SetProjection(ProjectionMode mode);

        void SetZAxisFactor(float factor);
        void ResetZAxis();

        void SetMaxRayMarchSteps(int steps);

        void SetVignetteIntensity(float intensity);
        void SetVignetteRange(float fadeStart, float fadeEnd);
        void SetVignetteColor(FeatureColour color);

        void SetFoveatedRendering(bool enabled);
        void SetFoveationRange(float start, float end);
        void SetFoveatedStepBudget(int low, int high);

        void ResetTransform();
    }

    // ── IMomentMapRenderer (M-08; resolution line 13) ───────────────────────
    public enum MomentOrder { Moment0 = 0, Moment1 = 1 }

    public readonly record struct MomentMapRequest(
        MomentOrder Order, float Threshold, bool UseMask, bool UseZScale, bool Inverted);

    public readonly record struct MomentMapResult(
        MomentOrder Order, int Width, int Height, float[] Values, float MinValue, float MaxValue);

    public interface IMomentMapRenderer
    {
        Task<MomentMapResult> RenderMomentMap(MomentMapRequest request,
            CancellationToken cancellationToken = default);

        bool IsRenderInProgress { get; }
        event Action RenderProgressChanged;
    }

    // ── IRenderStateCapture (M-16; resolution line 21) ──────────────────────
    public sealed class RenderStateDto
    {
        public int SchemaVersion { get; set; } = 1;

        public float ThresholdMin { get; set; }
        public float ThresholdMax { get; set; }
        public ScalingType ScalingType { get; set; }
        public float ScalingBias { get; set; }
        public float ScalingContrast { get; set; }
        public float ScalingAlpha { get; set; }
        public float ScalingGamma { get; set; }
        public ColorMapEnum ColorMap { get; set; }
        public ProjectionMode ProjectionMode { get; set; }
        public float ZAxisFactor { get; set; }
        public int MaxRayMarchSteps { get; set; }
        public bool FoveatedRendering { get; set; }
        public MaskMode MaskMode { get; set; }
        public bool DisplayMask { get; set; }
    }

    public interface IRenderStateCapture
    {
        RenderStateDto Capture();
        void           Restore(RenderStateDto dto);
    }
}
