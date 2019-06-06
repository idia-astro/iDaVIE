using UnityEngine;
using System.Collections;
using DataFeatures;
using UnityEditor;


[CustomEditor(typeof(FeatureSetRenderer))]
public class FeatureSetRendererEditor : Editor
{
    private int selectionTarget;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        FeatureSetRenderer featureSetRenderer = (FeatureSetRenderer) target;
        if (GUILayout.Button("Toggle Set Visibility"))
        {
            featureSetRenderer.ToggleVisibility();
        }

        selectionTarget = EditorGUILayout.IntField("Feature ID", selectionTarget);
        GUILayout.BeginHorizontal();
        GUI.enabled = (selectionTarget > 0 && selectionTarget <= featureSetRenderer.FeatureList.Count);
        if (GUILayout.Button("Select feature"))
        {
            var feature = featureSetRenderer.FeatureList[selectionTarget - 1];
            if (feature != null)
            {
                featureSetRenderer.SelectFeature(feature);
            }
        }

        GUILayout.EndHorizontal();
        GUI.enabled = true;
    }
}