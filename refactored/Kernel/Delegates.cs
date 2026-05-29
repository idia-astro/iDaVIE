// SPDX-License-Identifier: LGPL-3.0-or-later
// Delegates — central declaration site for cross-cutting event delegates (M-15).
// Replaces Assets/Scripts/Tools/Delegates.cs (28 LOC). New delegate types require
// ADR-002 sign-off per global_model.md §1 ST1.

using iDaVIE.Kernel.Contracts.Types;   // CartesianCoord, SubcubeBounds

namespace iDaVIE.Kernel
{
    public delegate void DatasetLoadedHandler();
    public delegate void DatasetUnloadedHandler();
    public delegate void SubcubeChangedHandler(SubcubeBounds bounds);
    public delegate void RestFrequencyChangedHandler(double restFrequencyGHz);
    public delegate void ConfigChangedHandler();
    public delegate void CursorMovedHandler(CartesianCoord voxel);
}
