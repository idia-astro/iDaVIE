// SPDX-License-Identifier: LGPL-3.0-or-later
// iDaVIE (immersive Data Visualisation Interactive Explorer)
// Copyright (C) 2024 IDIA, INAF-OACT — refactor skeleton, design-only.
//
// DebugLogSink — ST1 Infrastructure / ACL realising ILogSink (M-20).
//
// Replaces:
//   Assets/Scripts/Debuggers/DebugLogging.cs
//
// Refactor delta:
//   - The legacy class wrote directly to the engine debug console from anywhere in the
//     codebase (static reach). After refactor it is the single ACL that touches
//     that console; domain code emits through the injected ILogSink port and
//     stays Unity-free (same pattern as the ST1 BenchmarkHarness ACL over the
//     Unity Profiler — global_model.md §1 ST1).
//   - ST6's debug console subscribes to EntryAppended instead of scraping the
//     Unity console.
//
// Constructed once by KernelCompositionRoot and handed out as ILogSink.

using System;
using System.Collections.Generic;
using System.Linq;
using iDaVIE.Kernel.Contracts;   // ILogSink, LogLevel, LogEntry

namespace iDaVIE.Kernel
{
    internal sealed class DebugLogSink : ILogSink
    {
        private readonly int _capacity;
        private readonly List<LogEntry> _entries = new();

        public DebugLogSink(int capacity = 500)
        {
            _capacity = capacity > 0 ? capacity : 500;
        }

        public event Action<LogEntry> EntryLogged;
        public event Action<LogEntry> EntryAppended;

        public IReadOnlyList<LogEntry> RecentEntries => _entries.ToArray();

        public LogLevel MinimumStoredLevel { get; set; } = LogLevel.Trace;

        // Maps level to the presentation-side debug sink, then raises
        // EntryAppended for the ST6 console.
        public void Write(LogLevel level, string source, string message) =>
            Log(level, source, message);

        public void Log(LogLevel level, string source, string message)
        {
            var entry = new LogEntry(level, source ?? string.Empty, message ?? string.Empty);

            if (level >= MinimumStoredLevel)
            {
                _entries.Add(entry);
                if (_entries.Count > _capacity)
                    _entries.RemoveRange(0, _entries.Count - _capacity);
            }

            EntryLogged?.Invoke(entry);
            EntryAppended?.Invoke(entry);
        }

        public void LogInfo(string source, string message) =>
            Log(LogLevel.Info, source, message);

        public void LogWarning(string source, string message) =>
            Log(LogLevel.Warning, source, message);

        public void LogError(string source, string message) =>
            Log(LogLevel.Error, source, message);
    }
}
