using Vectrosity;
using UnityEngine;

// Feature is the basic unit of marking up the volume
public class Feature : MonoBehaviour
{
    private Vector3 _cornerMin;
    private Vector3 _cornerMax;
    private Color _color;
    private VectorLine _boundingBox;

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
        get => (Vector3.Max(_cornerMax, _cornerMin) - Vector3.Min(_cornerMax, _cornerMin));
        set
        {
            var currentCenter = Center;
            _cornerMin = currentCenter - value / 2.0f;
            _cornerMax = currentCenter + value / 2.0f;
            UpdateCube();
        }
    }
    
    public void MoveToPosition(Vector3 position)
    {
        transform.localPosition = position;
    }

    public void SetBounds(Vector3 cornerMin, Vector3 cornerMax)
    {
        _cornerMin = cornerMin;
        _cornerMax = cornerMax;
        UpdateCube();
    }

    public void SetVoxel(Vector3Int voxel)
    {
        _cornerMin = voxel - 0.5f * Vector3.one;
        _cornerMax = voxel + 0.5f * Vector3.one;
        UpdateCube();
    }

    public void SetName(string newName)
    {
        name = newName;
        if (_boundingBox != null)
        {
            _boundingBox.name = $"{newName}_outline";
        }
    }

    public void SetColor(Color color)
    {
        _color = color;
        if (_boundingBox != null)
        {
            _boundingBox.SetColor(color);
        }
    }

    public void SetBoundingBox(VectorLine boundingBox)
    {
        _boundingBox = boundingBox;
    }
    
    private void UpdateCube()
    {
        if (_boundingBox != null)
        {
            var newSize = Size;
            var center = Center;
            _boundingBox.MakeCube(center, newSize.x, newSize.y, newSize.z);
        }
    }
}