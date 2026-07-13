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

using iDaVIE.Persistence.Domain.Serialization;

namespace iDaVIE.Persistence.Domain.Dtos
{
    /// <summary>
    /// Persistent state for the Rendering Engine.
    /// Source: VolumeDataSetRenderer fields and MomentMapRenderer.
    /// NOT persisted: XDim/YDim/ZDim, Min/Max/Mean/StdDev, Histogram, GPU textures/compute buffers.
    /// </summary>
    public class RenderingStateDto
    {
        // ── Ray-marching ────────────────────────────────────────────────────────
        [PersistField] public int    MaxSteps       { get; set; } = 192;
        [PersistField] public string ProjectionMode  { get; set; }
        /// <summary>"Point" or "Bilinear".</summary>
        [PersistField] public string TextureFilter   { get; set; }
        [PersistField] public float  Jitter          { get; set; } = 1.0f;

        // ── Thresholds ───────────────────────────────────────────────────────────
        [PersistField] public float ThresholdMin { get; set; }
        [PersistField] public float ThresholdMax { get; set; } = 1f;

        // ── Colour mapping ───────────────────────────────────────────────────────
        [PersistField] public string ColorMap              { get; set; }
        [PersistField] public string ScalingType           { get; set; }
        [PersistField] public float  ScalingBias           { get; set; }
        [PersistField] public float  ScalingContrast       { get; set; } = 1f;
        [PersistField] public float  ScalingAlpha          { get; set; } = 1000f;
        [PersistField] public float  ScalingGamma          { get; set; } = 1f;
        [PersistField] public float  SelectionSaturateFactor { get; set; } = 0.7f;

        // ── Volume data state ────────────────────────────────────────────────────
        [PersistField] public float ScaleMax { get; set; }
        [PersistField] public float ScaleMin { get; set; }

        // ── Spatial transform ────────────────────────────────────────────────────
        [PersistField] public SerializableVector3    Position { get; set; }
        [PersistField] public SerializableQuaternion Rotation { get; set; }
        [PersistField] public SerializableVector3    Scale    { get; set; }

        // ── Rest frequency override ──────────────────────────────────────────────
        [PersistField]                  public bool    OverrideRestFrequency  { get; set; }
        [PersistField(Optional = true)] public double? CustomRestFrequencyGHz { get; set; }

        // ── Optional sub-sections ───────────────────────────────────────────────
        [PersistField(Optional = true)] public FoveationStateDto  Foveation  { get; set; }
        [PersistField(Optional = true)] public MaskStateDto       Mask       { get; set; }
        [PersistField(Optional = true)] public MomentMapStateDto  MomentMaps { get; set; }
    }

    /// <summary>Foveated rendering configuration.</summary>
    public class FoveationStateDto
    {
        [PersistField] public bool  FoveatedRendering  { get; set; }
        [PersistField] public float FoveationStart     { get; set; } = 0.15f;
        [PersistField] public float FoveationEnd       { get; set; } = 0.40f;
        [PersistField] public float FoveationJitter    { get; set; }
        [PersistField] public int   FoveatedStepsLow   { get; set; } = 64;
        [PersistField] public int   FoveatedStepsHigh  { get; set; } = 384;
    }

    /// <summary>Mask rendering configuration.</summary>
    public class MaskStateDto
    {
        [PersistField] public bool             DisplayMask    { get; set; }
        [PersistField] public string           MaskMode       { get; set; }
        [PersistField] public float            MaskVoxelSize  { get; set; } = 1f;
        [PersistField] public SerializableColor MaskVoxelColor { get; set; }
    }

    /// <summary>Moment map rendering configuration.</summary>
    public class MomentMapStateDto
    {
        [PersistField] public string ColorMapM0         { get; set; }
        [PersistField] public string ColorMapM1         { get; set; }
        [PersistField] public string ScalingTypeM0      { get; set; }
        [PersistField] public string ScalingTypeM1      { get; set; }
        [PersistField] public float  MomentMapThreshold { get; set; }
        [PersistField] public bool   UseMask            { get; set; } = true;
    }
}
