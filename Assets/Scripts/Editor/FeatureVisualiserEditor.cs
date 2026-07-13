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
using iDaVIE.Infrastructure.Unity;
using UnityEditor;
using UnityEngine;
using VolumeData;

[CustomEditor(typeof(FeatureVisualiser))]
public class FeatureVisualiserEditor : Editor
{
    private string _selectionComment = "";
    private float _metric = 0.0f;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        FeatureVisualiser featureVisualiser = (FeatureVisualiser) target;
        if (EditorApplication.isPlaying)
        {
            if (GUILayout.Button("Import Feature Set"))
            {
                Debug.Log("Ability is currently disabled!");
            }

            EditorGUILayout.Space();
            var selectedFeature = featureVisualiser.Service?.SelectedFeature;
            EditorGUILayout.LabelField("Selected feature", selectedFeature != null ? selectedFeature.Name : "None");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Teleport"))
            {
                VolumeDataSetRenderer volumeDataSetRenderer = featureVisualiser.GetComponentInParent<VolumeDataSetRenderer>();
                if (volumeDataSetRenderer)
                {
                    volumeDataSetRenderer.TeleportToRegion();
                }
            }

            if (GUILayout.Button("Crop"))
            {
                VolumeDataSetRenderer volumeDataSetRenderer = featureVisualiser.GetComponentInParent<VolumeDataSetRenderer>();
                if (volumeDataSetRenderer)
                {
                    volumeDataSetRenderer.CropToFeature();
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            _selectionComment = EditorGUILayout.TextField("Comment", _selectionComment);
            _metric = EditorGUILayout.FloatField("Metric", _metric);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Save to file"))
            {
                if (selectedFeature != null)
                {
                    featureVisualiser.Catalog.AppendFeatureToSessionFile(selectedFeature);
                    Debug.Log($"Appended feature to file {featureVisualiser.Catalog.SessionOutputFileName}");
                }
            }

            GUI.enabled = true;
        }
    }
}
