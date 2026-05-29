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
//   - The legacy class wrote directly to UnityEngine.Debug from anywhere in the
//     codebase (static reach). After refactor it is the single ACL that touches
//     UnityEngine.Debug; domain code emits through the injected ILogSink port and
//     stays Unity-free (same pattern as the ST1 BenchmarkHarness ACL over the
//     Unity Profiler — global_model.md §1 ST1).
//   - ST6's debug console subscribes to EntryAppended instead of scraping the
//     Unity console.
//
// Constructed once by KernelCompositionRoot and handed out as ILogSink.

using System;
using iDaVIE.Kernel.Contracts;   // ILogSink, LogLevel, LogEntry

namespace iDaVIE.Kernel
{
    internal sealed class DebugLogSink : ILogSink
    {
        public event Action<LogEntry> EntryAppended;

        // Maps level → UnityEngine.Debug.Log / LogWarning / LogError (verbatim from
        // legacy DebugLogging), then raises EntryAppended for the ST6 console.
        public void Write(LogLevel level, string source, string message) =>
            throw new NotImplementedException();
    }
}
