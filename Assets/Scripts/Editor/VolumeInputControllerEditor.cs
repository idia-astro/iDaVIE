using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VolumeInputController))]
public class VolumeInputControllerEditor : Editor
{
    private Vector3Int _targetCenter;
    private Vector3Int _boundsMin;
    private Vector3Int _boundsMax;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        VolumeInputController volumeInputController = (VolumeInputController) target;
        if (volumeInputController)
        {
            GUILayout.Space(20);
            _targetCenter = EditorGUILayout.Vector3IntField("Point Voxel", _targetCenter);
            if (GUILayout.Button("Teleport"))
            {
                volumeInputController.Teleport(_targetCenter - (0.5f * Vector3.one), _targetCenter + (0.5f * Vector3.one));
            }

            GUILayout.Space(20);
            _boundsMin = EditorGUILayout.Vector3IntField("Bounds Min", _boundsMin);
            _boundsMax = EditorGUILayout.Vector3IntField("Bounds Max", _boundsMax);
            if (GUILayout.Button("Teleport"))
            {
                volumeInputController.Teleport(_boundsMin - (0.5f * Vector3.one), _boundsMax + (0.5f * Vector3.one));
            }
        }
    }
}