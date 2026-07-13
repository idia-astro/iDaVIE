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
// AFTER — Interface: IMaskController (design-level worked example)
//
// Kernel boundary contract for voxel mask painting.
// Implementors: MaskControllerService.
// ISP compliance: 3 members.
// =============================================================================

namespace VolumeData.Refactored
{
    /// <summary>
    /// Contract for initialising and painting the voxel mask.
    /// Decoupled from rendering so that mask edits can be replayed
    /// or unit-tested without a GPU context.
    /// </summary>
    public interface IMaskController
    {
        /// <summary>Binds the mask controller to a loaded data set.</summary>
        void Initialise(VolumeDataSet dataSet);

        /// <summary>
        /// Paints the mask value at the current cursor voxel and its neighbours
        /// within the active brush radius.
        /// </summary>
        /// <returns><c>true</c> if at least one voxel was successfully painted.</returns>
        bool PaintCursor(short value);

        /// <summary>Flushes the current stroke and triggers moment-map recalculation.</summary>
        void FinishBrushStroke();
    }
}
