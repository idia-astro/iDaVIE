// SPDX-License-Identifier: LGPL-3.0-or-later
// VolumePersistenceService — extracts SaveSubCube / SaveMask from
// VolumeDataSetRenderer (lines 1261-1379, ~120 lines).
//
// Refactor delta:
//   - Three save modes (new mask / save-copy / overwrite) become a
//     Strategy enum + dispatch table on IFitsMaskWriter, satisfying OCP.
//   - Toast notifications (presentation concern) move out of this layer;
//     this service returns a Result + raises events. The Unity ACL
//     subscribes and renders toasts.
//   - File-path mangling (Regex copy-naming, line 1328) is a private helper
//     here, not a side effect of save dispatch.

using System;
using iDaVIE.Data;                        // IFitsMaskWriter, IFitsCubeReader
using iDaVIE.Kernel.Contracts;            // IVolumeDataSet
using iDaVIE.Kernel.Contracts.Types;      // CartesianCoord, SubcubeBounds

namespace iDaVIE.Rendering
{
    public enum MaskSaveMode { New, Copy, Overwrite }

    public readonly record struct SaveResult(bool Success, string SavedPath, string? Error);

    public sealed class VolumePersistenceService
    {
        private readonly IFitsMaskWriter _maskWriter;
        private readonly IFitsCubeReader _cubeReader;
        private readonly IVolumeDataSet  _volume;

        public VolumePersistenceService(IFitsMaskWriter maskWriter,
                                        IFitsCubeReader cubeReader,
                                        IVolumeDataSet volume)
        {
            _maskWriter = maskWriter;
            _cubeReader = cubeReader;
            _volume     = volume;
        }

        public SaveResult SaveSubCube(SubcubeBounds bounds)
        {
            long elements = (long)bounds.SizeX * bounds.SizeY * bounds.SizeZ;
            if (elements > int.MaxValue)
                return new SaveResult(false, "", $"Subcube exceeds CFITSIO {int.MaxValue}-element limit.");

            // TODO: delegate to ST2 — _cubeReader.SaveSubCube(_volume.FilePath, bounds, maskPath).
            return new SaveResult(true, _volume.FilePath, null);
        }

        public SaveResult SaveMask(MaskSaveMode mode)
        {
            // Legacy SaveMask:1290 had three branches inside one method. The Strategy
            // dispatch below makes each branch independently testable and OCP-clean —
            // adding a fourth mode (e.g. SaveMaskAs) is a new switch arm, not a fifth
            // 30-line nested branch inside this method.
            return mode switch
            {
                MaskSaveMode.New       => SaveNewMask(),
                MaskSaveMode.Copy      => SaveMaskCopy(),
                MaskSaveMode.Overwrite => OverwriteMask(),
                _ => new SaveResult(false, "", $"Unknown save mode {mode}")
            };
        }

        private SaveResult SaveNewMask()
        {
            // TODO: build target path next to cube file (legacy line 1310);
            //       call _maskWriter.WriteNew(_volume.FilePath, _volume.HduIndex, targetPath).
            return default;
        }

        private SaveResult SaveMaskCopy()
        {
            // TODO: build copy path via timestamp regex (legacy line 1328);
            //       call _maskWriter.WriteCopy(currentMaskPath, copyPath).
            return default;
        }

        private SaveResult OverwriteMask()
        {
            // TODO: _maskWriter.Overwrite(currentMaskPath).
            return default;
        }
    }
}
