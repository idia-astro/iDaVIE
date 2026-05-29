// SPDX-License-Identifier: LGPL-3.0-or-later
// BenchmarkHarness — ST1 benchmark boundary replacing Tools/BenchmarkManager.
// The Unity-facing adapter can drive rotations or profiler samples outside the
// kernel; this class owns the testable timing/session model.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using iDaVIE.Kernel.Contracts;

namespace iDaVIE.Kernel
{
    public sealed class BenchmarkSample
    {
        public string Name { get; init; } = string.Empty;
        public DateTime StartedUtc { get; init; }
        public DateTime CompletedUtc { get; init; }
        public TimeSpan Elapsed { get; init; }
        public IReadOnlyDictionary<string, string> Tags { get; init; }
            = new Dictionary<string, string>();
    }

    public interface IBenchmarkHarness
    {
        IDisposable Measure(string name, IReadOnlyDictionary<string, string>? tags = null);
        BenchmarkSession Start(string name, IReadOnlyDictionary<string, string>? tags = null);
        BenchmarkSample Complete(BenchmarkSession session);
        IReadOnlyList<BenchmarkSample> RecentSamples { get; }
        event Action<BenchmarkSample> SampleCompleted;
    }

    public sealed class BenchmarkSession
    {
        internal BenchmarkSession(string name, IReadOnlyDictionary<string, string> tags)
        {
            Name = name;
            Tags = tags;
            StartedUtc = DateTime.UtcNow;
            Stopwatch = Stopwatch.StartNew();
        }

        public string Name { get; }
        public DateTime StartedUtc { get; }
        public IReadOnlyDictionary<string, string> Tags { get; }
        internal Stopwatch Stopwatch { get; }
    }

    internal sealed class BenchmarkHarness : IBenchmarkHarness
    {
        private readonly ILogSink _log;
        private readonly int _capacity;
        private readonly List<BenchmarkSample> _samples = new();

        public BenchmarkHarness(ILogSink log, int capacity = 200)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _capacity = capacity > 0 ? capacity : 200;
        }

        public IReadOnlyList<BenchmarkSample> RecentSamples => _samples.ToArray();
        public event Action<BenchmarkSample> SampleCompleted;

        public IDisposable Measure(string name, IReadOnlyDictionary<string, string>? tags = null) =>
            new Scope(this, Start(name, tags));

        public BenchmarkSession Start(string name, IReadOnlyDictionary<string, string>? tags = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Benchmark name must not be empty.", nameof(name));

            return new BenchmarkSession(name, CloneTags(tags));
        }

        public BenchmarkSample Complete(BenchmarkSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            session.Stopwatch.Stop();
            var sample = new BenchmarkSample
            {
                Name = session.Name,
                StartedUtc = session.StartedUtc,
                CompletedUtc = DateTime.UtcNow,
                Elapsed = session.Stopwatch.Elapsed,
                Tags = session.Tags
            };

            _samples.Add(sample);
            if (_samples.Count > _capacity)
                _samples.RemoveRange(0, _samples.Count - _capacity);

            _log.LogInfo(nameof(BenchmarkHarness),
                $"{sample.Name} completed in {sample.Elapsed.TotalMilliseconds:0.###} ms.");
            SampleCompleted?.Invoke(sample);
            return sample;
        }

        private static IReadOnlyDictionary<string, string> CloneTags(IReadOnlyDictionary<string, string>? tags) =>
            tags == null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string>(tags);

        private sealed class Scope : IDisposable
        {
            private readonly BenchmarkHarness _owner;
            private BenchmarkSession? _session;

            public Scope(BenchmarkHarness owner, BenchmarkSession session)
            {
                _owner = owner;
                _session = session;
            }

            public void Dispose()
            {
                var session = _session;
                if (session == null)
                    return;
                _session = null;
                _owner.Complete(session);
            }
        }
    }
}
