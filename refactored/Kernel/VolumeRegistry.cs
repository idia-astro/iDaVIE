// SPDX-License-Identifier: LGPL-3.0-or-later
// VolumeRegistry — ST1 application service realising IVolumeRegistry.
// Replaces scene-level "active renderer" searches with a narrow kernel port.

using System;
using System.Collections.Generic;
using iDaVIE.Kernel.Contracts;

namespace iDaVIE.Kernel
{
    internal sealed class VolumeRegistry : IVolumeRegistry
    {
        private readonly List<IVolumeDataSet> _loadedVolumes = new();

        public IReadOnlyList<IVolumeDataSet> LoadedVolumes => _loadedVolumes.AsReadOnly();
        public IVolumeDataSet? ActiveVolume { get; private set; }

        public IVolumeDataSet Active =>
            ActiveVolume ?? throw new InvalidOperationException("No active volume is registered.");

        public bool HasActive => ActiveVolume != null;

        public event Action ActiveVolumeChanged;
        public event Action Changed;

        public void Add(IVolumeDataSet volume)
        {
            if (volume == null)
                throw new ArgumentNullException(nameof(volume));
            if (!_loadedVolumes.Contains(volume))
                _loadedVolumes.Add(volume);
            if (ActiveVolume == null)
                ActiveVolume = volume;
            RaiseChanged(activeChanged: ActiveVolume == volume);
        }

        public bool Remove(IVolumeDataSet volume)
        {
            if (volume == null)
                return false;

            var removed = _loadedVolumes.Remove(volume);
            if (!removed)
                return false;

            var activeChanged = ReferenceEquals(ActiveVolume, volume);
            if (activeChanged)
                ActiveVolume = _loadedVolumes.Count > 0 ? _loadedVolumes[0] : null;

            RaiseChanged(activeChanged);
            return true;
        }

        public void SetActive(IVolumeDataSet volume)
        {
            if (volume == null)
                throw new ArgumentNullException(nameof(volume));
            if (!_loadedVolumes.Contains(volume))
                _loadedVolumes.Add(volume);
            if (ReferenceEquals(ActiveVolume, volume))
                return;
            ActiveVolume = volume;
            RaiseChanged(activeChanged: true);
        }

        public void ClearActive()
        {
            if (ActiveVolume == null)
                return;
            ActiveVolume = null;
            RaiseChanged(activeChanged: true);
        }

        private void RaiseChanged(bool activeChanged)
        {
            Changed?.Invoke();
            if (activeChanged)
                ActiveVolumeChanged?.Invoke();
        }
    }
}
