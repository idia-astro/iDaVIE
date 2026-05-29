// SPDX-License-Identifier: LGPL-3.0-or-later
// Pure C# smoke tests for the Team 1 refactored slice. This file intentionally
// has no test-framework dependency; a runner can call Team1KernelSmokeTests.RunAll().

using System;
using System.Collections.Generic;
using System.IO;
using iDaVIE.Kernel;
using iDaVIE.Kernel.Contracts;
using iDaVIE.Kernel.Contracts.Plugins;
using iDaVIE.Kernel.Contracts.Types;

namespace iDaVIE.Tests
{
    internal static class Team1KernelSmokeTests
    {
        public static void RunAll()
        {
            ConfigLoadsDefaultsAndJson();
            PluginRegistryProtectsDuplicateContracts();
            DebugLogSinkStoresRecentEntries();
            VolumeRegistryTransitionsActiveVolume();
            EnumStringUsesFallback();
            VolumeDataSetComputesStatsAndHistogram();
        }

        private static void ConfigLoadsDefaultsAndJson()
        {
            var defaults = Config.LoadFromJson(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json"));
            AssertEqual(4, defaults.MaxLoadedVolumes, nameof(defaults.MaxLoadedVolumes));

            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
            File.WriteAllText(path, "{\"MaxLoadedVolumes\":2,\"logRingCapacity\":7,\"extras\":{\"mode\":\"test\"}}");
            var loaded = Config.LoadFromJson(path);
            AssertEqual(2, loaded.MaxLoadedVolumes, nameof(loaded.MaxLoadedVolumes));
            AssertEqual(7, loaded.LogRingCapacity, nameof(loaded.LogRingCapacity));
            AssertEqual("test", loaded.Extras["mode"], "Extras[mode]");
        }

        private static void PluginRegistryProtectsDuplicateContracts()
        {
            var registry = new PluginRegistry();
            registry.RegisterPlugin<object>(new object());
            AssertTrue(registry.IsRegistered<object>(), nameof(registry.IsRegistered));
            AssertThrows<InvalidOperationException>(() => registry.RegisterPlugin<object>(new object()));
            AssertThrows<PluginNotFoundException>(() => registry.GetPlugin<IDisposable>());
        }

        private static void DebugLogSinkStoresRecentEntries()
        {
            var sink = new DebugLogSink(capacity: 2);
            var events = 0;
            sink.EntryLogged += _ => events++;
            sink.LogInfo("test", "one");
            sink.LogWarning("test", "two");
            sink.LogError("test", "three");
            AssertEqual(3, events, nameof(events));
            AssertEqual(2, sink.RecentEntries.Count, nameof(sink.RecentEntries));
            AssertEqual("three", sink.RecentEntries[1].Message, "last log message");
        }

        private static void VolumeRegistryTransitionsActiveVolume()
        {
            var registry = new VolumeRegistry();
            var first = MakeVolume("a");
            var second = MakeVolume("b");
            registry.Add(first);
            registry.Add(second);
            registry.SetActive(second);
            AssertTrue(ReferenceEquals(second, registry.ActiveVolume), nameof(registry.ActiveVolume));
            registry.Remove(second);
            AssertTrue(ReferenceEquals(first, registry.ActiveVolume), nameof(registry.ActiveVolume));
        }

        private static void EnumStringUsesFallback()
        {
            var parsed = EnumString.TryParseOrDefault("Warning", LogLevel.Info);
            var fallback = EnumString.TryParseOrDefault("NotALevel", LogLevel.Info);
            AssertEqual(LogLevel.Warning, parsed, nameof(parsed));
            AssertEqual(LogLevel.Info, fallback, nameof(fallback));
        }

        private static void VolumeDataSetComputesStatsAndHistogram()
        {
            var volume = MakeVolume("stats");
            var stats = volume.GetStats();
            AssertEqual(1f, stats.Min, nameof(stats.Min));
            AssertEqual(4f, stats.Max, nameof(stats.Max));
            AssertEqual(2.5f, stats.Mean, nameof(stats.Mean));
            AssertEqual(256, volume.GetHistogram().BinCount, nameof(HistogramData.BinCount));
            AssertEqual("deg", volume.GetAxisUnits().AxisX, nameof(AxisUnits.AxisX));
        }

        private static VolumeDataSet MakeVolume(string name)
        {
            var header = new Dictionary<string, string>
            {
                ["CUNIT1"] = "deg",
                ["CUNIT2"] = "deg",
                ["CUNIT3"] = "m/s"
            };
            return new VolumeDataSet(
                name,
                0,
                new VolumeExtents(2, 2, 1),
                SubcubeBounds.FullVolume(new VolumeExtents(2, 2, 1)),
                header,
                new RawAccess(),
                new EmptyMask());
        }

        private static void AssertTrue(bool condition, string label)
        {
            if (!condition)
                throw new InvalidOperationException($"Assertion failed: {label}");
        }

        private static void AssertEqual<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException($"Assertion failed for {label}: expected {expected}, got {actual}");
        }

        private static void AssertThrows<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
        }

        private sealed class RawAccess : IRawVoxelAccess
        {
            private readonly float[] _data = { 1f, 2f, 3f, 4f };

            public VoxelBufferDescriptor Descriptor { get; } = new()
            {
                Length = 4,
                SizeX = 2,
                SizeY = 2,
                SizeZ = 1,
                Generation = 1
            };

            public long CurrentGeneration => 1;
            public float[] GetSlice(int zIndex) => zIndex == 0 ? (float[])_data.Clone() : Array.Empty<float>();

            public void GetRegion(int zIndex, int xMin, int xMax, int yMin, int yMax, Span<float> destination)
            {
                var slice = GetSlice(zIndex);
                var cursor = 0;
                for (var y = yMin; y <= yMax && cursor < destination.Length; y++)
                {
                    for (var x = xMin; x <= xMax && cursor < destination.Length; x++)
                        destination[cursor++] = slice[y * 2 + x];
                }
            }
        }

        private sealed class EmptyMask : IMaskEditState
        {
            public short GetMaskValue(int x, int y, int z) => 0;
            public short[] GetMaskSlice(int axis, int sliceIndex) => Array.Empty<short>();
        }
    }
}
