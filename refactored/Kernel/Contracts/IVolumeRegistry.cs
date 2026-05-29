// SPDX-License-Identifier: LGPL-3.0-or-later
// IVolumeRegistry — ST1 cross-team contract. Tracks the currently active volume
// (M-03, M-19; replaces ST4's removed DataSetRegistry). Consumed by ST3, ST5, ST6.

using System;

namespace iDaVIE.Kernel.Contracts
{
    public interface IVolumeRegistry
    {
        IVolumeDataSet Active { get; }
        bool           HasActive { get; }

        /// <summary>Fired when the active volume changes (Set or Cleared).</summary>
        event Action ActiveVolumeChanged;

        void SetActive(IVolumeDataSet volume);
        void ClearActive();
    }
}
