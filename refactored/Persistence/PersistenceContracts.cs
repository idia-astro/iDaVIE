// SPDX-License-Identifier: LGPL-3.0-or-later
// PersistenceContracts — sole declaration site for the cross-team interface
// surface ST7 publishes (IWorkspaceSaveCommand, IWorkspaceLoadCommand,
// IStateIndexQuery, IPersistenceEvents) plus the SavedStateInfo DTO.
// Canonical signatures from shared_interfaces.md §7; "ST7 wins by default"
// (interface_resolutions.md line 31).
//
// Legacy: no persistence subsystem exists in Assets/Scripts/. The only
// persistence-adjacent code is VolumeDataSet.SaveMask(path) (line ~1380),
// which writes the mask FITS file only and has no wider state capture.
//
// Refactor delta:
//   - Four narrow interfaces replace a hypothetical single save-service class,
//     satisfying ISP: ST4 only receives IWorkspaceSaveCommand +
//     IWorkspaceLoadCommand; ST6 additionally receives IStateIndexQuery +
//     IPersistenceEvents. No consumer is forced to depend on members it
//     does not use.
//   - Fire-and-forget command pattern decouples callers from I/O timing —
//     Save() and Load() return immediately; outcomes arrive via IPersistenceEvents.
//   - SavedStateInfo is a plain-C# sealed class; no UnityEngine types cross
//     this boundary (brief §4.2 constraint 3).
//   - DIP: every consumer holds an interface, not a concrete class.

using System;
using System.Collections.Generic;

namespace iDaVIE.Persistence
{
    // Consumed by: ST4 (voice/quick-menu trigger), ST6 (desktop save button).
    public interface IWorkspaceSaveCommand
    {
        /// <summary>Fire-and-forget. Outcome reported via IPersistenceEvents.</summary>
        void Save();
    }

    // Consumed by: ST4 (voice restore), ST6 (desktop load button).
    public interface IWorkspaceLoadCommand
    {
        /// <summary>Triggers load by opaque stateId obtained from IStateIndexQuery.</summary>
        void Load(string stateId);
    }

    public sealed class SavedStateInfo
    {
        public string   StateId     { get; init; } = string.Empty;
        public string   DisplayName { get; init; } = string.Empty;
        public DateTime SavedAtUtc  { get; init; }
    }

    // Consumed by: ST6 (save/load dialog list). Not exposed to ST4.
    public interface IStateIndexQuery
    {
        IReadOnlyList<SavedStateInfo> GetAll();
        IReadOnlyList<SavedStateInfo> Search(string searchTerm);
    }

    // Consumed by: ST6 (progress spinner, toast messages). Not exposed to ST4.
    public interface IPersistenceEvents
    {
        event Action         SaveStarted;
        event Action<string> SaveCompleted;   // payload: stateId of the new state
        event Action<string> SaveFailed;      // payload: human-readable error

        event Action         LoadStarted;
        event Action         LoadCompleted;
        event Action<string> LoadFailed;      // payload: human-readable error
    }
}