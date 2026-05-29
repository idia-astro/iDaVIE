// SPDX-License-Identifier: LGPL-3.0-or-later
// ValidationAndRecoveryService — ST7 Application. Integrity checks on load.
// Recovery procedure when a load fails partway: no partial state visible to
// the system. Rolls back if any per-team restore rejects the payload.

using iDaVIE.Persistence.Domain;

namespace iDaVIE.Persistence.Application
{
    internal sealed class ValidationAndRecoveryService
    {
        /// <summary>Verifies the integrity record against the on-disk payload.</summary>
        public bool ValidateIntegrity(StoredState state) => throw new System.NotImplementedException();

        /// <summary>Applies any necessary migration rules to bring an older envelope
        /// up to the current SchemaVersion.</summary>
        public StoredState Migrate(StoredState state) => throw new System.NotImplementedException();

        /// <summary>Rolls back partial state when a per-team restore rejects its
        /// payload mid-load.</summary>
        public void Rollback() => throw new System.NotImplementedException();
    }
}
