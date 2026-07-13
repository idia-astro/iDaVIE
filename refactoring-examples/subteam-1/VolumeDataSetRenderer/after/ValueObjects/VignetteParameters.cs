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
// AFTER — Value object: VignetteParameters (design-level worked example)
// =============================================================================

using UnityEngine;

namespace VolumeData.Refactored
{
    /// <summary>
    /// Immutable bundle of vignette (tunnelling) rendering parameters.
    /// Keeping vignette separate means comfort-setting changes are localised
    /// to this type and the shader pass, not scattered across a 1400-line class.
    /// </summary>
    public readonly struct VignetteParameters
    {
        public float FadeStart { get; }
        public float FadeEnd { get; }
        public float Intensity { get; }
        public Color Color { get; }

        public VignetteParameters(float fadeStart, float fadeEnd, float intensity, Color color)
        {
            FadeStart = fadeStart;
            FadeEnd = fadeEnd;
            Intensity = intensity;
            Color = color;
        }
    }
}
