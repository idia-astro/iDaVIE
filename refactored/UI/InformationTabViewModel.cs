// SPDX-License-Identifier: LGPL-3.0-or-later
// InformationTabViewModel — ST6 ViewModel for the Information tab of
// CanvassDesktop. File path, HDU selection, subcube bounds, current load
// status. Consumes IVolumeDataSet for header / extents and IVolumeLoader for
// load triggers.

using System;
using iDaVIE.Kernel.Contracts;
using iDaVIE.Kernel.Contracts.Types;

namespace iDaVIE.UI
{
    public sealed class InformationTabViewModel : IDisposable
    {
        public InformationTabViewModel(IVolumeDataSet volume, IVolumeLoader loader)
            => throw new NotImplementedException();

        public string        FilePath      { get; private set; } = "";
        public int           HduIndex      { get; private set; }
        public LoadStatus    LoadStatus    { get; private set; }
        public VolumeExtents Extents       { get; private set; }
        public SubcubeBounds SubcubeBounds { get; private set; }

        public event Action Updated;

        public void OpenFile(string path, int hduIndex) => throw new NotImplementedException();
        public void Close()                             => throw new NotImplementedException();
        public void SetSubcube(SubcubeBounds bounds)    => throw new NotImplementedException();

        public void Dispose() => throw new NotImplementedException();
    }
}
