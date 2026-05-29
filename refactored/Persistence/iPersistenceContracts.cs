// iDaVIE — immersive Data Visualisation Interactive Explorer
// Copyright (C) 2024 IDIA, INAF-OACT
// SPDX-License-Identifier: LGPL-3.0-or-later
//
// Sub-Team 7 — Persistence & Workspace State
// Cross-team interface declarations (ST7 owns; ST4 and ST6 consume).
// Canonical signatures are in shared_interfaces.md §7; this file reproduces
// them verbatim so the ST7 skeleton compiles-as-illustrated.

namespace iDaVIE.Persistence
{
    using System;
    using System.Collections.Generic;

    // -------------------------------------------------------------------------
    // §3.7  IWorkspaceSaveCommand
    // Consumed by: ST4 (voice/quick-menu), ST6 (desktop save button)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Fire-and-forget save trigger. Outcome is delivered asynchronously via
    /// <see cref="IPersistenceEvents"/>.
    /// </summary>
    public interface IWorkspaceSaveCommand
    {
        /// <summary>
        /// Initiates a workspace snapshot. Non-blocking — returns immediately.
        /// <para>
        /// On completion, <see cref="IPersistenceEvents.SaveCompleted"/> fires
        /// with the new <c>stateId</c>; on failure,
        /// <see cref="IPersistenceEvents.SaveFailed"/> fires with a
        /// human-readable reason string.
        /// </para>
        /// </summary>
        void Save();
    }

    // -------------------------------------------------------------------------
    // §3.7  IWorkspaceLoadCommand
    // Consumed by: ST4 (voice restore), ST6 (desktop load button)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Triggers restoration of a previously saved workspace.
    /// The <paramref name="stateId"/> is the opaque key returned by
    /// <see cref="IStateIndexQuery"/> or supplied by <see cref="IPersistenceEvents.SaveCompleted"/>.
    /// </summary>
    public interface IWorkspaceLoadCommand
    {
        /// <summary>
        /// Initiates a workspace restore. Non-blocking — returns immediately.
        /// <para>
        /// On success, <see cref="IPersistenceEvents.LoadCompleted"/> fires.
        /// On failure, <see cref="IPersistenceEvents.LoadFailed"/> fires with
        /// a human-readable reason.
        /// </para>
        /// </summary>
        /// <param name="stateId">Opaque identifier from <see cref="SavedStateInfo.StateId"/>.</param>
        void Load(string stateId);
    }

    // -------------------------------------------------------------------------
    // §3.7  SavedStateInfo  (value object)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Metadata record returned by <see cref="IStateIndexQuery"/>.
    /// All fields are plain C# — no <c>UnityEngine</c> types (brief §4.2 constraint 3).
    /// </summary>
    public sealed class SavedStateInfo
    {
        /// <summary>Opaque, stable identifier for the saved state (GUID string).</summary>
        public string StateId { get; init; } = string.Empty;

        /// <summary>Human-readable label chosen at save time (or auto-generated).</summary>
        public string DisplayName { get; init; } = string.Empty;

        /// <summary>UTC timestamp recorded when the snapshot was written to disk.</summary>
        public DateTime SavedAtUtc { get; init; }
    }

    // -------------------------------------------------------------------------
    // §3.7  IStateIndexQuery
    // Consumed by: ST6 (save/load dialog list)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Read-only access to the on-disk index of saved workspace states.
    /// The index is updated by <see cref="WorkspaceService"/> after every
    /// successful save; consumers do not write to it.
    /// </summary>
    public interface IStateIndexQuery
    {
        /// <summary>Returns all saved states, newest first.</summary>
        IReadOnlyList<SavedStateInfo> GetAll();

        /// <summary>
        /// Returns states whose <see cref="SavedStateInfo.DisplayName"/> contains
        /// <paramref name="searchTerm"/> (case-insensitive substring match).
        /// </summary>
        IReadOnlyList<SavedStateInfo> Search(string searchTerm);
    }

    // -------------------------------------------------------------------------
    // §3.7  IPersistenceEvents
    // Consumed by: ST6 (UI feedback — spinner, toast messages)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Observable lifecycle events raised by <see cref="WorkspaceService"/>.
    /// ST6 subscribes to drive the save/load progress UI.
    /// All events are raised on the Unity main thread.
    /// </summary>
    public interface IPersistenceEvents
    {
        /// <summary>Raised immediately before the snapshot pipeline begins.</summary>
        event Action SaveStarted;

        /// <summary>
        /// Raised when the snapshot has been written successfully.
        /// Payload is the <c>stateId</c> of the new state (for UI confirmation).
        /// </summary>
        event Action<string> SaveCompleted;

        /// <summary>
        /// Raised when the save pipeline fails.
        /// Payload is a human-readable error message suitable for display.
        /// </summary>
        event Action<string> SaveFailed;

        /// <summary>Raised immediately before the restore pipeline begins.</summary>
        event Action LoadStarted;

        /// <summary>Raised when restoration has completed successfully.</summary>
        event Action LoadCompleted;

        /// <summary>
        /// Raised when the restore pipeline fails.
        /// Payload is a human-readable error message.
        /// </summary>
        event Action<string> LoadFailed;
    }
}