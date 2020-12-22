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
        public int Index {get; private set;}
        private bool _selected;
        private Bounds _unityBounds;
        private Vector3 _position;
        private Vector3[] _corners = new Vector3[2];
        private VectorLine _boundingBox;
        public string[] RawData {get; set;}
        public FeatureSetRenderer FeatureSetParent {get; private set;}

        public GameObject LinkedListItem {get; set;}

        public bool StatusChanged;

        public Feature(Vector3 cubeMin, Vector3 cubeMax, Color cubeColor, string name, int index, string[] rawData, FeatureSetRenderer parent, bool startVisible)
        {
            Index = index;
            _boundingBox = new VectorLine(name, new List<Vector3>(24), 1.0f) {drawTransform = parent.transform, color = cubeColor};
            _boundingBox.Draw3DAuto();
            SetBounds(cubeMin, cubeMax);
            RawData = rawData;
            FeatureSetParent = parent;
            _boundingBox.active = startVisible;
        }

        public void ChangeColor(Color color)
        {
            _boundingBox.color=color;
        }

        public void Deactivate()
        {
            if (_boundingBox != null)
            {
                _boundingBox.StopDrawing3DAuto();
                _boundingBox.active = false;
                VectorLine.Destroy(ref _boundingBox);
            }
        }

        ~Feature()
        {
            Deactivate();
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
            get => _boundingBox.color;
            set => _boundingBox.color = value;
        }

        public bool Visible
        {
            get => _boundingBox.active;
            set => _boundingBox.active = value;
        }

        public string Name
        {
            get => _boundingBox?.name;
        }

        public bool Selected
        {
            get => _selected;
            set
            {
                _selected = value;
                if (_boundingBox != null)
                {
                    _boundingBox.lineWidth = _selected ? 5.0f : 1.0f;
                }
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
            _boundingBox?.MakeCube(center, boundingBoxSize.x, boundingBoxSize.y, boundingBoxSize.z);
            _unityBounds = new Bounds(center, boundingBoxSize);
        }
    }
}