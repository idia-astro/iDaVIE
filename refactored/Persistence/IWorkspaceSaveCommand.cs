// SPDX-License-Identifier: LGPL-3.0-or-later
// IWorkspaceSaveCommand — ST7 cross-team contract. Consumed by ST4 (voice /
// quick-menu save action) and ST6 (file menu). Payload-free; outcome reported
// via IPersistenceEvents.

namespace iDaVIE.Persistence
{
    public interface IWorkspaceSaveCommand
    {
        /// <summary>Triggers a save with an optional human-readable label.
        /// Returns once the save has been enqueued; the actual outcome arrives
        /// via IPersistenceEvents.SaveCompleted / SaveFailed.</summary>
        void Save(string label = null);
    }
}
