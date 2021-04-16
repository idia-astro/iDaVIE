using Vectrosity;
using System.Collections.Generic;
using UnityEngine;

namespace DataFeatures
{
    // Feature is the basic unit of marking up the volume
    public class Feature
    {
        public bool Temporary;
        public string Comment;
        public float Metric;
        public int Index {get;}
        public string Name { get;}
        private bool _selected;
        private Color _color;
        private bool _active;
        private Bounds _unityBounds;
        private Vector3 _position;
        private Vector3[] _corners = new Vector3[2];
        public string[] RawData {get; set;}
        public FeatureSetRenderer FeatureSetParent {get; private set;}

        public GameObject LinkedListItem {get; set;}

        public bool StatusChanged;

        public Feature(Vector3 cubeMin, Vector3 cubeMax, Color cubeColor, string name, int index, string[] rawData, FeatureSetRenderer parent, bool startVisible)
        {
            FeatureSetParent = parent;
            Index = index;
            _color = cubeColor;
            Name = name;
            SetBounds(cubeMin, cubeMax);
            RawData = rawData;
            parent.IsDirty = true;
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
                    FeatureSetParent.IsDirty = true;
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
                    FeatureSetParent.IsDirty = true;
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
                    FeatureSetParent.IsDirty = true;
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
            FeatureSetParent.IsDirty = true;
        }
        
        public static void SetCubeColors(VectorLine cube, Color32 baseColor, bool colorAxes, int index = 0)
        {
            cube.SetColor(baseColor);

            if (colorAxes)
            {
                var colorAxisX = new Color(1.0f, 0.3f, 0.3f);
                var colorAxisY = new Color(0.3f, 1.0f, 0.3f);
                var colorAxisZ = new Color(0.3f, 0.3f, 1.0f);
                cube.SetColor(colorAxisX, 8 + index);
                cube.SetColor(colorAxisY, 4 + index);
                cube.SetColor(colorAxisZ, 11 + index);
            }
        }
    }
}