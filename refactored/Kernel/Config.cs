// SPDX-License-Identifier: LGPL-3.0-or-later
// Config — refactored from Assets/Scripts/VolumeData/Config.cs (237 LOC).
// The legacy singleton is removed; consumers receive injected configuration
// through the composition root. Loaded once at startup from JSON.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace iDaVIE.Kernel.Contracts
{
    /// <summary>Cross-cutting startup configuration. Loaded once; immutable thereafter.</summary>
    public interface IConfig
    {
        float  DefaultThresholdMin         { get; }
        float  DefaultThresholdMax         { get; }
        float  DefaultZAxisFactor          { get; }
        int    MaxLoadedVolumes            { get; }
        int    DefaultSubcubeSize          { get; }
        int    LogRingCapacity             { get; }
        string PersistenceRootPath         { get; }
        int    MaxSavedWorkspaces          { get; }
        float  DefaultBrushRadius          { get; }
        int    ExpectedPluginAbiMajor      { get; }
        IReadOnlyDictionary<string, string> Extras { get; }

        // Compatibility surface retained for earlier skeleton drafts.
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

    public sealed class Config : IConfig
    {
        public float  DefaultThresholdMin          { get; init; } = 0.05f;
        public float  DefaultThresholdMax          { get; init; } = 0.95f;
        public float  DefaultZAxisFactor           { get; init; } = 1.0f;
        public string DefaultColorMap              { get; init; } = "Plasma";
        public int    MaxLoadedVolumes             { get; init; } = 4;
        public int    DefaultSubcubeSize           { get; init; }
        public int    LogRingCapacity              { get; init; } = 500;
        public string PersistenceRootPath          { get; init; } = "Workspaces";
        public int    MaxSavedWorkspaces           { get; init; } = 20;
        public float  DefaultBrushRadius           { get; init; } = 3.0f;
        public int    ExpectedPluginAbiMajor       { get; init; } = 1;
        public IReadOnlyDictionary<string, string> Extras { get; init; }
            = new Dictionary<string, string>();

        // Compatibility surface retained for earlier skeleton drafts.
        public int    GpuMemoryLimitMb             { get; init; } = 384;
        public int    MaxRaymarchingSteps          { get; init; } = 384;
        public int    MaxModeDownsampling          { get; init; } = 1;
        public bool   FoveatedRendering            { get; init; } = true;
        public bool   BilinearFiltering            { get; init; }
        public string DefaultScalingType           { get; init; } = "Linear";
        public string AngleCoordFormat             { get; init; } = "Sexagesimal";
        public string VelocityUnit                 { get; init; } = "Km";
        public float  VoiceCommandConfidenceLevel  { get; init; } = 0.3f;
        public bool   ImportedFeaturesStartVisible { get; init; } = true;
        public int    MomentMapThresholdSteps      { get; init; } = 40;
        public float  MomentMapStepsPerSecond      { get; init; } = 2f;
        public IReadOnlyList<string> Flags         { get; init; } = new[] { "-1", "0", "1" };

        /// <summary>Loads Config from a JSON file at the given path.</summary>
        public static Config LoadFromJson(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return new Config();

            using var stream = File.OpenRead(filePath);
            using var doc = JsonDocument.Parse(stream, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });

            var root = doc.RootElement;
            var defaults = new Config();

            return new Config
            {
                DefaultThresholdMin = ReadFloat(root, nameof(DefaultThresholdMin), "defaultThresholdMin", defaults.DefaultThresholdMin),
                DefaultThresholdMax = ReadFloat(root, nameof(DefaultThresholdMax), "defaultThresholdMax", defaults.DefaultThresholdMax),
                DefaultZAxisFactor = ReadFloat(root, nameof(DefaultZAxisFactor), "defaultZAxisFactor", defaults.DefaultZAxisFactor),
                DefaultColorMap = ReadString(root, nameof(DefaultColorMap), "defaultColorMap", defaults.DefaultColorMap),
                MaxLoadedVolumes = ReadInt(root, nameof(MaxLoadedVolumes), "maxLoadedVolumes", defaults.MaxLoadedVolumes),
                DefaultSubcubeSize = ReadInt(root, nameof(DefaultSubcubeSize), "defaultSubcubeSize", defaults.DefaultSubcubeSize),
                LogRingCapacity = ReadInt(root, nameof(LogRingCapacity), "logRingCapacity", defaults.LogRingCapacity),
                PersistenceRootPath = ReadString(root, nameof(PersistenceRootPath), "persistenceRootPath", defaults.PersistenceRootPath),
                MaxSavedWorkspaces = ReadInt(root, nameof(MaxSavedWorkspaces), "maxSavedWorkspaces", defaults.MaxSavedWorkspaces),
                DefaultBrushRadius = ReadFloat(root, nameof(DefaultBrushRadius), "defaultBrushRadius", defaults.DefaultBrushRadius),
                ExpectedPluginAbiMajor = ReadInt(root, nameof(ExpectedPluginAbiMajor), "expectedPluginAbiMajor", defaults.ExpectedPluginAbiMajor),
                Extras = ReadExtras(root),

                GpuMemoryLimitMb = ReadInt(root, nameof(GpuMemoryLimitMb), "gpuMemoryLimitMb", defaults.GpuMemoryLimitMb),
                MaxRaymarchingSteps = ReadInt(root, nameof(MaxRaymarchingSteps), "maxRaymarchingSteps", defaults.MaxRaymarchingSteps),
                MaxModeDownsampling = ReadInt(root, nameof(MaxModeDownsampling), "maxModeDownsampling", defaults.MaxModeDownsampling),
                FoveatedRendering = ReadBool(root, nameof(FoveatedRendering), "foveatedRendering", defaults.FoveatedRendering),
                BilinearFiltering = ReadBool(root, nameof(BilinearFiltering), "bilinearFiltering", defaults.BilinearFiltering),
                DefaultScalingType = ReadString(root, nameof(DefaultScalingType), "defaultScalingType", defaults.DefaultScalingType),
                AngleCoordFormat = ReadString(root, nameof(AngleCoordFormat), "angleCoordFormat", defaults.AngleCoordFormat),
                VelocityUnit = ReadString(root, nameof(VelocityUnit), "velocityUnit", defaults.VelocityUnit),
                VoiceCommandConfidenceLevel = ReadFloat(root, nameof(VoiceCommandConfidenceLevel), "voiceCommandConfidenceLevel", defaults.VoiceCommandConfidenceLevel),
                ImportedFeaturesStartVisible = ReadBool(root, nameof(ImportedFeaturesStartVisible), "importedFeaturesStartVisible", defaults.ImportedFeaturesStartVisible),
                MomentMapThresholdSteps = ReadInt(root, nameof(MomentMapThresholdSteps), "momentMapThresholdSteps", defaults.MomentMapThresholdSteps),
                MomentMapStepsPerSecond = ReadFloat(root, nameof(MomentMapStepsPerSecond), "momentMapStepsPerSecond", defaults.MomentMapStepsPerSecond),
                Flags = ReadStringArray(root, nameof(Flags), "flags", defaults.Flags)
            };
        }

        private static IReadOnlyDictionary<string, string> ReadExtras(JsonElement root)
        {
            if (!TryGet(root, nameof(Extras), "extras", out var value) ||
                value.ValueKind != JsonValueKind.Object)
            {
                return new Dictionary<string, string>();
            }

            var extras = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in value.EnumerateObject())
            {
                extras[property.Name] = property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString() ?? string.Empty
                    : property.Value.ToString();
            }

            return extras;
        }

        private static int ReadInt(JsonElement root, string canonical, string legacy, int fallback)
        {
            if (!TryGet(root, canonical, legacy, out var value))
                return fallback;
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
                return number;
            if (value.ValueKind == JsonValueKind.True)
                return 1;
            if (value.ValueKind == JsonValueKind.False)
                return 0;
            return value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out number)
                ? number
                : fallback;
        }

        private static float ReadFloat(JsonElement root, string canonical, string legacy, float fallback)
        {
            if (!TryGet(root, canonical, legacy, out var value))
                return fallback;
            if (value.ValueKind == JsonValueKind.Number && value.TryGetSingle(out var number))
                return number;
            return value.ValueKind == JsonValueKind.String &&
                   float.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out number)
                ? number
                : fallback;
        }

        private static bool ReadBool(JsonElement root, string canonical, string legacy, bool fallback)
        {
            if (!TryGet(root, canonical, legacy, out var value))
                return fallback;
            if (value.ValueKind == JsonValueKind.True)
                return true;
            if (value.ValueKind == JsonValueKind.False)
                return false;
            return value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out var result)
                ? result
                : fallback;
        }

        private static string ReadString(JsonElement root, string canonical, string legacy, string fallback)
        {
            if (!TryGet(root, canonical, legacy, out var value))
                return fallback;
            return value.ValueKind == JsonValueKind.String ? value.GetString() ?? fallback : value.ToString();
        }

        private static IReadOnlyList<string> ReadStringArray(
            JsonElement root,
            string canonical,
            string legacy,
            IReadOnlyList<string> fallback)
        {
            if (!TryGet(root, canonical, legacy, out var value) || value.ValueKind != JsonValueKind.Array)
                return fallback;

            var result = new List<string>();
            foreach (var item in value.EnumerateArray())
                result.Add(item.ValueKind == JsonValueKind.String ? item.GetString() ?? string.Empty : item.ToString());
            return result;
        }

        private static bool TryGet(JsonElement root, string canonical, string legacy, out JsonElement value)
        {
            if (root.TryGetProperty(canonical, out value))
                return true;
            if (root.TryGetProperty(legacy, out value))
                return true;

            foreach (var property in root.EnumerateObject())
            {
                if (string.Equals(property.Name, canonical, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(property.Name, legacy, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }
    }
}
