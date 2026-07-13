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
// AFTER — Value object: FoveationParameters (design-level worked example)
// =============================================================================

namespace VolumeData.Refactored
{
    /// <summary>
    /// Immutable bundle of foveated-rendering parameters.
    /// Isolating foveation from the main <see cref="RenderingParameters"/> means
    /// eye-tracking integration only touches this type and <see cref="IVolumeRenderer"/>.
    /// </summary>
    public readonly struct FoveationParameters
    {
        public bool Enabled { get; }
        public float Start { get; }
        public float End { get; }
        public float Jitter { get; }
        public int StepsLow { get; }
        public int StepsHigh { get; }

        public FoveationParameters(bool enabled, float start, float end,
                                   float jitter, int stepsLow, int stepsHigh)
        {
            Enabled = enabled;
            Start = start;
            End = end;
            Jitter = jitter;
            StepsLow = stepsLow;
            StepsHigh = stepsHigh;
        }
    }
}
