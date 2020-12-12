using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using VolumeData;
using VoTableReader;
using UnityEngine;
using System.Linq;
using System.Globalization;
using System.Text;

namespace DataFeatures
{
    public class FeatureSetRenderer : MonoBehaviour
    {
        private enum coordTypes {  cartesian,  freqz,  velz, redz   }

        public List<Feature> FeatureList { get; private set; }

        public Dictionary<string, string>[] FeatureRawData { get; private set; }

        private IntPtr _astFrameSet;
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
            var setCoordinates = mapping.Keys;
            bool containsBoxes = false;
            coordTypes sourceType = coordTypes.cartesian;
            var volumeDataSetRenderer = GetComponentInParent<VolumeDataSetRenderer>();
            int[] posIndices = new int[3];
            IntPtr volumeAstFrame = volumeDataSetRenderer.AstFrame;
            string[] colNames = new string[voTable.Column.Count];
            for (int i = 0; i < voTable.Column.Count; i++)
                colNames[i] = voTable.Column[i].Name;
            if (setCoordinates.Contains(SourceMappingOptions.X))
            {
                posIndices[0] = Array.IndexOf(colNames, mapping[SourceMappingOptions.X]);
                posIndices[1] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Y]);
                posIndices[2] =Array.IndexOf(colNames, mapping[SourceMappingOptions.Z]);
            }
            else
            {
                posIndices[0] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Ra]);
                posIndices[1] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Dec]); 
                if (setCoordinates.Contains(SourceMappingOptions.Velo))
                {
                    sourceType = coordTypes.velz;
                    AstTool.GetAltSpecSet(volumeAstFrame, out _astFrameSet, new StringBuilder("VOPT"), new StringBuilder("m/s"), new StringBuilder(volumeDataSetRenderer.StdOfRest));
                    posIndices[2] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Velo]); 
                }
                else if (setCoordinates.Contains(SourceMappingOptions.Freq))
                {
                    sourceType = coordTypes.freqz;
                    AstTool.GetAltSpecSet(volumeAstFrame, out _astFrameSet, new StringBuilder("FREQ"), new StringBuilder("Hz"), new StringBuilder(volumeDataSetRenderer.StdOfRest));
                    posIndices[2] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Freq]); 
                }
                else if (setCoordinates.Contains(SourceMappingOptions.Redshift))
                {
                    sourceType = coordTypes.redz;
                    AstTool.GetAltSpecSet(volumeAstFrame, out _astFrameSet, new StringBuilder("REDSHIFT"), new StringBuilder(""), new StringBuilder(volumeDataSetRenderer.StdOfRest));
                    posIndices[2] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Redshift]); 
                }
            }
            AstTool.Invert(_astFrameSet);
            if (setCoordinates.Contains(SourceMappingOptions.Xmin))
                containsBoxes = true;
            if (voTable.Rows.Count == 0 || voTable.Column.Count == 0)
            {
                Debug.Log($"Error reading VOTable! Note: Currently the VOTable may not contain xmlns declarations.");
                return;
            }
            FeatureRawData = new Dictionary<string, string>[voTable.Rows.Count];
            
            if (posIndices[0] < 0 ||  posIndices[1] < 0 ||  posIndices[2] < 0)
            {
                Debug.Log($"Minimum column parameters not found!");
                return;
            }
            int[] boxIndices = {-1,-1,-1,-1,-1,-1};
            if (containsBoxes)
            {
                boxIndices[0] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Xmin]);
                boxIndices[1] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Xmax]);
                boxIndices[2] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Ymin]);
                boxIndices[3] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Ymax]);
                boxIndices[4] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Zmin]);
                boxIndices[5] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Zmax]);
            }
            int nameIndex = -1;
            if (setCoordinates.Contains(SourceMappingOptions.ID))
                nameIndex = Array.IndexOf(colNames, mapping[SourceMappingOptions.ID]);
            NumberFeatures = voTable.Rows.Count;
            FeatureNames = new string[NumberFeatures];
            FeaturePositions = new Vector3[NumberFeatures];
            BoxMinPositions = new Vector3[NumberFeatures];
            BoxMaxPositions = new Vector3[NumberFeatures];
            double xPhys, yPhys, zPhys;
            xPhys = yPhys = zPhys = double.NaN;
            for (int row = 0; row < voTable.Rows.Count; row++)   // For each row (feature)...
            {
                for (int i = 0; i < posIndices.Length; i++)
                {
                    string stringToParse = (string)voTable.Rows[row].ColumnData[posIndices[i]];
                    double value;
                    if (double.TryParse(stringToParse, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                    {
                        switch (i)
                        {
                            case 0:
                                xPhys = Math.PI * value / 180.0;
                                break;
                            case 1:
                                yPhys = Math.PI * value / 180.0;
                                break;
                            case 2:
                                zPhys = value;
                                break;
                        }
                    }
                }
                if (sourceType != coordTypes.cartesian)
                {
                    double x,y,z;
                    AstTool.Transform3D(_astFrameSet, xPhys, yPhys, zPhys, 1, out x, out y, out z);
                    FeaturePositions[row].Set((float)x, (float)y, (float)z);
                }
                else
                    FeaturePositions[row].Set((float)xPhys, (float)yPhys, (float)zPhys);

                // ...get box bounds if they exist
                if (boxIndices.Min() >= 0)
                {
                    for (int i = 0; i < boxIndices.Length; i++)
                    {
                        float value = float.Parse((string)voTable.Rows[row].ColumnData[boxIndices[i]], CultureInfo.InvariantCulture); //change to tryparse
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
                else
                {
                    BoxMinPositions[row].Set(FeaturePositions[row].x - 1, FeaturePositions[row].y - 1, FeaturePositions[row].z - 1);
                    BoxMaxPositions[row].Set(FeaturePositions[row].x + 1, FeaturePositions[row].y + 1, FeaturePositions[row].z + 1);
                }
                // ...get name if exists
                if (nameIndex >= 0)
                {
                    string value = (string)voTable.Rows[row].ColumnData[nameIndex];
                    FeatureNames[row] = value;
                }
                else
                {
                    FeatureNames[row] = $"Source #{row + 1}";
                }
            }
            if (volumeDataSetRenderer)
            {
                Vector3 cubeMin, cubeMax;
                if (BoxMinPositions.Length > 0)
                {
                    for (int i = 0; i < NumberFeatures; i++)
                    {
                        cubeMin = BoxMinPositions[i];
                        cubeMax = BoxMaxPositions[i];
                        FeatureList.Add(new Feature(cubeMin, cubeMax, Color.cyan, transform, FeatureNames[i], i));
                    }
                }
                else
                {
                    for (int i = 0; i < NumberFeatures; i++)
                    {
                        cubeMin = FeaturePositions[i];
                        cubeMax = FeaturePositions[i];
                        FeatureList.Add(new Feature(cubeMin, cubeMax, Color.cyan, transform, FeatureNames[i], i));
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