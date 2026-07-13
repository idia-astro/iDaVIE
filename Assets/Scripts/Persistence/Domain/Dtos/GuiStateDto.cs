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
    /// Minimal GUI-only persistent state.
    /// All other "GUI" fields (FileName, SelectedHdu, SubsetBounds, MaskFileName, trueBounds)
    /// are authoritative in DataIoStateDto and must not be duplicated here.
    /// </summary>
    public class GuiStateDto
    {
        /// <summary>
        /// The axis used as the depth/Z axis in the cube view (0-indexed).
        /// Default 2 = standard FITS Z axis. Clamped to [0,2] by WorkspaceValidator.
        /// </summary>
        [PersistField]
        public int CubeDepthAxis { get; set; } = 2;
    }
}
