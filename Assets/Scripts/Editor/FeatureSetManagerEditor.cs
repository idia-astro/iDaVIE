using UnityEngine;
using System.Collections;
using DataFeatures;
using UnityEditor;
using VolumeData;


[CustomEditor(typeof(FeatureSetManager))]
public class FeatureSetManagerEditor : Editor
{
    private string selectionComment = "";
    private float metric = 0.0f;
    private bool _appendToFile = true;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        FeatureSetManager featureSetManager = (FeatureSetManager) target;
        if (EditorApplication.isPlaying)
        {
            if (GUILayout.Button("Import Feature Set"))
            {
                Debug.Log("Ability is currently disabled!");
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Selected feature", featureSetManager.SelectedFeature != null ? featureSetManager.SelectedFeature.Name : "None");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Teleport"))
            {
                VolumeDataSetRenderer volumeDataSetRenderer = featureSetManager.GetComponentInParent<VolumeDataSetRenderer>();
                if (volumeDataSetRenderer)
                {
                    volumeDataSetRenderer.TeleportToRegion();
                }
            }

            if (GUILayout.Button("Crop"))
            {
                VolumeDataSetRenderer volumeDataSetRenderer = featureSetManager.GetComponentInParent<VolumeDataSetRenderer>();
                if (volumeDataSetRenderer)
                {
                    volumeDataSetRenderer.CropToRegion();
                }
            }
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            selectionComment = EditorGUILayout.TextField("Comment", selectionComment);
            metric = EditorGUILayout.FloatField("Metric", metric);
            GUILayout.EndHorizontal();
            _appendToFile = GUILayout.Toggle(_appendToFile, "Append To File");
            if (GUILayout.Button("Add to list"))
            {
                if (featureSetManager.SelectedFeature != null && featureSetManager.AddToList(featureSetManager.SelectedFeature, metric, selectionComment))
                {
                    Debug.Log($"Added feature {featureSetManager.SelectedFeature.Name} to list with metric {metric} and comment {selectionComment}");
                }
                if (_appendToFile)
                {
                    if (featureSetManager.SelectedFeature != null && featureSetManager.AppendFeatureToFile(featureSetManager.SelectedFeature))
                    {
                        Debug.Log($"Appeneded feature to file {featureSetManager.OutputFile}");
                    }
                }
            }
            GUI.enabled = true;
        }
    }
}