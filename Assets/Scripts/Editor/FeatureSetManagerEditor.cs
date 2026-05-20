/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
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
using DataFeatures;
using UnityEditor;
using UnityEngine;
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
                    volumeDataSetRenderer.CropToFeature();
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
                        Debug.Log($"Appeneded feature to file {featureSetManager.OutputFileName}");
                    }
                }
            }
            GUI.enabled = true;
        }
    }
}