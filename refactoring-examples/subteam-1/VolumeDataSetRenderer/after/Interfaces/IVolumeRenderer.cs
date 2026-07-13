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
// AFTER — Interface: IVolumeRenderer (design-level worked example)
//
// Kernel boundary contract between the MonoBehaviour orchestrator and the
// concrete shader/material pipeline. Implementors: VolumeRenderingService.
// Test doubles: MockVolumeRenderer (unit tests), NullVolumeRenderer (headless CI).
//
// ISP compliance: 6 members — within the ≤7 limit.
// =============================================================================

namespace VolumeData.Refactored
{
    /// <summary>
    /// Contract for applying volumetric rendering parameters to the underlying
    /// GPU material pipeline.  All parameter mutation goes through value objects
    /// so callers never hold raw Material references.
    /// </summary>
    public interface IVolumeRenderer
    {
        void ApplyRenderingParameters(RenderingParameters parameters);
        void ApplyMaskParameters(MaskParameters parameters);
        void ApplyFoveationParameters(FoveationParameters parameters);
        void ApplyVignetteParameters(VignetteParameters parameters);

        /// <summary>Regenerates the downsampled GPU texture after resolution/crop change.</summary>
        void RegenerateCubes();

        /// <summary>Cycles the active colour map by <paramref name="delta"/> steps.</summary>
        void ShiftColorMap(int delta);
    }
}
