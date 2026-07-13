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
// AFTER — Service: VolumeCoordinateMapper (design-level worked example)
//
// Single responsibility: coordinate space conversions between world, local,
// and voxel-index spaces.
//
// CK AFTER (estimated):
//   WMC  ≈ 10  (5 public + 1 constructor, avg complexity 1.8)
//   CBO  ≈ 3   (Transform, VolumeDataSet, UnityEngine.Mathf)
//   RFC  ≈ 12
//   LCOM ≈ 0.05  (all methods use _transform + _dataSet)
// =============================================================================

using UnityEngine;

namespace VolumeData.Refactored
{
    /// <summary>
    /// Implements all coordinate-space conversions for a loaded volume.
    /// <para>
    /// Takes a <see cref="Transform"/> reference at construction time so that
    /// the conversion logic can be replaced or mocked in tests without a scene.
    /// The <paramref name="cubeDepthAxis"/> parameter encodes which Unity axis
    /// corresponds to the FITS spectral axis (varies per dataset).
    /// </para>
    /// </summary>
    public sealed class VolumeCoordinateMapper : ICoordinateMapper
    {
        private readonly Transform _transform;
        private readonly VolumeDataSet _dataSet;
        private readonly int _cubeDepthAxis;

        public VolumeCoordinateMapper(Transform transform, VolumeDataSet dataSet, int cubeDepthAxis)
        {
            _transform     = transform;
            _dataSet       = dataSet;
            _cubeDepthAxis = cubeDepthAxis;
        }

        // ── ICoordinateMapper ──────────────────────────────────────────────────

        /// <inheritdoc/>
        public Vector3 WorldToVolume(Vector3 worldPos)
        {
            return _transform.InverseTransformPoint(worldPos);
        }

        /// <inheritdoc/>
        public Quaternion WorldRotationToVolume(Quaternion worldRot)
        {
            return Quaternion.Inverse(_transform.rotation) * worldRot;
        }

        /// <inheritdoc/>
        public Vector3 VolumeToLocal(Vector3 volumePos)
        {
            return new Vector3(
                volumePos.x / _dataSet.XDim - 0.5f,
                volumePos.y / _dataSet.YDim - 0.5f,
                volumePos.z / _dataSet.ZDim - 0.5f);
        }

        /// <inheritdoc/>
        public Vector3 LocalToVolume(Vector3 localPos)
        {
            return new Vector3(
                (localPos.x + 0.5f) * _dataSet.XDim,
                (localPos.y + 0.5f) * _dataSet.YDim,
                (localPos.z + 0.5f) * _dataSet.ZDim);
        }

        /// <inheritdoc/>
        public Vector3Int GetVoxelAtWorldPos(Vector3 worldPos)
        {
            var local = _transform.InverseTransformPoint(worldPos);
            local = new Vector3(
                Mathf.Clamp(local.x, -0.5f, 0.5f),
                Mathf.Clamp(local.y, -0.5f, 0.5f),
                Mathf.Clamp(local.z, -0.5f, 0.5f));
            var cubeSpace  = LocalToVolume(local);
            var cornerFloor = new Vector3(Mathf.Floor(cubeSpace.x), Mathf.Floor(cubeSpace.y), Mathf.Floor(cubeSpace.z));
            return new Vector3Int(
                Mathf.Clamp(Mathf.RoundToInt(cornerFloor.x) + 1, 1, (int)_dataSet.XDim),
                Mathf.Clamp(Mathf.RoundToInt(cornerFloor.y) + 1, 1, (int)_dataSet.YDim),
                Mathf.Clamp(Mathf.RoundToInt(cornerFloor.z) + 1, 1, (int)_dataSet.ZDim));
        }
    }
}
