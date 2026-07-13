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
// AFTER — Value object: MaskParameters (design-level worked example)
// =============================================================================

using UnityEngine;

namespace VolumeData.Refactored
{
    /// <summary>
    /// Immutable bundle of mask-overlay rendering parameters.
    /// Separating mask from volume parameters means mask colour changes
    /// do not require touching the main rendering pipeline.
    /// </summary>
    public readonly struct MaskParameters
    {
        public MaskMode Mode { get; }
        public float VoxelSize { get; }
        public Color VoxelColor { get; }
        public bool DisplayMask { get; }

        public MaskParameters(MaskMode mode, float voxelSize, Color voxelColor, bool displayMask)
        {
            Mode = mode;
            VoxelSize = voxelSize;
            VoxelColor = voxelColor;
            DisplayMask = displayMask;
        }
    }
}
