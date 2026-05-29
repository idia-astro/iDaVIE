// SPDX-License-Identifier: LGPL-3.0-or-later
// IPersistenceEvents — ST7 cross-team contract. Consumed by ST6 for UI
// feedback (autosave indicator, progress badges, error toasts).

using System;

namespace iDaVIE.Persistence
{
    public interface IPersistenceEvents
    {
        event Action               SaveStarted;
        event Action<string>       SaveCompleted;  // stateId
        event Action<string>       SaveFailed;     // error message
        event Action               LoadStarted;
        event Action               LoadCompleted;
        event Action<string>       LoadFailed;     // error message
    }
}
