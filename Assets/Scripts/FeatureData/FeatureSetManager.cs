using System;
using System.IO;
using System.Collections.Generic;
using VolumeData;
using UnityEngine;
using UnityEngine.Serialization;
using VoTableReader;

namespace DataFeatures
{

    public enum FeatureSetType
    {
        Selection,
        Mask,
        New,
        Imported
    }
    
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
        
        public bool NeedToRespawnMenuList = true;
        public bool NeedToUpdateInfo;

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
                    if (_selectedFeature.FeatureSetParent)
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

        [FormerlySerializedAs("OutputFile")] public string OutputFileName;

        // Lists containing the different FeatureSets (example: SofiaSet, CustomSet, VRSet, etc.)
        // UI will have tab for each Renderer containing the lists of Features
        public FeatureSetRenderer SelectionFeatureSet {get; private set;}    // Feature set for making selections
        public List<FeatureSetRenderer> MaskFeatureSetList {get; private set;}        // Feature sets generated from mask
        public List<FeatureSetRenderer> NewFeatureSetList {get; private set;}         // Feature sets created by user
        public List<FeatureSetRenderer> ImportedFeatureSetList {get; private set;}  // Feature sets imported from catalogs


        void Awake()
        {
            MaskFeatureSetList = new List<FeatureSetRenderer>();
            ImportedFeatureSetList = new List<FeatureSetRenderer>();
            NewFeatureSetList = new List<FeatureSetRenderer>();
            
            _timeStamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            OutputFileName = _timeStamp + ".ascii";

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
            if (_selectedFeature != null && _selectedFeature.FeatureSetParent && _selectedFeature.Selected)
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
                        anchor.transform.SetParent(_selectedFeature.FeatureSetParent.transform, false);
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


        /// <summary>
        /// Creates new empty FeatureSetRenderer for adding Features
        /// </summary>
        /// <param name="name"></param>
        /// <param name="tag"></param>
        /// <param name="index"></param>
        /// <param name="color"></param>
        /// <returns>FeatureSetRenderer</returns>
        public FeatureSetRenderer CreateEmptyFeatureSet(string name, string tag, int index, Color color)
        {
            Vector3 CubeDimensions = VolumeRenderer.GetCubeDimensions();
            FeatureSetRenderer featureSetRenderer;
            featureSetRenderer = Instantiate(FeatureSetRendererPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            featureSetRenderer.transform.SetParent(transform, false);
            featureSetRenderer.Initialize();
            featureSetRenderer.name = name;
            featureSetRenderer.tag = tag;
            // Move BLC to (0,0,0)
            featureSetRenderer.transform.localPosition -= 0.5f * Vector3.one;
            featureSetRenderer.transform.localScale = new Vector3(1 / CubeDimensions.x, 1 / CubeDimensions.y, 1 / CubeDimensions.z);
            // Shift by half a voxel (because voxel center has integer coordinates, not corner)
            featureSetRenderer.transform.localPosition -= featureSetRenderer.transform.localScale * 0.5f;
            featureSetRenderer.FeatureColor = color;
            featureSetRenderer.Index = index;
            return featureSetRenderer;
        }
        
        /// <summary>
        /// Creates a new FeatureSetRenderer for the single selection feature
        /// </summary>
        public FeatureSetRenderer CreateSelectionFeatureSet()
        {
            var selectionFeatureSet = CreateEmptyFeatureSet("Selection Set", "customSet", 0, Color.white);
            selectionFeatureSet.RawDataKeys = new string[] { "RawData" };
            selectionFeatureSet.RawDataTypes = new string[] { "string" };
            SelectionFeatureSet = selectionFeatureSet;
            return selectionFeatureSet;
        }
        
        /// <summary>
        /// Creates a new FeatureSetRenderer for the mask stats to populate
        /// </summary>
        /// <returns>FeatureSetRenderer</returns>
        public FeatureSetRenderer CreateMaskFeatureSet()
        {
            var maskFeatureSet = CreateEmptyFeatureSet("Mask Source Set", "customSet", 0, FeatureColors[0]); //TODO: make color assignment smarter (based on previous sets)
            MaskFeatureSetList.Add(maskFeatureSet);
            return maskFeatureSet;
        }

        /// <summary>
        /// Creates FeatureSetRenderer filled with Features from voTable file
        /// </summary>
        /// <param name="mapping"></param>
        /// <param name="voTable"></param>
        /// <param name="name"></param>
        /// <param name="columnsMask"></param>
        /// <param name="excludeExternal"></param>
        /// <returns>FeatureSetRenderer</returns>
        public FeatureSetRenderer ImportFeatureSet(Dictionary<SourceMappingOptions, string> mapping, VoTable voTable, string name, bool[] columnsMask, bool excludeExternal)
        {
            var importedFeatureSetRenderer = CreateEmptyFeatureSet(name, "customSet", ImportedFeatureSetList.Count, FeatureColors[ImportedFeatureSetList.Count]);
            importedFeatureSetRenderer.SpawnFeaturesFromVOTable(mapping, voTable, columnsMask, excludeExternal);
            var config = Config.Instance;
            if (config.importedFeaturesStartVisible)
            {
                importedFeatureSetRenderer.SetVisibilityOn();
            }
            else
            {
                importedFeatureSetRenderer.SetVisibilityOff();
            }

            ImportedFeatureSetList.Add(importedFeatureSetRenderer);
            return importedFeatureSetRenderer;
        }

        public bool SelectFeature(Vector3 cursorWorldSpace)
        {
            DeselectFeature();
            Feature foundFeature = null;
            //if (ImportedFeatureSetList[0].
            foundFeature = FindFeatureAtCursor(cursorWorldSpace, ImportedFeatureSetList);
            if (foundFeature == null)
            {
                foundFeature = FindFeatureAtCursor(cursorWorldSpace, MaskFeatureSetList);
            }
            if (foundFeature == null)
            {
                foundFeature = FindFeatureAtCursor(cursorWorldSpace, NewFeatureSetList);
            }
            if (foundFeature != null)
            {
                SelectedFeature = foundFeature;
                SelectedFeature.Selected = true;
                NeedToRespawnMenuList = true;
                return true;
            }
            return false;
        }
//TODO: This is not working as intended!
        private Feature FindFeatureAtCursor(Vector3 cursorWorldSpace, List<FeatureSetRenderer> featureSetList)
        {
            Feature foundFeature = null;
            foreach (var featureSetRenderer in featureSetList)
            {
                Vector3 volumeSpacePosition = featureSetRenderer.transform.InverseTransformPoint(cursorWorldSpace);
                float prevVolume = float.NaN;
                foreach (var feature in featureSetRenderer.FeatureList)
                {
                    if (feature.Visible && feature.UnityBounds.Contains(volumeSpacePosition))
                    {
                        if (!float.IsNaN(prevVolume))
                        {
                            float currentVolume = feature.UnityBounds.size.x * feature.UnityBounds.size.y * feature.UnityBounds.size.z;
                            if (currentVolume > prevVolume || currentVolume == prevVolume && SelectionFeatureSet != feature.FeatureSetParent)
                                continue;
                        }
                        foundFeature = feature;
                        prevVolume = foundFeature.UnityBounds.size.x * foundFeature.UnityBounds.size.y * foundFeature.UnityBounds.size.z;
                    }
                }
            }

            return foundFeature;
        }

        public bool SelectFeature(Feature feature)
        {
            DeselectFeature();
            SelectedFeature = feature;
            SelectedFeature.Selected = true;
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
        
        /// <summary>
        /// Create a temporary feature to function as the selection box
        /// </summary>
        /// <param name="boundsMin"></param>
        /// <param name="boundsMax"></param>
        /// <param name="featureName"></param>
        /// <param name="temporary"></param>
        /// <returns>boolean indicating success</returns>
        public bool CreateSelectionFeature(Vector3 boundsMin, Vector3 boundsMax)
        {
            SelectionFeatureSet.ClearFeatures();
            var flag = "";
            if (SelectionFeatureSet)
            {
                DeselectFeature();
                var selectionFeature = new Feature(boundsMin, boundsMax, Color.white, "selection", flag, -1, -1, new string[] { "" }, true) {Temporary = true};
                SelectionFeatureSet.AddFeature(selectionFeature);
                SelectedFeature = selectionFeature;
                selectionFeature.ShowAxes(true);
                return true;
            }
            return false;
        }

        public bool AddSelectedFeatureToNewSet()
        {
            if (NewFeatureSetList.Count == 0)
            {
                NewFeatureSetList.Add(CreateEmptyFeatureSet("New Feature Set", "customSet", 0, Color.green));   //consider different tag names
                // Need to fix how exporting VOTables works, but this is necessary in meantime
                NewFeatureSetList[0].RawDataKeys = new[] { "RawData" };
                NewFeatureSetList[0].RawDataTypes = new[] { "char" };
            }
            if (SelectedFeature != null)
            {
                if (SelectedFeature.FeatureSetParent == SelectionFeatureSet)
                {
                    SelectedFeature.Temporary = false;
                    SelectedFeature.Index = NewFeatureSetList[0].FeatureList.Count;
                    NewFeatureSetList[0].AddFeature(SelectedFeature);
                }
                else
                {
                    //make a duplicate of the feature and add it to the new set
                    var duplicateFeature = new Feature(SelectedFeature.CornerMin, SelectedFeature.CornerMax, SelectedFeature.CubeColor, SelectedFeature.Name, SelectedFeature.Flag, NewFeatureSetList[0].FeatureList.Count, SelectedFeature.Id, new string[]{""}, true);
                    NewFeatureSetList[0].AddFeature(duplicateFeature);
                }
                NewFeatureSetList[0].FeatureMenuScrollerDataSource.InitData();
                NeedToRespawnMenuList = true;
                return true;
            }
            return false;
        }
        
        public bool AddToList(Feature feature, float metric, string comment)
        {
            if (NewFeatureSetList[0])
            {
                feature.Temporary = false;
                feature.Comment = comment;
                feature.Metric = metric;
                if (!NewFeatureSetList[0].FeatureList.Contains(feature))
                {
                    NewFeatureSetList[0].FeatureList.Add(feature);
                    return true;
                }
            }
            return false;
        }

        public bool AppendFeatureToFile(Feature feature)
        {
            if (_streamWriter == null || !File.Exists($"Data/DataFeatures/{OutputFileName}"))
            {
                string outFilePath = "Data/DataFeatures/" + OutputFileName;
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