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
 */

namespace iDaVIE.Persistence.Domain.Dtos
{
    /// <summary>
    /// Persistent state for the Data I/O sub-system.
    /// Source fields: VolumeDataSet.FileName, SelectedHdu, subsetBounds, and AstFrameSet attributes.
    /// NOT persisted: IntPtr handles (FitsData, FitsHeader, AstFrameSet, AstAltSpecSet) — rebuilt on restore.
    /// </summary>
    public class DataIoStateDto
    {
        /// <summary>Absolute path to the primary FITS image.</summary>
        [PersistField]
        public string FileName { get; set; }

        /// <summary>Path to the mask FITS file, if any.</summary>
        [PersistField(Optional = true)]
        public string MaskFileName { get; set; }

        /// <summary>User crop region [xMin, xMax, yMin, yMax, zMin, zMax] (1-indexed).</summary>
        [PersistField]
        public int[] SubsetBounds { get; set; }

        /// <summary>FITS HDU index (1-indexed).</summary>
        [PersistField]
        public int SelectedHdu { get; set; } = 1;

        /// <summary>For 4-D cubes: index of dimension shown on Z-axis (2 or 3).</summary>
        [PersistField(Optional = true)]
        public int? Index2 { get; set; }

        /// <summary>For 4-D cubes: slice index within the 4th dimension.</summary>
        [PersistField(Optional = true)]
        public int? SliceDim { get; set; }

        /// <summary>Primary AST spectral system (e.g. "FREQ", "VRAD").</summary>
        [PersistField]
        public string PrimarySpectralSystem { get; set; }

        /// <summary>Target alternative spectral system.</summary>
        [PersistField]
        public string AlternativeSpectralTarget { get; set; }

        /// <summary>Target alternative spectral unit (e.g. "km/s", "Hz").</summary>
        [PersistField]
        public string AlternativeSpectralUnit { get; set; }

        /// <summary>AST rest-frame label (e.g. "Heliocentric").</summary>
        [PersistField]
        public string StandardOfRest { get; set; }

        /// <summary>
        /// Numeric transformed spectral coordinate.
        /// Revalidated against the restored AST frame after load.
        /// </summary>
        [PersistField]
        public double TransformedSpectralValue { get; set; }
    }
}
