// SPDX-License-Identifier: LGPL-3.0-or-later
// ILogSink — ST1 cross-cutting log sink (global_model.md §1 ST1). Consumed by
// ST6's debug console; realised by Kernel/DebugLogSink (ST1).

using System;

namespace iDaVIE.Kernel.Contracts
{
    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Warning = 3,
        Error = 4,
        Fatal = 5
    }

    public readonly record struct LogEntry(LogLevel Level, string Source, string Message, DateTime Timestamp)
    {
        public LogEntry(LogLevel level, string source, string message)
            : this(level, source, message, DateTime.UtcNow)
        {
        }
    }

    public interface ILogSink
    {
        void Log(LogLevel level, string source, string message);

        void LogInfo(string source, string message);
        void LogWarning(string source, string message);
        void LogError(string source, string message);

        event Action<LogEntry> EntryLogged;

        System.Collections.Generic.IReadOnlyList<LogEntry> RecentEntries { get; }

        LogLevel MinimumStoredLevel { get; set; }

        // Compatibility surface retained for earlier refactored skeletons.
        void Write(LogLevel level, string source, string message);
        event Action<LogEntry> EntryAppended;
    }
}
