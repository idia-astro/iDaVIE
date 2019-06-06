using UnityEngine;
using System.Collections;
using DataFeatures;
using UnityEditor;


[CustomEditor(typeof(FeatureSetRenderer))]
public class FeatureSetRendererEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        FeatureSetRenderer featureSetRenderer = (FeatureSetRenderer)target;
        if (GUILayout.Button("Toggle Set Visibility"))
        {
            featureSetRenderer.ToggleVisibility();
        }
    }
}