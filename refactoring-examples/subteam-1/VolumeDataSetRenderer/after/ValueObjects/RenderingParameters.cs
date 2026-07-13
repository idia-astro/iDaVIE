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
// AFTER — Value object: RenderingParameters (design-level worked example)
//
// Groups all shader-level rendering knobs into a single immutable bundle.
// ISO 25010 Analysability: a change to the threshold model touches exactly
// this class and VolumeRenderingService — nothing else.
// =============================================================================

using UnityEngine;

namespace VolumeData.Refactored
{
    /// <summary>
    /// Immutable snapshot of volume rendering parameters passed to the shader.
    /// Constructed by the MonoBehaviour orchestrator from Inspector-serialised fields
    /// and forwarded to <see cref="IVolumeRenderer"/> each frame or on change.
    /// </summary>
    public readonly struct RenderingParameters
    {
        public int MaxSteps { get; }
        public ProjectionMode ProjectionMode { get; }
        public FilterMode TextureFilter { get; }
        public float Jitter { get; }

        public float ThresholdMin { get; }
        public float ThresholdMax { get; }

        public ColorMapEnum ColorMap { get; }
        public ScalingType ScalingType { get; }
        public float ScalingBias { get; }
        public float ScalingContrast { get; }
        public float ScalingAlpha { get; }
        public float ScalingGamma { get; }

        public RenderingParameters(
            int maxSteps, ProjectionMode projectionMode, FilterMode textureFilter, float jitter,
            float thresholdMin, float thresholdMax,
            ColorMapEnum colorMap, ScalingType scalingType,
            float scalingBias, float scalingContrast, float scalingAlpha, float scalingGamma)
        {
            MaxSteps = maxSteps;
            ProjectionMode = projectionMode;
            TextureFilter = textureFilter;
            Jitter = jitter;
            ThresholdMin = thresholdMin;
            ThresholdMax = thresholdMax;
            ColorMap = colorMap;
            ScalingType = scalingType;
            ScalingBias = scalingBias;
            ScalingContrast = scalingContrast;
            ScalingAlpha = scalingAlpha;
            ScalingGamma = scalingGamma;
        }

        /// <summary>Returns a copy with updated threshold bounds.</summary>
        public RenderingParameters WithThresholds(float min, float max) =>
            new RenderingParameters(MaxSteps, ProjectionMode, TextureFilter, Jitter,
                min, max, ColorMap, ScalingType,
                ScalingBias, ScalingContrast, ScalingAlpha, ScalingGamma);

        /// <summary>Returns a copy with a different colour map.</summary>
        public RenderingParameters WithColorMap(ColorMapEnum map) =>
            new RenderingParameters(MaxSteps, ProjectionMode, TextureFilter, Jitter,
                ThresholdMin, ThresholdMax, map, ScalingType,
                ScalingBias, ScalingContrast, ScalingAlpha, ScalingGamma);
    }
}
