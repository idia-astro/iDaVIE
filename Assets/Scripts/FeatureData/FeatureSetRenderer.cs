using System;
using System.Collections.Generic;
using VolumeData;
using VoTableReader;
using UnityEngine;
using System.Linq;
using System.Globalization;
using System.Text;

namespace DataFeatures
{
    enum FeatureVisibility : int
    {
        Hidden = 0,
        Visible = 1,
        Selected = 2
    }

    struct FeatureVertex
    {
        public Vector3 Position;
        public Vector4 Color;
        public FeatureVisibility Visibility;
    }
    
    public class FeatureSetRenderer : MonoBehaviour
    {
        public enum CoordTypes {  cartesian,  freqz,  velz, redz   }

        public CoordTypes ZType { get; private set;}

        public List<Feature> FeatureList { get; private set; }

        public FeatureSetType FeatureSetType = FeatureSetType.Unassigned;
        public VolumeDataSetRenderer VolumeRenderer { get; private set; }
        public FeatureSetManager FeatureManager { get; private set; }
        public string[] FeatureNames { get; private set; }
        public Vector3[] FeaturePositions { get; private set; }
        public Vector3[] BoxMinPositions { get; private set; }
        public Vector3[] BoxMaxPositions { get; private set; }
        
        //TODO: Need to find a better "host" for RawData and RawDataKeys. Right now it is split between
        //Features and FeatureSetRenderer which causes problems when moving features between sets.
        public string[] RawDataKeys { get; set; }
        public string[] RawDataTypes { get; set; }
        public string FileName { get; private set; }

        public string[] Flags { get; set; }

        private bool importFlags;
        public int Index;
        
        public Color FeatureColor;

        public bool featureSetVisible = false;
        public Material LineRenderingMaterial;

        private static readonly int VerticesPerFeature = 24;
        // Vector3 for position, Vector4 for color, int for visibility info
        private static readonly int BytesPerVertex = 32;
        private static readonly int DefaultFeatureCapacity = 16384;
        private ComputeBuffer _computeBufferVertices;
        private FeatureVertex[] _vertices;
        private Material _materialInstance;
        private List<int> _dirtyFeatures;

        // For the recycled scrolling list
        public FeatureMenuDataSource FeatureMenuScrollerDataSource;



        private void Awake()
        {
            FeatureList = new List<Feature>();
            _dirtyFeatures = new List<int>();
            _dirtyFeatures.Capacity = DefaultFeatureCapacity;
            
            _computeBufferVertices = new ComputeBuffer(DefaultFeatureCapacity * VerticesPerFeature, BytesPerVertex, ComputeBufferType.Structured);
            _vertices = new FeatureVertex[DefaultFeatureCapacity * VerticesPerFeature];
            _materialInstance = Material.Instantiate(LineRenderingMaterial);
        }

        public void Initialize()
        {
            FeatureManager = GetComponentInParent<FeatureSetManager>();
            VolumeRenderer = FeatureManager.VolumeRenderer;
        }


        

        public void Update()
        {
            if (_dirtyFeatures.Count > 0)
            {
                bool allDirty = _dirtyFeatures[0] == -1;
                
                int currentCapacity = _computeBufferVertices.count / VerticesPerFeature;
                int requiredCapacity = FeatureList.Count;
                if (requiredCapacity > currentCapacity)
                {
                    _vertices = new FeatureVertex[requiredCapacity * VerticesPerFeature];
                    _computeBufferVertices.Release();
                    _computeBufferVertices = new ComputeBuffer(requiredCapacity * VerticesPerFeature, BytesPerVertex, ComputeBufferType.Structured);
                }

                if (allDirty)
                {
                    for (var i = 0; i < requiredCapacity; i++)
                    {
                        var feature = FeatureList[i];
                        FeatureVisibility visibility = feature.Visible ? (feature.Selected ? FeatureVisibility.Selected: FeatureVisibility.Visible) : FeatureVisibility.Hidden;
                        MakeAxisAlignedCube(feature.Center, feature.Size, feature.CubeColor, visibility, i * VerticesPerFeature, _vertices);
                    }
                }
                else
                {
                    foreach (var i in _dirtyFeatures)
                    {
                        if (i < FeatureList.Count && FeatureList[i] != null)
                        {
                            var feature = FeatureList[i];
                            FeatureVisibility visibility = feature.Visible ? (feature.Selected ? FeatureVisibility.Selected: FeatureVisibility.Visible) : FeatureVisibility.Hidden;
                            MakeAxisAlignedCube(feature.Center, feature.Size, feature.CubeColor, visibility, i * VerticesPerFeature, _vertices);
                        }
                    }
                }
                _computeBufferVertices.SetData(_vertices);
                _dirtyFeatures.Clear();
            }
        }

        /// <summary>
        /// Add feature to Renderer as container
        /// </summary>
        /// <param name="featureToAdd">The feature to be added</param>
        public void AddFeature(Feature featureToAdd)
        {
            FeatureList.Add(featureToAdd);
            FeatureMenuListItemInfo obj = new FeatureMenuListItemInfo();
            obj.IdTextField = (FeatureList.Count).ToString();
            obj.SourceName = featureToAdd.Name;
            obj.Feature = featureToAdd;
            featureToAdd.FeatureSetParent = this;
            SetFeatureAsDirty(featureToAdd.Index);
        }

        public void RemoveFeature(Feature featureToRemove)
        {
            FeatureList.Remove(featureToRemove);
            FeatureMenuScrollerDataSource.InitData();
        }

        public void ClearFeatures()
        {
            FeatureList.Clear();
            SetFeatureAsDirty();
        }

        /// <summary>
        /// Function to toggle the visibility on all features
        /// </summary>
        public void ToggleVisibility()
        {
            foreach (var feature in FeatureList)
            {
                feature.StatusChanged = true;
                feature.Visible = !feature.Visible;
            }
        }

        public void SetFeatureAsDirty(int index = -1)
        {
            // All Sources are dirty if the first element is -1
            if (_dirtyFeatures.Count > 0 && _dirtyFeatures[0] == -1)
            {
                return;
            }
            if (index >= FeatureList.Count)
            {
                Debug.Log($"Feature index {index} out of bounds! Marking all features as dirty");
                index = -1;
            }
            
            if (index == -1)
            {
                _dirtyFeatures.Clear();
            }
            _dirtyFeatures.Add(index);
        }

        /// <summary>
        /// Function to set all feature outlines visible
        /// </summary>
        public void SetVisibilityOn()
        {
            featureSetVisible = true;
            SetFeatureAsDirty();
            foreach (var feature in FeatureList)
            {
                feature.Visible = true;
                feature.StatusChanged = true;
            }
        }

        /// <summary>
        /// Function to set all feature outlines hidden
        /// </summary>
        public void SetVisibilityOff()
        {
            featureSetVisible = false;
            SetFeatureAsDirty();
            foreach (var feature in FeatureList)
            {
                feature.Visible = false;
                feature.StatusChanged = true;
            }
        }

        public void SelectFeature(Feature feature)
        {
            if (FeatureManager)
            {
                FeatureManager.SelectedFeature = feature;
                Debug.Log($"Selected feature '{feature.Name}'");
            }
        }

        /// <summary>
        /// Function to change the colour of the outlines of the features in the cube
        /// </summary>
        public void UpdateColor()
        {
            foreach (var feature in FeatureList)
            {
                feature.CubeColor = FeatureColor;
            }
        }

        public void SpawnFeaturesFromSourceStats(Dictionary<int, DataAnalysis.SourceStats> sourceStatsDict)
        {
            var velocityUnit = VolumeRenderer.Data.AstframeIsFreq ? VolumeRenderer.Data.GetAstAltAttribute("Unit(3)") : VolumeRenderer.Data.GetAstAttribute("Unit(3)") ;
            RawDataKeys = new[] {"Sum", "Peak", "VSys (Channel)", "W20 (Channel)", $"VSys ({velocityUnit})", $"W20 ({velocityUnit})"};
            RawDataTypes = new[] {"float", "float", "float", "float", "float", "float"};
            var flag = "";
            foreach (var item in sourceStatsDict)
            {
                var sourceStats = item.Value;
                var boxMin = new Vector3(sourceStats.minX + 1, sourceStats.minY + 1, sourceStats.minZ + 1);
                var boxMax = new Vector3(sourceStats.maxX + 1, sourceStats.maxY + 1, sourceStats.maxZ + 1);
                var featureName = $"Masked Source #{item.Key}";
                var rawStrings = new [] {$"{sourceStats.sum}", $"{sourceStats.peak}", $"{sourceStats.channelVsys}", $"{sourceStats.channelW20}", $"{sourceStats.veloVsys}", $"{sourceStats.veloW20}"};
                AddFeature(new Feature(boxMin, boxMax, FeatureColor, featureName, flag, FeatureList.Count, item.Key - 1, rawStrings, false));
            }
            FeatureMenuScrollerDataSource.InitData();
        }

        // Spawn Feature objects into world from FileName
        public void SpawnFeaturesFromTable(Dictionary<SourceMappingOptions, string> mapping, FeatureTable table, bool[] columnsMask, bool excludeExternal)
        {
            if (VolumeRenderer == null)
            {
                Debug.LogWarning("No VolumeDataSetRenderer detected for Feature import... was FeatureSetRenderer initialized properly?");
                return;
            }
            List<string> rawDataKeysList = new List<string>();
            IntPtr volumeAstFrame = VolumeRenderer.AstFrame;
            var setCoordinates = mapping.Keys;
            bool containsBoxes = false;
            List<string>[] featureRawData = new List<string>[table.Rows.Count];            ZType = CoordTypes.cartesian;
            int[] posIndices = new int[3];
            IntPtr astFrameSet = IntPtr.Zero;
            string[] colNames = new string[table.Column.Count];
            string[] colUnits = new string[table.Column.Count];
            for (int i = 0; i < table.Column.Count; i++)
            {
                colNames[i] = table.Column[i].Name;
                colUnits[i] = table.Column[i].Unit;
                if (columnsMask[i])
                    rawDataKeysList.Add(colNames[i]);
            }
            RawDataKeys = rawDataKeysList.ToArray();
            RawDataTypes = Enumerable.Repeat("string", RawDataKeys.Length).ToArray();
            if (setCoordinates.Contains(SourceMappingOptions.X))
            {
                posIndices[0] = Array.IndexOf(colNames, mapping[SourceMappingOptions.X]);
                posIndices[1] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Y]);
                posIndices[2] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Z]);
            }
            else if (setCoordinates.Contains(SourceMappingOptions.Ra))
            {
                posIndices[0] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Ra]);
                posIndices[1] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Dec]); 
                if (setCoordinates.Contains(SourceMappingOptions.Velo))
                {
                    ZType = CoordTypes.velz;
                    posIndices[2] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Velo]);
                    if (AstTool.GetAltSpecSet(volumeAstFrame, out astFrameSet, new StringBuilder("VRAD"), new StringBuilder(colUnits[posIndices[2]]), new StringBuilder(VolumeRenderer.StdOfRest)) != 0)
                    {
                        Debug.LogError($"Error creating feature astframe!");
                        return;
                    }
                     
                }
                else if (setCoordinates.Contains(SourceMappingOptions.Freq))
                {
                    ZType = CoordTypes.freqz;
                    posIndices[2] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Freq]); 
                    if (AstTool.GetAltSpecSet(volumeAstFrame, out astFrameSet, new StringBuilder("FREQ"), new StringBuilder(colUnits[posIndices[2]]), new StringBuilder(VolumeRenderer.StdOfRest)) != 0)
                    {
                        Debug.LogError($"Error creating feature astframe!");
                        return;
                    }
                }
                else if (setCoordinates.Contains(SourceMappingOptions.Redshift))
                {
                    ZType = CoordTypes.redz;
                    posIndices[2] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Redshift]);
                    if (AstTool.GetAltSpecSet(volumeAstFrame, out astFrameSet, new StringBuilder("REDSHIFT"), new StringBuilder(""), new StringBuilder(VolumeRenderer.StdOfRest)) != 0)
                    {
                        Debug.LogError($"Error creating feature astframe!");
                        return;
                    } 
                }
                if (AstTool.Invert(astFrameSet) != 0)
                {
                    Debug.LogError("Error finding inverted frame set!");
                }
            }
            if (setCoordinates.Contains(SourceMappingOptions.Xmin))
                containsBoxes = true;
            if (table.Rows.Count == 0 || table.Column.Count == 0)
            {
                Debug.LogError($"Error reading Source Table! Note: Currently the VOTable may not contain xmlns declarations.");
                return;
            }
            bool containsPositions = !(posIndices[0] < 0 ||  posIndices[1] < 0 ||  posIndices[2] < 0);
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
            int FlagIndex = -1;
            if (setCoordinates.Contains(SourceMappingOptions.Flag))
            {
                FlagIndex = Array.IndexOf(colNames, mapping[SourceMappingOptions.Flag]);
                importFlags = true;
            }
            else
                importFlags = false;

            var flags = new string[table.Rows.Count];
            var featureNames = new string[table.Rows.Count];
            var featurePositions = new Vector3[table.Rows.Count];
            var boxMinPositions = new Vector3[table.Rows.Count];
            var boxMaxPositions = new Vector3[table.Rows.Count];
            double xPhys, yPhys, zPhys;
            xPhys = yPhys = zPhys = double.NaN;
            for (int row = 0; row < table.Rows.Count; row++)   // For each row (feature)...
            {
                featureRawData[row] = new List<string>();
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    if (columnsMask[i])
                        featureRawData[row].Add(table.Rows[row].ColumnData[i].ToString());
                }
                if (importFlags)
                {
                    flags[row] = (string) table.Rows[row].ColumnData[FlagIndex];
                }
                if (containsPositions && !containsBoxes)
                {
                    for (int i = 0; i < posIndices.Length; i++)
                    {
                        string stringToParse = (string)table.Rows[row].ColumnData[posIndices[i]];
                        double value;
                        if (double.TryParse(stringToParse, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                        {
                            switch (i)
                            {
                                case 0:
                                    if (ZType != CoordTypes.cartesian)
                                        xPhys = Math.PI * value / 180.0;
                                    else
                                        xPhys = value;
                                    break;
                                case 1:
                                    if (ZType != CoordTypes.cartesian)
                                        yPhys = Math.PI * value / 180.0;
                                    else
                                        yPhys = value;
                                    break;
                                case 2:
                                    zPhys = value;
                                    break;
                            }
                        }
                    }
                    if (ZType != CoordTypes.cartesian)
                    {
                        double x,y,z;
                        AstTool.Transform3D(astFrameSet, xPhys, yPhys, zPhys, 1, out x, out y, out z);
                        featurePositions[row].Set((float)x, (float)y, (float)z);
                    }
                    else
                        featurePositions[row].Set((float)xPhys, (float)yPhys, (float)zPhys);
                    boxMinPositions[row].Set(featurePositions[row].x - 1, featurePositions[row].y - 1, featurePositions[row].z - 1);
                    boxMaxPositions[row].Set(featurePositions[row].x + 1, featurePositions[row].y + 1, featurePositions[row].z + 1);
                }
                // ...get box bounds if they exist
                else if (containsBoxes)
                {
                    for (int i = 0; i < boxIndices.Length; i++)
                    {
                        float value = float.Parse((string)table.Rows[row].ColumnData[boxIndices[i]], CultureInfo.InvariantCulture); //change to tryparse
                        switch (i)
                        {
                            case 0:
                                boxMinPositions[row].x = value;
                                break;
                            case 1:
                                boxMaxPositions[row].x = value;
                                break;
                            case 2:
                                boxMinPositions[row].y = value;
                                break;
                            case 3:
                                boxMaxPositions[row].y = value;
                                break;
                            case 4:
                                boxMinPositions[row].z = value;
                                break;
                            case 5:
                                boxMaxPositions[row].z = value;
                                break;
                        }
                    }
                }
                else
                {
                    Debug.LogError("Error: dimensionless features loaded!");
                    return;
                }
                // ...get name if exists
                if (nameIndex >= 0)
                {
                    string value = (string)table.Rows[row].ColumnData[nameIndex];
                    featureNames[row] = value;
                }
                else
                {
                    featureNames[row] = $"Source #{row + 1}";
                }
            }
            
            SetFeatureAsDirty();
            
            if (VolumeRenderer)
            {
                Vector3 cornerMin, cornerMax;
                for (int i = 0; i < table.Rows.Count; i++)
                {
                    cornerMin = boxMinPositions[i];
                    cornerMax = boxMaxPositions[i];
                    var flag = (importFlags) ? flags[i] : "";
                    var featureToAdd = new Feature(cornerMin, cornerMax, FeatureColor, featureNames[i], flag, i, i,
                        featureRawData[i].ToArray(), false);
                    featureToAdd.FeatureSetParent = this;
                    if (!(excludeExternal && FeatureIsWithinVolume(featureToAdd, VolumeRenderer)))
                    {
                        FeatureList.Add(featureToAdd);
                    }
                }

                if (excludeExternal)
                {
                    FeatureNames = new string[FeatureList.Count];
                    FeaturePositions = new Vector3[FeatureList.Count];
                    BoxMinPositions = new Vector3[FeatureList.Count];
                    BoxMaxPositions = new Vector3[FeatureList.Count];
                    if (importFlags)
                    {
                        Flags = new string[FeatureList.Count];
                    }
                    for (int i = 0; i < FeatureList.Count; i++)
                    {
                        FeatureNames[i] = featureNames[FeatureList[i].Index];
                        FeaturePositions[i] = featurePositions[FeatureList[i].Index];
                        BoxMinPositions[i] = boxMinPositions[FeatureList[i].Index];
                        BoxMaxPositions[i] = boxMaxPositions[FeatureList[i].Index];
                        if (importFlags)
                        {
                            Flags[i] = flags[FeatureList[i].Index];
                        }
                        FeatureList[i].Index = i;   // set the feature's index to the new index
                    }
                }
                else
                {
                    FeatureNames = featureNames;
                    FeaturePositions = featurePositions;
                    BoxMinPositions = boxMinPositions;
                    BoxMaxPositions = boxMaxPositions;
                }
            }
            FeatureMenuScrollerDataSource.InitData();
        }
        
        /// <summary>
        /// Method checks if the given feature's center point is within the given volume's bounds
        /// </summary>
        /// <param name="feature"></param>
        /// <param name="volume"></param>
        /// <returns></returns>
        public static bool FeatureIsWithinVolume(Feature feature, VolumeDataSetRenderer volume)
        {
            return (feature.Center.x < 0 || feature.Center.x > volume.Data.XDim ||
                feature.Center.y < 0 || feature.Center.y > volume.Data.YDim || 
                feature.Center.z < 0 || feature.Center.z > volume.Data.ZDim);
        }
        
        void OnRenderObject()
        {
            // TODO: how does this work with VR?
            _materialInstance.SetMatrix("datasetMatrix", transform.localToWorldMatrix);
            _materialInstance.SetBuffer("inputData", _computeBufferVertices);
            
            _materialInstance.SetPass(0);
            // Render lines on the GPU using vertex pulling
            Graphics.DrawProceduralNow(MeshTopology.Lines, FeatureList.Count * VerticesPerFeature);
        }

        void OnDestroy()
        {
            _computeBufferVertices.Release();
        }

        static void MakeAxisAlignedCube(Vector3 position, Vector3 size, Color color, FeatureVisibility visibility, int offset, FeatureVertex[] list)
        {
            if (offset + 24 > list.Length)
            {
                return;
            }

            size *= 0.5f;

            list[offset + 0] = new FeatureVertex {Position = position + new Vector3(-size.x, size.y, -size.z)};
            list[offset + 1] = new FeatureVertex {Position = position + new Vector3(size.x, size.y, -size.z)};
            list[offset + 2] = new FeatureVertex {Position = position + new Vector3(size.x, size.y, -size.z)};
            list[offset + 3] = new FeatureVertex {Position = position + new Vector3(size.x, size.y, size.z)};
            list[offset + 4] = new FeatureVertex {Position = position + new Vector3(size.x, size.y, size.z)};
            list[offset + 5] = new FeatureVertex {Position = position + new Vector3(-size.x, size.y, size.z)};
            list[offset + 6] = new FeatureVertex {Position = position + new Vector3(-size.x, size.y, size.z)};
            list[offset + 7] = new FeatureVertex {Position = position + new Vector3(-size.x, size.y, -size.z)};
            list[offset + 8] = new FeatureVertex {Position = position + new Vector3(-size.x, -size.y, -size.z)};
            list[offset + 9] = new FeatureVertex {Position = position + new Vector3(-size.x, size.y, -size.z)};
            list[offset + 10] = new FeatureVertex {Position = position + new Vector3(size.x, -size.y, -size.z)};
            list[offset + 11] = new FeatureVertex {Position = position + new Vector3(size.x, size.y, -size.z)};
            list[offset + 12] = new FeatureVertex {Position = position + new Vector3(-size.x, -size.y, size.z)};
            list[offset + 13] = new FeatureVertex {Position = position + new Vector3(-size.x, size.y, size.z)};
            list[offset + 14] = new FeatureVertex {Position = position + new Vector3(size.x, -size.y, size.z)};
            list[offset + 15] = new FeatureVertex {Position = position + new Vector3(size.x, size.y, size.z)};
            list[offset + 16] = new FeatureVertex {Position = position + new Vector3(-size.x, -size.y, -size.z)};
            list[offset + 17] = new FeatureVertex {Position = position + new Vector3(size.x, -size.y, -size.z)};
            list[offset + 18] = new FeatureVertex {Position = position + new Vector3(size.x, -size.y, -size.z)};
            list[offset + 19] = new FeatureVertex {Position = position + new Vector3(size.x, -size.y, size.z)};
            list[offset + 20] = new FeatureVertex {Position = position + new Vector3(size.x, -size.y, size.z)};
            list[offset + 21] = new FeatureVertex {Position = position + new Vector3(-size.x, -size.y, size.z)};
            list[offset + 22] = new FeatureVertex {Position = position + new Vector3(-size.x, -size.y, size.z)};
            list[offset + 23] = new FeatureVertex {Position = position + new Vector3(-size.x, -size.y, -size.z)};

            for (var i = 0; i < VerticesPerFeature; i++)
            {
                list[offset + i].Color = color;
                list[offset + i].Visibility = visibility;
            }
        }
    
        public void SaveAsVoTable(string filePath)
        {
            VoTableSaver.SaveFeatureSetAsVoTable(this, filePath);
        }
    }
}