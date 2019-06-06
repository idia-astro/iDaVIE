using UnityEngine;
using System.Collections;
using DataFeatures;
using UnityEditor;


[CustomEditor(typeof(FeatureSetManager))]
public class FeatureSetManagerEditor : Editor
{
    private string selectionComment = "";
    private float metric = 0.0f;
    
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        FeatureSetManager featureSetManager = (FeatureSetManager) target;
        if (EditorApplication.isPlaying)
        {
            if (GUILayout.Button("Import Feature Set"))
            {
                featureSetManager.ImportFeatureSet();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Selected feature", featureSetManager.SelectedFeature != null ? featureSetManager.SelectedFeature.Name : "None");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Teleport"))
            {
                //featureSetManager.ImportFeatureSet();
            }

            if (GUILayout.Button("Crop"))
            {
                //featureSetManager.ImportFeatureSet();
            }
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            selectionComment = EditorGUILayout.TextField("Comment", selectionComment);
            metric = EditorGUILayout.FloatField("Metric", metric);
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Add to list"))
            {
                if (featureSetManager.SelectedFeature != null && featureSetManager.AddToList(featureSetManager.SelectedFeature, metric, selectionComment))
                {
                    Debug.Log($"Added feature {featureSetManager.SelectedFeature.Name} to list with metric {metric} and comment {selectionComment}");
                }
            }
            GUI.enabled = true;
        }
    }
}