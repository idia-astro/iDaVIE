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

// Unity-side adapter — only layer permitted to reference UnityEngine.
// Reads from VolumeDataSetRenderer and MomentMapRenderer.
// Must be called on the Unity main thread.

using iDaVIE.Persistence.Application.Interfaces;
using iDaVIE.Persistence.Domain.Dtos;
using iDaVIE.Persistence.Domain.Serialization;
using UnityEngine;
using VolumeData;

namespace iDaVIE.Persistence.Adapters
{
    /// <summary>
    /// Captures and restores rendering engine state including ray-marching params,
    /// colour mapping, spatial transforms, foveation, mask, and moment maps.
    ///
    /// NOT captured: XDim/YDim/ZDim, Min/Max/Mean/StdDev, Histogram, GPU textures.
    /// Restore priority: Config defaults first, then session state overrides.
    /// </summary>
    public class RenderingStateAdapter : IRenderingStateAdapter
    {
        private readonly VolumeDataSetRenderer _renderer;

        public RenderingStateAdapter(VolumeDataSetRenderer renderer)
        {
            _renderer = renderer;
        }

        public RenderingStateDto Capture()
        {
            var t = _renderer.gameObject.transform;
            var mm = _renderer.GetComponentInChildren<MomentMapRenderer>();

            return new RenderingStateDto
            {
                // Ray-marching
                MaxSteps      = _renderer.MaxSteps,
                ProjectionMode = _renderer.ProjectionMode.ToString(),
                TextureFilter  = _renderer.TextureFilter == FilterMode.Bilinear ? "Bilinear" : "Point",
                Jitter         = _renderer.Jitter,

                // Thresholds
                ThresholdMin = _renderer.ThresholdMin,
                ThresholdMax = _renderer.ThresholdMax,

                // Colour mapping
                ColorMap             = _renderer.ColorMap.ToString(),
                ScalingType          = _renderer.ScalingType.ToString(),
                ScalingBias          = _renderer.ScalingBias,
                ScalingContrast      = _renderer.ScalingContrast,
                ScalingAlpha         = _renderer.ScalingAlpha,
                ScalingGamma         = _renderer.ScalingGamma,
                SelectionSaturateFactor = _renderer.SelectionSaturateFactor,

                // Volume data
                ScaleMax = _renderer.ScaleMax,
                ScaleMin = _renderer.ScaleMin,

                // Spatial transform
                Position = new SerializableVector3(t.localPosition.x, t.localPosition.y, t.localPosition.z),
                Rotation = new SerializableQuaternion(t.localRotation.x, t.localRotation.y, t.localRotation.z, t.localRotation.w),
                Scale    = new SerializableVector3(t.localScale.x, t.localScale.y, t.localScale.z),

                // Rest frequency override
                OverrideRestFrequency   = _renderer.OverrideRestFrequency,
                CustomRestFrequencyGHz  = _renderer.OverrideRestFrequency ? _renderer.RestFrequencyGHz : null,

                // Foveation
                Foveation = new FoveationStateDto
                {
                    FoveatedRendering  = _renderer.FoveatedRendering,
                    FoveationStart     = _renderer.FoveationStart,
                    FoveationEnd       = _renderer.FoveationEnd,
                    FoveationJitter    = _renderer.FoveationJitter,
                    FoveatedStepsLow   = _renderer.FoveatedStepsLow,
                    FoveatedStepsHigh  = _renderer.FoveatedStepsHigh,
                },

                // Mask
                Mask = new MaskStateDto
                {
                    DisplayMask    = _renderer.DisplayMask,
                    MaskMode       = _renderer.MaskMode.ToString(),
                    MaskVoxelSize  = _renderer.MaskVoxelSize,
                    MaskVoxelColor = new SerializableColor(
                        _renderer.MaskVoxelColor.r,
                        _renderer.MaskVoxelColor.g,
                        _renderer.MaskVoxelColor.b,
                        _renderer.MaskVoxelColor.a),
                },

                // Moment maps
                MomentMaps = mm == null ? null : new MomentMapStateDto
                {
                    ColorMapM0         = mm.ColorMapM0.ToString(),
                    ColorMapM1         = mm.ColorMapM1.ToString(),
                    ScalingTypeM0      = mm.ScalingTypeM0.ToString(),
                    ScalingTypeM1      = mm.ScalingTypeM1.ToString(),
                    MomentMapThreshold = mm.MomentMapThreshold,
                    UseMask            = mm.UseMask,
                },
            };
        }

        public void Restore(RenderingStateDto dto)
        {
            // Config defaults are applied first by VolumeDataSetRenderer._startFunc().
            // Session state overrides are applied here after FITS load completes.
            _renderer.ThresholdMin          = dto.ThresholdMin;
            _renderer.ThresholdMax          = dto.ThresholdMax;
            _renderer.MaxSteps              = dto.MaxSteps;
            _renderer.ScalingBias           = dto.ScalingBias;
            _renderer.ScalingContrast       = dto.ScalingContrast;
            _renderer.ScalingAlpha          = dto.ScalingAlpha;
            _renderer.ScalingGamma          = dto.ScalingGamma;
            _renderer.SelectionSaturateFactor = dto.SelectionSaturateFactor;

            if (System.Enum.TryParse<ColorMapEnum>(dto.ColorMap, out var cmap))
                _renderer.ColorMap = cmap;
            if (System.Enum.TryParse<ScalingType>(dto.ScalingType, out var scale))
                _renderer.ScalingType = scale;

            // Spatial transform
            if (dto.Position != null)
                _renderer.gameObject.transform.localPosition = new Vector3(dto.Position.X, dto.Position.Y, dto.Position.Z);
            if (dto.Rotation != null)
                _renderer.gameObject.transform.localRotation = new Quaternion(dto.Rotation.X, dto.Rotation.Y, dto.Rotation.Z, dto.Rotation.W);
            if (dto.Scale != null)
                _renderer.gameObject.transform.localScale = new Vector3(dto.Scale.X, dto.Scale.Y, dto.Scale.Z);

            // Rest frequency
            if (dto.OverrideRestFrequency && dto.CustomRestFrequencyGHz.HasValue)
            {
                _renderer.OverrideRestFrequency = true;
                _renderer.RestFrequencyGHz = dto.CustomRestFrequencyGHz.Value;
            }

            // Foveation
            if (dto.Foveation != null)
            {
                _renderer.FoveatedRendering = dto.Foveation.FoveatedRendering;
                _renderer.FoveationStart    = dto.Foveation.FoveationStart;
                _renderer.FoveationEnd      = dto.Foveation.FoveationEnd;
                _renderer.FoveationJitter   = dto.Foveation.FoveationJitter;
                _renderer.FoveatedStepsLow  = dto.Foveation.FoveatedStepsLow;
                _renderer.FoveatedStepsHigh = dto.Foveation.FoveatedStepsHigh;
            }

            // Mask
            if (dto.Mask != null)
            {
                _renderer.DisplayMask = dto.Mask.DisplayMask;
                if (System.Enum.TryParse<MaskMode>(dto.Mask.MaskMode, out var mm))
                    _renderer.MaskMode = mm;
                _renderer.MaskVoxelSize = dto.Mask.MaskVoxelSize;
                if (dto.Mask.MaskVoxelColor != null)
                    _renderer.MaskVoxelColor = new Color(
                        dto.Mask.MaskVoxelColor.R,
                        dto.Mask.MaskVoxelColor.G,
                        dto.Mask.MaskVoxelColor.B,
                        dto.Mask.MaskVoxelColor.A);
            }

            // Moment maps
            if (dto.MomentMaps != null)
            {
                var momentRenderer = _renderer.GetComponentInChildren<MomentMapRenderer>();
                if (momentRenderer != null)
                {
                    if (System.Enum.TryParse<ColorMapEnum>(dto.MomentMaps.ColorMapM0, out var cm0))
                        momentRenderer.ColorMapM0 = cm0;
                    if (System.Enum.TryParse<ColorMapEnum>(dto.MomentMaps.ColorMapM1, out var cm1))
                        momentRenderer.ColorMapM1 = cm1;
                    if (System.Enum.TryParse<ScalingType>(dto.MomentMaps.ScalingTypeM0, out var st0))
                        momentRenderer.ScalingTypeM0 = st0;
                    if (System.Enum.TryParse<ScalingType>(dto.MomentMaps.ScalingTypeM1, out var st1))
                        momentRenderer.ScalingTypeM1 = st1;
                    momentRenderer.MomentMapThreshold = dto.MomentMaps.MomentMapThreshold;
                    momentRenderer.UseMask            = dto.MomentMaps.UseMask;
                }
            }

            Debug.Log("[Persistence] RenderingState restored.");
        }
    }
}
