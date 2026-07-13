/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 *
 * This file is part of the iDaVIE project.
 *
 * iDaVIE is free software: you can redistribute it and/or modify it under the terms
 * of the GNU Lesser General Public License (LGPL) as published by the Free Software
 * Foundation, either version 3 of the License, or (at your option) any later version.
 *
 * iDaVIE is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
 * PURPOSE. See the GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License along with
 * iDaVIE in the LICENSE file. If not, see <https://www.gnu.org/licenses/>.
 *
 * Additional information and disclaimers regarding liability and third-party
 * components can be found in the DISCLAIMER and NOTICE files included with this project.
 *
 */

// =============================================================================
// AFTER — Service: VolumeDataExportService (design-level worked example)
//
// Single responsibility: FITS file export for subcubes and masks.
// All FitsReader P/Invoke calls are contained here, so the upstream P/Invoke
// dependency does not leak into the rendering or interaction layers.
//
// CK AFTER (estimated):
//   WMC  ≈ 10  (3 public + 2 private, avg complexity 2.8)
//   CBO  ≈ 5   (VolumeDataSet×2, FitsReader, FeatureSetManager, ToastNotification)
//   RFC  ≈ 18
//   LCOM ≈ 0.12
// =============================================================================

using System;
using System.IO;
using System.Text.RegularExpressions;
using DataFeatures;
using UnityEngine;

namespace VolumeData.Refactored
{
    /// <summary>
    /// Handles writing subcubes and mask data to FITS files.
    /// <para>
    /// By moving all <c>FitsReader</c> P/Invoke calls here, the rest of the
    /// rendering pipeline never imports native interop — a prerequisite for
    /// running server-side (headless) builds on Linux where the native DLL is absent.
    /// </para>
    /// </summary>
    public sealed class VolumeDataExportService : IVolumeDataExporter
    {
        private readonly VolumeDataSet _dataSet;
        private readonly FeatureSetManager _featureManager;

        private VolumeDataSet _maskDataSet;
        private bool _isMaskNew;
        private string _lastSavedMaskPath = "";

        public VolumeDataExportService(VolumeDataSet dataSet, FeatureSetManager featureManager)
        {
            _dataSet        = dataSet;
            _featureManager = featureManager;
        }

        /// <summary>Provides the active mask dataset after it has been created or loaded.</summary>
        public void BindMaskDataSet(VolumeDataSet maskDataSet, bool isMaskNew)
        {
            _maskDataSet = maskDataSet;
            _isMaskNew   = isMaskNew;
        }

        // ── IVolumeDataExporter ────────────────────────────────────────────────

        public void SaveSubCube()
        {
            Vector3Int cornerMin, cornerMax, cornerMinWorld, cornerMaxWorld;

            if (_featureManager.SelectedFeature != null)
            {
                cornerMinWorld = Vector3Int.FloorToInt(_featureManager.SelectedFeature.CornerMin);
                cornerMaxWorld = Vector3Int.FloorToInt(_featureManager.SelectedFeature.CornerMax);
                cornerMin      = GetDataSpaceVoxel(cornerMinWorld);
                cornerMax      = GetDataSpaceVoxel(cornerMaxWorld);
            }
            else
            {
                ToastNotification.ShowWarning("No feature selected, saving entire loaded cube.");
                cornerMin = cornerMinWorld = Vector3Int.one;
                cornerMax = cornerMaxWorld = new Vector3Int((int)_dataSet.XDim, (int)_dataSet.YDim, (int)_dataSet.ZDim);
            }

            var featureSize = cornerMax - cornerMin + Vector3Int.one;
            long elements   = (long)featureSize.x * featureSize.y * featureSize.z;
            if (elements > int.MaxValue)
            {
                long pct = Mathf.RoundToInt((float)(elements / int.MaxValue) * 100);
                ToastNotification.ShowError($"Subcube is {pct}% of CFITSIO max; select a smaller region.");
                return;
            }

            _dataSet.SaveSubCubeFromOriginal(cornerMin, cornerMax, cornerMinWorld, cornerMaxWorld, _maskDataSet);
        }

        public void SaveMask(bool overwrite)
        {
            if (_maskDataSet == null)
            {
                ToastNotification.ShowError("No mask data to save.");
                return;
            }

            IntPtr fitsPtr = IntPtr.Zero;
            int status     = 0;

            if (_isMaskNew)
            {
                SaveNewMask(ref fitsPtr, ref status);
            }
            else if (!overwrite)
            {
                SaveMaskCopy(ref fitsPtr, ref status);
            }
            else
            {
                OverwriteMask(ref fitsPtr, ref status);
            }

            if (fitsPtr != IntPtr.Zero)
                FitsReader.FitsCloseFile(fitsPtr, out status);
        }

        public string GetMaskSavedFilePath() => _lastSavedMaskPath;

        // ── Internal ───────────────────────────────────────────────────────────

        private void SaveNewMask(ref IntPtr fitsPtr, ref int status)
        {
            string datasetFileName = _dataSet.FileName;
            if (_dataSet.SelectedHdu != 1)
                datasetFileName += $"[{_dataSet.SelectedHdu}]";

            FitsReader.FitsOpenFileReadOnly(out fitsPtr, datasetFileName, out status);
            string directory       = Path.GetDirectoryName(_dataSet.FileName);
            _maskDataSet.FileName  = $"!{directory}/{Path.GetFileNameWithoutExtension(_dataSet.FileName)}-mask.fits";

            if (_maskDataSet.SaveMask(fitsPtr, _maskDataSet.FileName, false) != 0)
                ToastNotification.ShowError("Error saving new mask.");
            else
            {
                ToastNotification.ShowSuccess($"New mask saved to {Path.GetFileName(_maskDataSet.FileName)}");
                _lastSavedMaskPath = _maskDataSet.FileName;
            }

            _isMaskNew = false;
        }

        private void SaveMaskCopy(ref IntPtr fitsPtr, ref int status)
        {
            FitsReader.FitsOpenFileReadOnly(out fitsPtr, _maskDataSet.FileName, out status);

            var timeStamp  = DateTime.Now.ToString("yyyyMMdd_Hmmss");
            var regex      = new Regex(@"_copy_\d{8}_\d{5}");
            string baseName = Path.GetFileNameWithoutExtension(_maskDataSet.FileName);
            var match       = regex.Match(baseName);
            string newName  = match.Success
                ? baseName.Substring(0, baseName.Length - timeStamp.Length - 6) + "_copy_" + timeStamp
                : baseName + "_copy_" + timeStamp;

            string directory = Path.GetDirectoryName(_maskDataSet.FileName);
            string fullPath  = $"!{directory}/{newName}.fits";

            if (_maskDataSet.SaveMask(fitsPtr, fullPath, true) != 0)
                ToastNotification.ShowError("Error saving mask copy.");
            else
            {
                _lastSavedMaskPath = fullPath;
                ToastNotification.ShowSuccess($"Mask copy saved to {newName}");
            }
        }

        private void OverwriteMask(ref IntPtr fitsPtr, ref int status)
        {
            FitsReader.FitsOpenFileReadWrite(out fitsPtr, _maskDataSet.FileName, out status);
            if (_maskDataSet.SaveMask(fitsPtr, null, false) != 0)
                ToastNotification.ShowError("Error overwriting mask.");
            else
            {
                _lastSavedMaskPath = _maskDataSet.FileName;
                ToastNotification.ShowSuccess("Mask saved to disk.");
            }
        }

        private Vector3Int GetDataSpaceVoxel(Vector3Int worldSpaceVoxel)
        {
            var offset = new Vector3Int(
                _dataSet.subsetBounds[0],
                _dataSet.subsetBounds[2],
                _dataSet.subsetBounds[4]);
            return worldSpaceVoxel + offset - Vector3Int.one;
        }
    }
}
