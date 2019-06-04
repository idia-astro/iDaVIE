﻿using System.Collections.Generic;
using VolumeData;
using UnityEngine;


namespace DataFeatures
{
    public class FeatureSetManager : MonoBehaviour
    {
        public FeatureSetRenderer FeatureSetRendererPrefab;
        public string FeatureFileToLoad;
        public string FeatureMappingFile;
        public bool ImportAtStart;
        public Feature SelectedFeature { get; private set; }

        // List containing the different FeatureSets (example: SofiaSet, CustomSet, VRSet, etc.)
        // UI will have tab for each Renderer containing the lists of Features
        private List<FeatureSetRenderer> _featureSetList;

        // Active Renderer will be "container" to add Features to if saving is desired.
        private FeatureSetRenderer _activeFeatureSetRenderer;


        // Start is called before the first frame update
        void Start()
        {
            _featureSetList = new List<FeatureSetRenderer>();
            _activeFeatureSetRenderer = null;
            if (ImportAtStart)
            {
                ImportFeatureSet();
            }
        }

        // Creates new empty FeatureSetRenderer for adding Features
        public FeatureSetRenderer NewFeatureSet()
        {
            VolumeDataSetRenderer volumeRenderer = GetComponentInParent<VolumeDataSetRenderer>();
            Vector3 CubeDimensions = volumeRenderer.GetCubeDimensions();
            FeatureSetRenderer featureSetRenderer;
            featureSetRenderer = Instantiate(FeatureSetRendererPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            featureSetRenderer.transform.SetParent(transform, false);
            featureSetRenderer.transform.localScale = new Vector3(1 / CubeDimensions.x, 1 / CubeDimensions.y, 1 / CubeDimensions.z);
            _featureSetList.Add(featureSetRenderer);
            if (_activeFeatureSetRenderer == null)
                _activeFeatureSetRenderer = featureSetRenderer;
            return featureSetRenderer;
        }

        // Creates FeatureSetRenderer filled with Features from file
        public FeatureSetRenderer ImportFeatureSet()
        {
            FeatureSetRenderer featureSetRenderer = null;
            if (FeatureFileToLoad == "")
            {
                Debug.Log("Please enter path to feature file.");
                return featureSetRenderer;
            }

            if (FeatureMappingFile == "")
            {
                Debug.Log("Please enter path to feature mapping file.");
                return featureSetRenderer;
            }

            VolumeDataSetRenderer volumeRenderer = GetComponentInParent<VolumeDataSetRenderer>();
            Vector3 CubeDimensions = volumeRenderer.GetCubeDimensions();

            FeatureSetRendererPrefab.FileName = FeatureFileToLoad;
            FeatureSetRendererPrefab.MappingFileName = FeatureMappingFile;
            featureSetRenderer = Instantiate(FeatureSetRendererPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            featureSetRenderer.transform.SetParent(transform, false);
            // Move BLC to (0,0,0)
            featureSetRenderer.transform.localPosition -= 0.5f * Vector3.one;
            featureSetRenderer.transform.localScale = new Vector3(1 / CubeDimensions.x, 1 / CubeDimensions.y, 1 / CubeDimensions.z);
            // Shift by half a voxel (because voxel center has integer coordinates, not corner)
            featureSetRenderer.transform.localPosition -= featureSetRenderer.transform.localScale * 0.5f;

            featureSetRenderer.SpawnFeaturesFromFile();
            _featureSetList.Add(featureSetRenderer);
            if (_activeFeatureSetRenderer == null)
                _activeFeatureSetRenderer = featureSetRenderer;
            return featureSetRenderer;
        }

        public bool SelectFeature(Vector3 cursorWorldSpace)
        {
            // Deselect existing feature
            if (SelectedFeature != null)
            {
                SelectedFeature.Selected = false;
            }

            FeatureSetRenderer featureSetRenderer = GetComponentInChildren<FeatureSetRenderer>();
            // TODO: Extend this to multiple feature lists
            if (featureSetRenderer)
            {
                Vector3 volumeSpacePosition = featureSetRenderer.transform.InverseTransformPoint(cursorWorldSpace);
                foreach (var feature in featureSetRenderer.FeatureList)
                {
                    if (feature.UnityBounds.Contains(volumeSpacePosition))
                    {
                        SelectedFeature = feature;
                        SelectedFeature.Selected = true;
                        return true;
                    }
                }
            }

            return false;
        }

        public void ExportFeatureSet(FeatureSetRenderer setToExport, string FileName)
        {
        }
    }
}