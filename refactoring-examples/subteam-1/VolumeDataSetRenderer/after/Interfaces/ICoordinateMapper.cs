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
// AFTER — Interface: ICoordinateMapper (design-level worked example)
//
// Kernel boundary contract for world ↔ volume coordinate transforms.
// Implementors: VolumeCoordinateMapper.
// Consumed by: RegionControllerService, MaskControllerService, menus.
// ISP compliance: 5 members.
// =============================================================================

using UnityEngine;

namespace VolumeData.Refactored
{
    /// <summary>
    /// Contract for converting between world space, object (local) space,
    /// and voxel-index (volume) space.
    /// <para>
    /// Extracting this into an interface removes the direct Transform coupling
    /// from every caller and allows the mapping logic to be tested without
    /// a Unity scene.
    /// </para>
    /// </summary>
    public interface ICoordinateMapper
    {
        /// <summary>Converts a world-space position to volume (voxel-fraction) space.</summary>
        Vector3 WorldToVolume(Vector3 worldPos);

        /// <summary>Converts a world-space rotation to volume-space rotation.</summary>
        Quaternion WorldRotationToVolume(Quaternion worldRot);

        /// <summary>Converts a volume-space position to object (local) space.</summary>
        Vector3 VolumeToLocal(Vector3 volumePos);

        /// <summary>Converts an object (local) space position to volume space.</summary>
        Vector3 LocalToVolume(Vector3 localPos);

        /// <summary>
        /// Returns the integer voxel index at the given world-space position,
        /// clamped to the cube bounds.
        /// </summary>
        Vector3Int GetVoxelAtWorldPos(Vector3 worldPos);
    }
}
