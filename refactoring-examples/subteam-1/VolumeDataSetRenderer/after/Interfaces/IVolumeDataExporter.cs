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
// AFTER — Interface: IVolumeDataExporter (design-level worked example)
//
// Kernel boundary contract for FITS file export.
// Implementors: VolumeDataExportService.
// ISP compliance: 3 members.
// =============================================================================

namespace VolumeData.Refactored
{
    /// <summary>
    /// Contract for exporting volume and mask data to FITS files.
    /// <para>
    /// Keeping I/O behind an interface means the desktop GUI (Sub-team 6)
    /// depends only on this contract, not on the rendering MonoBehaviour,
    /// and export logic can be unit-tested with a stub.
    /// </para>
    /// </summary>
    public interface IVolumeDataExporter
    {
        /// <summary>Writes the currently selected subcube region to a new FITS file.</summary>
        void SaveSubCube();

        /// <summary>
        /// Saves the voxel mask.
        /// <paramref name="overwrite"/> = <c>true</c> overwrites the existing mask file;
        /// <c>false</c> creates a timestamped copy.
        /// </summary>
        void SaveMask(bool overwrite);

        /// <summary>Returns the file path used by the most recent successful mask save.</summary>
        string GetMaskSavedFilePath();
    }
}
