using System;
using System.IO;
using System.Collections.Generic;
using VolumeData;
using UnityEngine;
using VoTableReader;

namespace DataFeatures
{
    public class FeatureSetManager : MonoBehaviour
    {
        public static Color[] FeatureColors = {Color.cyan, Color.yellow, Color.magenta, Color.green, Color.red, Color.grey};
        public FeatureSetRenderer FeatureSetRendererPrefab;
        public GameObject FeatureAnchorPrefab;
        public string FeatureFileToLoad;
        public string FeatureMappingFile;
        public VolumeDataSetRenderer VolumeRenderer;
        private string _timeStamp;
        private StreamWriter _streamWriter;
        private Feature _selectedFeature;

        public bool NeedToResetList = true;

        public bool NeedToRespawnMenuList = true;
        public bool NeedToUpdateInfo = false;

        public FeatureMenuController SourceListController = null;
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
                    if (ActiveFeatureSetRenderer)
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
        public List<FeatureSetRenderer> ImportedFeatureSetList {get; private set;}
        public List<FeatureSetRenderer> GeneratedFeatureSetList {get; private set;}

        // Active Renderer will be "container" to add Features to if saving is desired.
        public FeatureSetRenderer ActiveFeatureSetRenderer {get; set;}

        void Awake()
        {
            ImportedFeatureSetList = new List<FeatureSetRenderer>();
            GeneratedFeatureSetList = new List<FeatureSetRenderer>();
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
            if (ActiveFeatureSetRenderer && _selectedFeature != null && _selectedFeature.Selected)
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
                        anchor.transform.SetParent(ActiveFeatureSetRenderer.transform, false);
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

        public FeatureSetRenderer CreateSelectionFeatureSet()
        {
            Vector3 CubeDimensions = VolumeRenderer.GetCubeDimensions();
            FeatureSetRenderer featureSetRenderer;
            featureSetRenderer = Instantiate(FeatureSetRendererPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            featureSetRenderer.transform.SetParent(transform, false);
            featureSetRenderer.Initialize(false);
            featureSetRenderer.name = "Selection Set";
            featureSetRenderer.tag = "customSet";
            // Move BLC to (0,0,0)
            featureSetRenderer.transform.localPosition -= 0.5f * Vector3.one;
            featureSetRenderer.transform.localScale = new Vector3(1 / CubeDimensions.x, 1 / CubeDimensions.y, 1 / CubeDimensions.z);
            // Shift by half a voxel (because voxel center has integer coordinates, not corner)
            featureSetRenderer.transform.localPosition -= featureSetRenderer.transform.localScale * 0.5f;
            GeneratedFeatureSetList.Add(featureSetRenderer); //TODO: change to GeneratedFeatureSetList later
            return featureSetRenderer; 
        }
        // Creates new empty FeatureSetRenderer for adding Features
        public FeatureSetRenderer CreateNewFeatureSet()
        {
            Vector3 CubeDimensions = VolumeRenderer.GetCubeDimensions();
            FeatureSetRenderer featureSetRenderer;
            featureSetRenderer = Instantiate(FeatureSetRendererPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            featureSetRenderer.transform.SetParent(transform, false);
            featureSetRenderer.Initialize(false);
            featureSetRenderer.name = "Mask Source Set";
            featureSetRenderer.tag = "customSet";
            // Move BLC to (0,0,0)
            featureSetRenderer.transform.localPosition -= 0.5f * Vector3.one;
            featureSetRenderer.transform.localScale = new Vector3(1 / CubeDimensions.x, 1 / CubeDimensions.y, 1 / CubeDimensions.z);
            // Shift by half a voxel (because voxel center has integer coordinates, not corner)
            featureSetRenderer.transform.localPosition -= featureSetRenderer.transform.localScale * 0.5f;
            featureSetRenderer.FeatureColor = FeatureColors[ImportedFeatureSetList.Count];
            featureSetRenderer.Index = ImportedFeatureSetList.Count;
            ImportedFeatureSetList.Add(featureSetRenderer); //TODO: change to GeneratedFeatureSetList later
            return featureSetRenderer;
        }

        // Creates FeatureSetRenderer filled with Features from file
        public FeatureSetRenderer ImportFeatureSet(Dictionary<SourceMappingOptions, string> mapping, VoTable voTable, string name, bool[] columnsMask)
        {
            FeatureSetRenderer featureSetRenderer = null;
            Vector3 CubeDimensions = VolumeRenderer.GetCubeDimensions();
            featureSetRenderer = Instantiate(FeatureSetRendererPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            featureSetRenderer.transform.SetParent(transform, false);
            featureSetRenderer.Initialize(true);
            featureSetRenderer.name = name;
            // Move BLC to (0,0,0)
            featureSetRenderer.transform.localPosition -= 0.5f * Vector3.one;
            featureSetRenderer.transform.localScale = new Vector3(1 / CubeDimensions.x, 1 / CubeDimensions.y, 1 / CubeDimensions.z);
            // Shift by half a voxel (because voxel center has integer coordinates, not corner)
            featureSetRenderer.transform.localPosition -= featureSetRenderer.transform.localScale * 0.5f;
            featureSetRenderer.FeatureColor = FeatureColors[ImportedFeatureSetList.Count];
            featureSetRenderer.SpawnFeaturesFromVOTable(mapping, voTable, columnsMask);
            featureSetRenderer.Index = ImportedFeatureSetList.Count;
            ImportedFeatureSetList.Add(featureSetRenderer);
            return featureSetRenderer;
        }

        public bool SelectFeature(Vector3 cursorWorldSpace)
        {
            DeselectFeature();
            foreach (var featureSetRenderer in ImportedFeatureSetList)
            {
                Vector3 volumeSpacePosition = featureSetRenderer.transform.InverseTransformPoint(cursorWorldSpace);
                float prevVolume = float.NaN;
                foreach (var feature in featureSetRenderer.FeatureList)
                {
                    if (feature.Visible && feature.UnityBounds.Contains(volumeSpacePosition))
                    {
                        if (prevVolume != float.NaN)
                        {
                            float currentVolume = feature.UnityBounds.size.x * feature.UnityBounds.size.y * feature.UnityBounds.size.z;
                            if (currentVolume > prevVolume || currentVolume == prevVolume && ActiveFeatureSetRenderer != feature.FeatureSetParent)
                                continue;
                        }
                        SelectedFeature = feature;
                        SelectedFeature.Selected = true;
                        NeedToRespawnMenuList = true;
                        ActiveFeatureSetRenderer = feature.FeatureSetParent;
                        prevVolume = SelectedFeature.UnityBounds.size.x * SelectedFeature.UnityBounds.size.y * SelectedFeature.UnityBounds.size.z;
                    }
                }
            }
            return SelectedFeature != null;
        }

        public void DeselectFeature()
        {
            if (SelectedFeature != null)
            {
                HideAnchors();
                SelectedFeature.Selected = false;
                if (SelectedFeature.Temporary)
                {
                    SelectedFeature.Visible = false;
                }
            }
        }
        
        public bool CreateCustomFeature(Vector3 boundsMin, Vector3 boundsMax, string featureName, bool temporary = true)
        {
            ActiveFeatureSetRenderer = GeneratedFeatureSetList[0];
            ActiveFeatureSetRenderer.ClearFeatures();
            if (ActiveFeatureSetRenderer)
            {
                DeselectFeature();
                SelectedFeature = new Feature(boundsMin, boundsMax, Color.white, featureName, -1, -1, null, ActiveFeatureSetRenderer, true) {Temporary = temporary, Selected = true};
                if (temporary)
                {
                    SelectedFeature.ShowAxes(true);
                }

                ActiveFeatureSetRenderer.AddFeature(SelectedFeature);
                return true;
            }

            return false;
        }

        public bool AddToList(Feature feature, float metric, string comment)
        {
            if (ActiveFeatureSetRenderer)
            {
                feature.Temporary = false;
                feature.Comment = comment;
                feature.Metric = metric;
                if (!ActiveFeatureSetRenderer.FeatureList.Contains(feature))
                {
                    ActiveFeatureSetRenderer.FeatureList.Add(feature);
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