/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 Inter-University Institute for Data Intensive Astronomy
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
using LineRenderer;
using UnityEngine;

namespace DataFeatures
{
    // Feature is the basic unit of marking up the volume
    public class Feature
    {
        public bool Temporary;
        public string Comment;
        public float Metric;
        public int Index { get; set; }
        public int Id { get; }
        public string Name { get; }

        public string Flag { get; set; }
        private bool _selected;
        private Color _color;
        private bool _active;
        private Bounds _unityBounds;
        private Vector3 _position;
        private Vector3[] _corners = new Vector3[2];
        public string[] RawData { get; set; }
        public FeatureSetRenderer FeatureSetParent { get; set; }

        public bool StatusChanged;

        public Feature(Vector3 cubeMin, Vector3 cubeMax, Color cubeColor, string name, string flag, int index, int id, string[] rawData, bool startVisible)
        {
            FeatureSetParent = null;
            Index = index;
            Id = id;
            _color = cubeColor;
            Name = name;
            Flag = flag;
            SetBounds(cubeMin, cubeMax);
            RawData = rawData;
            Visible = startVisible;
        }

        public void ShowAxes(bool show)
        {
            // TODO: Handle this
            // SetCubeColors(_boundingBox, _boundingBox.color, show);
        }

        public Bounds UnityBounds => _unityBounds;

        public Vector3 CornerMin => Vector3.Min(_corners[0], _corners[1]);

        public Vector3 CornerMax => Vector3.Max(_corners[0], _corners[1]);

        public Vector3 Center
        {
            get => (_corners[0] + _corners[1]) / 2.0f;
            set
            {
                var currentCenter = Center;
                var diff = value - currentCenter;
                _corners[0] += diff;
                _corners[1] += diff;
                UpdateCube();
            }
        }

        public Vector3 Size
        {
            //  Size is padded by one, because the bounding box includes both the min and max voxels
            get => (Vector3.Max(_corners[0], _corners[1]) - Vector3.Min(_corners[0], _corners[1]) + Vector3.one);
            set
            {
                var currentCenter = Center;
                _corners[0] = currentCenter - value / 2.0f;
                _corners[1] = currentCenter + value / 2.0f;
                UpdateCube();
            }
        }

        public Color CubeColor
        {
            get => _color;
            set
            {
                if (_color != value)
                {
                    if (FeatureSetParent)
                    {
                        FeatureSetParent.SetFeatureAsDirty(Index);
                    }
                }

                _color = value;
            }
        }

        public bool Visible
        {
            get => _active;
            set
            {
                if (_active != value)
                {
                    if (FeatureSetParent)
                    {
                        FeatureSetParent.SetFeatureAsDirty(Index);
                    }
                }

                _active = value;
            }
        }


        public bool Selected
        {
            get => _selected;
            set
            {
                if (_selected != value)
                {
                    if (FeatureSetParent)
                    {
                        FeatureSetParent.SetFeatureAsDirty(Index);
                    }
                }

                _selected = value;
            }
        }

        public void SetBounds(Vector3 cornerMin, Vector3 cornerMax)
        {
            _corners[0] = cornerMin;
            _corners[1] = cornerMax;
            UpdateCube();
        }

        public Vector3 GetMinBounds()
        {
            return _corners[0];
        }

        public Vector3 GetMaxBounds()
        {
            return _corners[1];
        }

        public void SetVoxel(Vector3Int voxel)
        {
            _corners[0] = voxel;
            _corners[1] = voxel;
            UpdateCube();
        }

        private void UpdateCube()
        {
            var boundingBoxSize = Size;
            var center = Center;
            _unityBounds = new Bounds(center, boundingBoxSize);
            if (FeatureSetParent)
            {
                FeatureSetParent.SetFeatureAsDirty(Index);
            }
        }
        
        public static void SetCubeColors(CuboidLine cube, Color baseColor, bool colorAxes)
        {
            cube.Color = baseColor;

            if (colorAxes)
            {
                var colorAxisX = new Color(1.0f, 0.3f, 0.3f);
                var colorAxisY = new Color(0.3f, 1.0f, 0.3f);
                var colorAxisZ = new Color(0.3f, 0.3f, 1.0f);
                cube.SetColor(colorAxisX, 7);
                cube.SetColor(colorAxisY, 4);
                cube.SetColor(colorAxisZ, 8);
            }
        }
    }
}