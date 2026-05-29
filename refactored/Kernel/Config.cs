// SPDX-License-Identifier: LGPL-3.0-or-later
// Config — refactored from Assets/Scripts/VolumeData/Config.cs (237 LOC).
// Singleton `Config.Instance` is removed; consumers receive injected `IConfig`
// through the composition root. Loaded once at startup from JSON.

using System.Collections.Generic;

namespace iDaVIE.Kernel
{
    /// <summary>Cross-cutting startup configuration. Loaded once; immutable thereafter.</summary>
    public interface IConfig
    {
        int    GpuMemoryLimitMb            { get; }
        int    MaxRaymarchingSteps         { get; }
        int    MaxModeDownsampling         { get; }
        bool   FoveatedRendering           { get; }
        bool   BilinearFiltering           { get; }
        string DefaultColorMap             { get; }
        string DefaultScalingType          { get; }
        string AngleCoordFormat            { get; }
        string VelocityUnit                { get; }
        float  VoiceCommandConfidenceLevel { get; }
        bool   ImportedFeaturesStartVisible{ get; }
        int    MomentMapThresholdSteps     { get; }
        float  MomentMapStepsPerSecond     { get; }
        IReadOnlyList<string> Flags        { get; }
    }

    internal sealed class Config : IConfig
    {
        public int    GpuMemoryLimitMb            { get; init; }
        public int    MaxRaymarchingSteps         { get; init; }
        public int    MaxModeDownsampling         { get; init; }
        public bool   FoveatedRendering           { get; init; }
        public bool   BilinearFiltering           { get; init; }
        public string DefaultColorMap             { get; init; } = "";
        public string DefaultScalingType          { get; init; } = "";
        public string AngleCoordFormat            { get; init; } = "";
        public string VelocityUnit                { get; init; } = "";
        public float  VoiceCommandConfidenceLevel { get; init; }
        public bool   ImportedFeaturesStartVisible{ get; init; }
        public int    MomentMapThresholdSteps     { get; init; }
        public float  MomentMapStepsPerSecond     { get; init; }
        public IReadOnlyList<string> Flags        { get; init; } = System.Array.Empty<string>();

        /// <summary>Loads Config from a JSON file at the given path.</summary>
        public static IConfig LoadFromJson(string filePath)
            => throw new System.NotImplementedException();
    }
}
