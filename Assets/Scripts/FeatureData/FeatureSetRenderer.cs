using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using VolumeData;
using UnityEngine;

namespace DataFeatures
{
    public class FeatureSetRenderer : MonoBehaviour
    {
        //public Feature FeaturePrefab;
        private FeatureSetImporter _importer;

        public List<Feature> FeatureList { get; private set; }

        private void Awake()
        {
            FeatureList = new List<Feature>();
        }

        // Add feature to Renderer as container
        public void AddFeature(Feature featureToAdd)
        {
            FeatureList.Add(featureToAdd);
        }

        // Spawn Feature objects intro world from FileName
        public void SpawnFeaturesFromFile(string fileName, string mappingFileName)
        {
            _importer = FeatureSetImporter.CreateSetFromAscii(fileName, mappingFileName);
            var volumeDataSetRenderer = GetComponentInParent<VolumeDataSetRenderer>();
            if (volumeDataSetRenderer)
            {
                Vector3 cubeMin, cubeMax;

                if (_importer.BoxMinPositions.Length > 0)
                {
                    for (int i = 0; i < _importer.NumberFeatures; i++)
                    {
                        cubeMin = _importer.BoxMinPositions[i];
                        cubeMax = _importer.BoxMaxPositions[i];
                        Debug.Log($"cubemin {cubeMin}");
                        Debug.Log($"cubemax {cubeMax}");

                        FeatureList.Add(new Feature(cubeMin, cubeMax, Color.cyan, transform, _importer.FeatureNames[i]));
                    }
                }
                else
                {
                    for (int i = 0; i < _importer.NumberFeatures; i++)
                    {
                        cubeMin = _importer.FeaturePositions[i];
                        cubeMax = _importer.FeaturePositions[i];
                        FeatureList.Add(new Feature(cubeMin, cubeMax, Color.cyan, transform, _importer.FeatureNames[i]));
                    }
                }
            }
        } 

        public void ToggleVisibility()
        {
            foreach (var feature in FeatureList)
                feature.Visible = !feature.Visible;
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