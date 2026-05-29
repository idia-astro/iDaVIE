// SPDX-License-Identifier: LGPL-3.0-or-later
// StorageLocation — ST7 Domain. Logical descriptor of the persistence backend.
// Decouples the index from the storage layer (FileSystemStorageBackend lives
// in Tier-2 Infrastructure).

namespace iDaVIE.Persistence.Domain
{
    public enum StorageKind { Filesystem }

    public readonly record struct StorageLocation(StorageKind Kind, string RootPath);
}
