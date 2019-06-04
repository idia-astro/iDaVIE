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
        public string FileName;
        public string MappingFileName;
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
        public void SpawnFeaturesFromFile()
        {
            _importer = FeatureSetImporter.CreateSetFromAscii(FileName, MappingFileName);
            var volumeDataSetRenderer = GetComponentInParent<VolumeDataSetRenderer>();
            if (volumeDataSetRenderer)
            {
                for (int i = 0; i < _importer.NumberFeatures; i++)
                {
                    Feature feature = new Feature(_importer.BoxMinPositions[i], _importer.BoxMaxPositions[i], Color.cyan, transform, _importer.FeatureNames[i]);
                    FeatureList.Add(feature);
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