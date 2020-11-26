using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using VolumeData;
using VoTableReader;
using UnityEngine;
using System.Linq;
using System.Globalization;

namespace DataFeatures
{
    public class FeatureSetRenderer : MonoBehaviour
    {
        //public Feature FeaturePrefab;
        //private FeatureSetImporter _importer;

        public List<Feature> FeatureList { get; private set; }

        public Dictionary<string, string>[] FeatureRawData { get; private set; }


        public int NumberFeatures { get; private set; }
        public string[] FeatureNames { get; private set; }
        public Vector3[] FeaturePositions { get; private set; }
        public Vector3[] BoxMinPositions { get; private set; }
        public Vector3[] BoxMaxPositions { get; private set; }

        private void Awake()
        {
            FeatureList = new List<Feature>();
        }

        // Add feature to Renderer as container
        public void AddFeature(Feature featureToAdd)
        {
            FeatureList.Add(featureToAdd);
        }

        public void ToggleVisibility()
        {
            foreach (var feature in FeatureList)
            {
                feature.StatusChanged = true;
                feature.Visible = !feature.Visible;
            }
        }

        public void SetVisibilityOn()
        {
            foreach (var feature in FeatureList)
                feature.Visible = true;
        }

        public void SetVisibilityOff()
        {
            foreach (var feature in FeatureList)
                feature.Visible = false;
        }

        public void SelectFeature(Feature feature)
        {
            var featureManager = GetComponentInParent<FeatureSetManager>();
            if (featureManager)
            {
                featureManager.SelectedFeature = feature;
                Debug.Log($"Selected feature '{feature.Name}'");
            }
        }

        // Spawn Feature objects intro world from FileName
        public void SpawnFeaturesFromVOTable(Dictionary<SourceMappingOptions, string> mapping, VoTable voTable)
        {
            if (voTable.Rows.Count == 0 || voTable.Column.Count == 0)
            {
                Debug.Log($"Error reading VOTable! Note: Currently the VOTable may not contain xmlns declarations.");
                return;
            }
            string[] colNames = new string[voTable.Column.Count];
            for (int i = 0; i < voTable.Column.Count; i++)
                colNames[i] = voTable.Column[i].Name;
            FeatureRawData = new Dictionary<string, string>[voTable.Rows.Count];
            int[] xyzIndices = { Array.IndexOf(colNames, mapping[SourceMappingOptions.X]),
                Array.IndexOf(colNames, mapping[SourceMappingOptions.Y]),
                Array.IndexOf(colNames, mapping[SourceMappingOptions.Z]) };
            if ( xyzIndices[0] < 0 ||  xyzIndices[1] < 0 ||  xyzIndices[2] < 0)
            {
                Debug.Log($"Minimum column parameters not found!");
                return;
            }
            int[] boxIndices =
            {
                Array.IndexOf(colNames, mapping[SourceMappingOptions.Xmin]),
                Array.IndexOf(colNames, mapping[SourceMappingOptions.Xmax]),
                Array.IndexOf(colNames, mapping[SourceMappingOptions.Ymin]),
                Array.IndexOf(colNames, mapping[SourceMappingOptions.Ymax]),
                Array.IndexOf(colNames, mapping[SourceMappingOptions.Zmin]),
                Array.IndexOf(colNames, mapping[SourceMappingOptions.Zmax]),
            };
            int nameIndex = Array.IndexOf(colNames, mapping[SourceMappingOptions.Name]);
            NumberFeatures = voTable.Rows.Count;
            FeatureNames = new string[NumberFeatures];
            FeaturePositions = new Vector3[NumberFeatures];

            // if there are box dimensions, initialize array with number of features, otherwise initialize empty array
            if (boxIndices.Min() > 0)
            {
                BoxMinPositions = new Vector3[NumberFeatures];
                BoxMaxPositions = new Vector3[NumberFeatures];
            }
            else
            {
                BoxMinPositions = new Vector3[0];
                BoxMaxPositions = new Vector3[0];
            }
            for (int row = 0; row < voTable.Rows.Count; row++)   // For each row (feature)...
            {

                for (int i = 0; i < xyzIndices.Length; i++)
                {
                    float value = float.Parse((string)voTable.Rows[row].ColumnData[xyzIndices[i]], CultureInfo.InvariantCulture);
                    switch (i)
                    {
                        case 0:
                            FeaturePositions[row].x = value;
                            break;
                        case 1:
                            FeaturePositions[row].y = value;
                            break;
                        case 2:
                            FeaturePositions[row].z = value;
                            break;
                    }
                }
                // ...get box bounds if they exist
                if (boxIndices.Min() > 0)
                {
                    for (int i = 0; i < boxIndices.Length; i++)
                    {
                        float value = float.Parse((string)voTable.Rows[row].ColumnData[boxIndices[i]], CultureInfo.InvariantCulture);
                        switch (i)
                        {
                            case 0:
                                BoxMinPositions[row].x = value;
                                break;
                            case 1:
                                BoxMaxPositions[row].x = value;
                                break;
                            case 2:
                                BoxMinPositions[row].y = value;
                                break;
                            case 3:
                                BoxMaxPositions[row].y = value;
                                break;
                            case 4:
                                BoxMinPositions[row].z = value;
                                break;
                            case 5:
                                BoxMaxPositions[row].z = value;
                                break;
                        }
                    }
                }
                // ...get name if exists
                if (nameIndex > 0)
                {
                    string value = (string)voTable.Rows[row].ColumnData[nameIndex];
                    FeatureNames[row] = value;
                }
                else
                {
                    FeatureNames[row] = "Source #" + row;
                }
            }
            var volumeDataSetRenderer = GetComponentInParent<VolumeDataSetRenderer>();
            if (volumeDataSetRenderer)
            {
                Vector3 cubeMin, cubeMax;

                if (BoxMinPositions.Length > 0)
                {
                    for (int i = 0; i < NumberFeatures; i++)
                    {
                        cubeMin = BoxMinPositions[i];
                        cubeMax = BoxMaxPositions[i];
                        FeatureList.Add(new Feature(cubeMin, cubeMax, Color.cyan, transform, FeatureNames[i]));
                    }
                }
                else
                {
                    for (int i = 0; i < NumberFeatures; i++)
                    {
                        cubeMin = FeaturePositions[i];
                        cubeMax = FeaturePositions[i];
                        FeatureList.Add(new Feature(cubeMin, cubeMax, Color.cyan, transform, FeatureNames[i]));
                    }
                }
            }
        } 


        /*
        // Output the features to File
        public void OutputFeaturesToFile(string FileName)
        {
            VolumeDataSet parentVolume = GetComponentInParent<VolumeDataSet>();
            var volumeDataSetRenderer = GetComponentInParent<VolumeDataSetRenderer>();
            if (parentVolume != null && volumeDataSetRenderer != null)
            {
                string volumeName = Path.GetFileName(parentVolume.FileName);
                string[] featureData = new string[2 + _featureList.Count];
                featureData[0] = "#VR Features from Cube: " + volumeName;
                featureData[1] = "    x    y    z";
                for (int i = 0; i < _featureList.Count; i++)
                {
                    Vector3 featurePosition = _featureList[i].transform.position;
                    Vector3 featureVolPosition = volumeDataSetRenderer.LocalPositionToVolumePosition(featurePosition);
                    featureData[i + 2] = $"    {featureVolPosition.x}    {featureVolPosition.y}    {featureVolPosition.z}";
                }

                File.WriteAllLines(FileName, featureData);
            }
        }
        */
    }
}