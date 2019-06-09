using Vectrosity;
using System.Collections.Generic;
using UnityEngine;


// Feature is the basic unit of marking up the volume
public class Feature
{
    public bool Temporary;
    public string Comment;
    public float Metric;
    
    private bool _selected;
    private Bounds _unityBounds;
    private Vector3 _position;
    private Vector3 _cornerMin;
    private Vector3 _cornerMax;
    private VectorLine _boundingBox;
    private bool _isVoxel;

    public Feature(Vector3 cubeMin, Vector3 cubeMax, Color cubeColor, Transform transform, string name, bool isVoxel)
    {
        _isVoxel = isVoxel;
        _boundingBox = new VectorLine(name, new List<Vector3>(24), 1.0f) {drawTransform = transform, color = cubeColor};
        _boundingBox.Draw3DAuto();
        SetBounds(cubeMin, cubeMax);
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

    public Vector3 CornerMin
    {
        get => _cornerMin;
        set
        {
            _cornerMin = value;
            UpdateCube();
        }
    }

    public Vector3 CornerMax
    {
        get => _cornerMax;
        set
        {
            _cornerMax = value;
            UpdateCube();
        }
    }

    public Vector3 Center
    {
        get => (_cornerMax + _cornerMin) / 2.0f;
        set
        {
            var currentCenter = Center;
            var diff = value - currentCenter;
            _cornerMin += diff;
            _cornerMax += diff;
            UpdateCube();
        }
    }

    public Vector3 Size
    {
        //  Size is padded by one, because the bounding box includes both the min and max voxels
        get => (Vector3.Max(_cornerMax, _cornerMin) - Vector3.Min(_cornerMax, _cornerMin) + Vector3.one);
        set
        {
            var currentCenter = Center;
            _cornerMin = currentCenter - value / 2.0f;
            _cornerMax = currentCenter + value / 2.0f;
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

    public bool IsVoxel
    {
        get => _isVoxel;
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
        _cornerMin = cornerMin;
        _cornerMax = cornerMax;
        UpdateCube();
    }

    public void SetVoxel(Vector3Int voxel)
    {
        _cornerMin = voxel;
        _cornerMax = voxel;
        UpdateCube();
    }

    private void UpdateCube()
    {
        var boundingBoxSize = Size;
        var center = Center;
        _boundingBox.MakeCube(center, boundingBoxSize.x, boundingBoxSize.y, boundingBoxSize.z);
        _unityBounds = new Bounds(center, boundingBoxSize);
    }
}