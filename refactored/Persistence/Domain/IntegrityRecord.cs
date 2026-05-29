// SPDX-License-Identifier: LGPL-3.0-or-later
// IntegrityRecord — ST7 Domain. Checksum + validation status alongside the
// state it certifies. Computed at save; verified at load.

namespace iDaVIE.Persistence.Domain
{
    public enum IntegrityStatus { Unverified, Valid, Corrupt }

    public readonly record struct IntegrityRecord(
        string          Sha256,
        long            SizeBytes,
        IntegrityStatus Status);
}
