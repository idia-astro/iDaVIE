﻿using System;
using System.IO;
using System.Collections.Generic;
using VolumeData;
using UnityEngine;


namespace DataFeatures
{
    public class FeatureSetManager : MonoBehaviour
    {
        public FeatureSetRenderer FeatureSetRendererPrefab;
        public GameObject FeatureAnchorPrefab;
        public string FeatureFileToLoad;
        public string FeatureMappingFile;

        private string _timeStamp;
        private StreamWriter _streamWriter;
        private Feature _selectedFeature;
        private readonly GameObject[] _anchorColliders = new GameObject[8];
        public Feature SelectedFeature
        {
            get => _selectedFeature;
            set
            {
                if (_selectedFeature == null || _selectedFeature != value)
                {
                    DeselectFeature();
                    _selectedFeature = value;
                    _selectedFeature.Selected = true;
                    if (_activeFeatureSetRenderer)
                    {
                        UpdateAnchors();
                    }
                }
            }
        }

        private static void SetGlobalScale (Transform t, Vector3 globalScale)
        {
            t.localScale = Vector3.one;
            t.localScale = new Vector3 (globalScale.x/t.lossyScale.x, globalScale.y/t.lossyScale.y, globalScale.z/t.lossyScale.z);
        }

        public string OutputFile;

        // List containing the different FeatureSets (example: SofiaSet, CustomSet, VRSet, etc.)
        // UI will have tab for each Renderer containing the lists of Features
        private List<FeatureSetRenderer> _featureSetList;

        // Active Renderer will be "container" to add Features to if saving is desired.
        private FeatureSetRenderer _activeFeatureSetRenderer;

        void Awake()
        {
            _featureSetList = new List<FeatureSetRenderer>();
            _timeStamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            OutputFile = _timeStamp + ".ascii";

            int anchorIndex = 0;
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    for (int k = 0; k < 2; k++)
                    {
                        _anchorColliders[anchorIndex] = Instantiate(FeatureAnchorPrefab, new Vector3(0, 0, 0), Quaternion.identity);
                        _anchorColliders[anchorIndex].transform.parent = transform;
                        _anchorColliders[anchorIndex].name = $"{(i == 0 ? "left" : "right")}_{(j == 0 ? "bottom" : "top")}_{(k == 0 ? "back" : "front")}";
                        anchorIndex++;
                    }
                }
            }

            HideAnchors();
        }
        
        public void Update()
        {
            if (_activeFeatureSetRenderer && _selectedFeature != null && _selectedFeature.Selected)
            {
                UpdateAnchors();
            }
            else
            {
                HideAnchors();
            }
        }

        private void UpdateAnchors()
        {
            int anchorIndex = 0;
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    for (int k = 0; k < 2; k++)
                    {
                        var anchor = _anchorColliders[anchorIndex];
                        anchor.transform.SetParent(_activeFeatureSetRenderer.transform, false);
                        Vector3 weighting = new Vector3(i, j, k);
                        anchor.transform.localPosition = Vector3.Scale(_selectedFeature.CornerMax + Vector3.one * 0.5f, weighting)
                                                         + Vector3.Scale(_selectedFeature.CornerMin - Vector3.one * 0.5f, Vector3.one - weighting);
                        SetGlobalScale(anchor.transform, Vector3.one * 0.01f);
                        anchorIndex++;
                    }
                }
            }
        }

        private void HideAnchors()
        {
            for (int i = 0; i < 8; i++)
            {
                _anchorColliders[i].transform.localScale = Vector3.zero;
            }
        }

        // Creates new empty FeatureSetRenderer for adding Features
        public void CreateNewFeatureSet()
        {
            VolumeDataSetRenderer volumeRenderer = GetComponentInParent<VolumeDataSetRenderer>();
            Vector3 CubeDimensions = volumeRenderer.GetCubeDimensions();
            FeatureSetRenderer featureSetRenderer;
            featureSetRenderer = Instantiate(FeatureSetRendererPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            featureSetRenderer.transform.SetParent(transform, false);
            featureSetRenderer.name = "Custom Feature Set";
            featureSetRenderer.tag = "customSet";
            // Move BLC to (0,0,0)
            featureSetRenderer.transform.localPosition -= 0.5f * Vector3.one;
            featureSetRenderer.transform.localScale = new Vector3(1 / CubeDimensions.x, 1 / CubeDimensions.y, 1 / CubeDimensions.z);
            // Shift by half a voxel (because voxel center has integer coordinates, not corner)
            featureSetRenderer.transform.localPosition -= featureSetRenderer.transform.localScale * 0.5f;
            _featureSetList.Add(featureSetRenderer);
            if (_activeFeatureSetRenderer == null)
                _activeFeatureSetRenderer = featureSetRenderer;
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

            featureSetRenderer = Instantiate(FeatureSetRendererPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            var indexSlash = FeatureFileToLoad.LastIndexOf("/", StringComparison.InvariantCulture) + 1;
            featureSetRenderer.name = FeatureFileToLoad.Substring(indexSlash);
            featureSetRenderer.transform.SetParent(transform, false);
            // Move BLC to (0,0,0)
            featureSetRenderer.transform.localPosition -= 0.5f * Vector3.one;
            featureSetRenderer.transform.localScale = new Vector3(1 / CubeDimensions.x, 1 / CubeDimensions.y, 1 / CubeDimensions.z);
            // Shift by half a voxel (because voxel center has integer coordinates, not corner)
            featureSetRenderer.transform.localPosition -= featureSetRenderer.transform.localScale * 0.5f;

            featureSetRenderer.SpawnFeaturesFromFile(FeatureFileToLoad, FeatureMappingFile);
            _featureSetList.Add(featureSetRenderer);
            if (_activeFeatureSetRenderer == null)
                _activeFeatureSetRenderer = featureSetRenderer;
            return featureSetRenderer;
        }

        public bool SelectFeature(Vector3 cursorWorldSpace)
        {
            DeselectFeature();
            foreach (var featureSetRenderer in _featureSetList)
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

        public void DeselectFeature()
        {
            if (SelectedFeature != null)
            {
                HideAnchors();
                SelectedFeature.Selected = false;
                if (SelectedFeature.Temporary)
                {
                    SelectedFeature.Deactivate();
                }
                
            }
        }
        
        public bool CreateNewFeature(Vector3 boundsMin, Vector3 boundsMax, string featureName, bool temporary = true)
        {
            if (_activeFeatureSetRenderer)
            {
                DeselectFeature();
                SelectedFeature = new Feature(boundsMin, boundsMax, Color.green, _activeFeatureSetRenderer.transform, featureName) {Temporary = temporary, Selected = true};
                return true;
            }

            return false;
        }

        public bool AddToList(Feature feature, float metric, string comment)
        {
            if (_activeFeatureSetRenderer)
            {
                feature.Temporary = false;
                feature.Comment = comment;
                feature.Metric = metric;
                if (!_activeFeatureSetRenderer.FeatureList.Contains(feature))
                {
                    _activeFeatureSetRenderer.FeatureList.Add(feature);
                    return true;
                }
            }
            return false;
        }

        public bool AppendFeatureToFile(Feature feature)
        {
            if (_streamWriter == null || !File.Exists($"Data/DataFeatures/{OutputFile}"))
            {
                string outFilePath = "Data/DataFeatures/" + OutputFile;
                _streamWriter = new StreamWriter(outFilePath, true);
                _streamWriter.WriteLine("# Custom List");
                _streamWriter.WriteLine("# " + "x".PadLeft(10, ' ') + "y".PadLeft(10, ' ') + "z".PadLeft(10, ' ') +
                         "x_min".PadLeft(10, ' ') + "x_max".PadLeft(10, ' ') +
                         "y_min".PadLeft(10, ' ') + "y_max".PadLeft(10, ' ') +
                         "z_min".PadLeft(10, ' ') + "z_max".PadLeft(10, ' ') +
                         "metric".PadLeft(10, ' ') + "comment".PadLeft(10, ' ')) ;
                    _streamWriter.Flush();
            }
            _streamWriter.WriteLine("  " + feature.Center.x.ToString().PadLeft(10, ' ') +
                   feature.Center.y.ToString().PadLeft(10, ' ') +
                   feature.Center.z.ToString().PadLeft(10, ' ') +
                   feature.CornerMin.x.ToString().PadLeft(10, ' ') +
                   feature.CornerMax.x.ToString().PadLeft(10, ' ') +
                   feature.CornerMin.y.ToString().PadLeft(10, ' ') +
                   feature.CornerMax.y.ToString().PadLeft(10, ' ') +
                   feature.CornerMin.z.ToString().PadLeft(10, ' ') +
                   feature.CornerMax.z.ToString().PadLeft(10, ' ') +
                   feature.Metric.ToString().PadLeft(10, ' ') + 
                   ("  \"" + feature.Comment.ToString()).PadLeft(10, ' ') + "\"");
            _streamWriter.Flush();
            return true;
        }

        public void ExportFeatureSet(FeatureSetRenderer setToExport, string FileName)
        {
        }
    }
}