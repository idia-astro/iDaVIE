// SPDX-License-Identifier: LGPL-3.0-or-later
// ILogSink — ST1 cross-cutting log sink (global_model.md §1 ST1). Consumed by
// ST6's debug console; realised by Kernel/DebugLogSink (ST1).

using System;

namespace iDaVIE.Kernel.Contracts
{
    public enum LogLevel { Trace, Info, Warning, Error }

    public readonly record struct LogEntry(DateTime Timestamp, LogLevel Level, string Source, string Message);

    public interface ILogSink
    {
        void Write(LogLevel level, string source, string message);
        event Action<LogEntry> EntryAppended;
    }
}
