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
// AFTER — Interface: IRegionController (design-level worked example)
//
// Kernel boundary contract for region selection, cropping and teleport.
// Implementors: RegionControllerService.
// ISP compliance: 7 members — at the limit; split further if sub-teams
// need only crop or only cursor functionality.
// =============================================================================

using DataFeatures;
using UnityEngine;

namespace VolumeData.Refactored
{
    /// <summary>
    /// Contract for managing the interactive region (bounding box selection,
    /// crop, and video-cursor overlay).
    /// <para>
    /// <see cref="RegionControllerService"/> depends on <see cref="ICoordinateMapper"/>
    /// rather than a concrete Transform, enabling headless unit testing of
    /// crop boundary calculations.
    /// </para>
    /// </summary>
    public interface IRegionController
    {
        void SetCursor(Vector3 worldPos, int brushSize);
        void SetVideoCursor(Vector3 worldPos);
        void ClearVideoCursor();

        void SetRegionStart(Vector3 worldPos);
        void SetRegionEnd(Vector3 worldPos);
        void SetRegionBounds(Vector3Int min, Vector3Int max);
        void ClearRegion();

        bool CropToFeature(Feature feature);
        void CropToRegion(Vector3 min, Vector3 max);
        void ResetCrop();
        void TeleportToRegion();
    }
}
