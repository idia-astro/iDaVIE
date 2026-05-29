// SPDX-License-Identifier: LGPL-3.0-or-later
// DebugTabViewModel — ST6 ViewModel for the debug-console panel of
// CanvassDesktop. Subscribes to ILogSink (ST1) and exposes a scrollable view
// of the recent entries.

using System;
using System.Collections.Generic;
using iDaVIE.Kernel.Contracts;          // ILogSink, LogEntry, LogLevel

namespace iDaVIE.UI
{
    public sealed class DebugTabViewModel : IDisposable
    {
        public DebugTabViewModel(ILogSink logSink, int bufferSize = 1000)
            => throw new NotImplementedException();

        public IReadOnlyList<LogEntry> Entries { get; } = new List<LogEntry>();
        public LogLevel                MinLevel { get; private set; }

        public event Action EntriesChanged;

        public void SetMinLevel(LogLevel level) => throw new NotImplementedException();
        public void Clear()                     => throw new NotImplementedException();
        public void Dispose()                   => throw new NotImplementedException();
    }
}
