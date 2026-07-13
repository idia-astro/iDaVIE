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
using iDaVIE.Domain.Feature;

namespace DataFeatures
{
    // Feature is the basic unit of marking up the volume
    public partial class Feature : IFeature
    {
        public bool Temporary;
        public string Comment;
        public float Metric;
        public int Index { get; set; }
        public int Id { get; }
        public string Name { get; }

        public string Flag { get; set; }
        private bool _selected;
        private FeatureColor _color;
        private bool _active;
        private Vec3[] _corners = new Vec3[2];
        public string[] RawData { get; set; }
        public FeatureSet FeatureSetParent { get; private set; }

        public void SetParent(FeatureSet parent) => FeatureSetParent = parent;
        public void ClearParent() => FeatureSetParent = null;

        public bool StatusChanged;

        public Feature(Vec3 cornerMin, Vec3 cornerMax, FeatureColor color, string name, string flag, int index, int id, string[] rawData, bool visible)
        {
            Index = index;
            Id = id;
            _color = color;
            Name = name;
            Flag = flag;
            SetBounds(cornerMin, cornerMax);
            RawData = rawData;
            Visible = visible;
        }

        public Vec3 CornerMin => Vec3.Min(_corners[0], _corners[1]);

        public Vec3 CornerMax => Vec3.Max(_corners[0], _corners[1]);

        public Vec3 Center
        {
            get => (_corners[0] + _corners[1]) / 2.0f;
            set
            {
                var diff = value - Center;
                _corners[0] = _corners[0] + diff;
                _corners[1] = _corners[1] + diff;
                NotifyDirty();
            }
        }

        public Vec3 Size
        {
            //  Size is padded by one, because the bounding box includes both the min and max voxels
            get => (Vec3.Max(_corners[0], _corners[1]) - Vec3.Min(_corners[0], _corners[1]) + Vec3.One);
            set
            {
                var currentCenter = Center;
                _corners[0] = currentCenter - value / 2.0f;
                _corners[1] = currentCenter + value / 2.0f;
                NotifyDirty();
            }
        }

        public float Volume
        {
            get
            {
                var s = Size;
                return s.X * s.Y * s.Z;
            }
        }

        public bool ContainsPoint(Vec3 point)
        {
            var min = CornerMin;
            var max = CornerMax;
            return point.X >= min.X && point.X <= max.X
                && point.Y >= min.Y && point.Y <= max.Y
                && point.Z >= min.Z && point.Z <= max.Z;
        }

        public FeatureColor CubeColor
        {
            get => _color;
            set
            {
                if (_color != value)
                    FeatureSetParent?.NotifyDirty(Index);
                _color = value;
            }
        }

        public bool Visible
        {
            get => _active;
            set
            {
                if (_active != value)
                    FeatureSetParent?.NotifyDirty(Index);
                _active = value;
            }
        }

        public bool Selected
        {
            get => _selected;
            set
            {
                if (_selected != value)
                    FeatureSetParent?.NotifyDirty(Index);
                _selected = value;
            }
        }

        public void SetBounds(Vec3 cornerMin, Vec3 cornerMax)
        {
            _corners[0] = cornerMin;
            _corners[1] = cornerMax;
            NotifyDirty();
        }

        public Vec3 GetMinBounds() => _corners[0];

        public Vec3 GetMaxBounds() => _corners[1];

        private void NotifyDirty()
        {
            FeatureSetParent?.NotifyDirty(Index);
        }

        // Service-layer API aliases
        public bool  BoundsContains(Vec3 point) => ContainsPoint(point);
        public float BoundsVolume()             => Volume;

        /// <summary>
        /// Returns true if this feature's center lies outside [volumeMin, volumeMax].
        /// Matches the check in FeatureSetRenderer.FeatureIsWithinVolume (which uses
        /// Data.XDim/YDim/ZDim as the max bounds with an implicit min of 0).
        /// </summary>
        public bool IsOutsideVolume(Vec3 volumeMin, Vec3 volumeMax) =>
            Center.X < volumeMin.X || Center.X > volumeMax.X ||
            Center.Y < volumeMin.Y || Center.Y > volumeMax.Y ||
            Center.Z < volumeMin.Z || Center.Z > volumeMax.Z;
    }
}