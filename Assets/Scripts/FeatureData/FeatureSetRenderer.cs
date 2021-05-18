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

        public VolumeDataSetRenderer VolumeRenderer { get; private set; }
        public FeatureSetManager FeatureManager { get; private set; }
        public string[] FeatureNames { get; private set; }
        public Vector3[] FeaturePositions { get; private set; }
        public Vector3[] BoxMinPositions { get; private set; }
        public Vector3[] BoxMaxPositions { get; private set; }
        public string[] RawDataKeys { get; set; }
        public string[] RawDataTypes { get; set; }
        public string FileName { get; private set; }

        public bool IsImported {get; private set;}

        //public GameObject MenuList = null;
        //public List<SofiaListItemInfo> SofiaList;

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

        //public List<SofiaListItemInfo> SofiaList {get; private set;} = new List<SofiaListItemInfo>(); //Try recycleable list instead public 
        public SofiaRecyclableScrollerDataSource SofiaMenuDataSource;



        private void Awake()
        {
            FeatureList = new List<Feature>();
            _dirtyFeatures = new List<int>();
            _dirtyFeatures.Capacity = DefaultFeatureCapacity;
            
            _computeBufferVertices = new ComputeBuffer(DefaultFeatureCapacity * VerticesPerFeature, BytesPerVertex, ComputeBufferType.Structured);
            _vertices = new FeatureVertex[DefaultFeatureCapacity * VerticesPerFeature];
            _materialInstance = Material.Instantiate(LineRenderingMaterial);
            //SofiaMenuData = GetComponent<SofiaRecyclableScrollerDataSource>();
        }

        public void Initialize(bool isImported)
        {
            IsImported = isImported;
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
                        var feature = FeatureList[i];
                        FeatureVisibility visibility = feature.Visible ? (feature.Selected ? FeatureVisibility.Selected: FeatureVisibility.Visible) : FeatureVisibility.Hidden;
                        MakeAxisAlignedCube(feature.Center, feature.Size, feature.CubeColor, visibility, i * VerticesPerFeature, _vertices);
                    }
                }
                _computeBufferVertices.SetData(_vertices);
                _dirtyFeatures.Clear();
            }
        }

        // Add feature to Renderer as container
        public void AddFeature(Feature featureToAdd)
        {
            FeatureList.Add(featureToAdd);
            SofiaListItemInfo obj = new SofiaListItemInfo();
            obj.IdTextField = (FeatureList.Count).ToString();
            obj.SourceName = featureToAdd.Name;
            obj.Feature = featureToAdd;
            //SofiaList.Add(obj);
            SetFeatureAsDirty(featureToAdd.Index);
        }

        public void ClearFeatures()
        {
            FeatureList.Clear();
            SetFeatureAsDirty();
        }
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

        public void SetVisibilityOn()
        {
            /*
            foreach (Transform child in MenuList.transform)
            {
                child.GetComponent<SofiaCell>().SetVisibilityIconsOn();
            }
*/
            featureSetVisible = true;
            SetFeatureAsDirty();
            foreach (var feature in FeatureList)
            {
                feature.Visible = true;
            }
        }

        public void SetVisibilityOff()
        {

            featureSetVisible = false;
            SetFeatureAsDirty();
            foreach (var feature in FeatureList)
            {
                feature.Visible = false;
            }
        }

        public void SelectFeature(Feature feature)
        {
            if (FeatureManager)
            {
                FeatureManager.SelectedFeature = feature;
                Debug.Log($"Selected feature '{feature.Name}'");
                FeatureManager.NeedToRespawnMenuList = true;
            }
        }

        public void UpdateColor()
        {
            foreach (var feature in FeatureList)
            {
                feature.CubeColor = FeatureColor;
            }
        }

        public void SpawnFeaturesFromSourceStats(Dictionary<short, DataAnalysis.SourceStats> sourceStatsDict)
        {
            RawDataKeys = new[] {"Sum", "Peak", "VSys (Channel)", "W20 (Channel)"};
            RawDataTypes = new[] {"float", "float", "float", "float"};
            foreach (var item in sourceStatsDict)
            {
                var sourceStats = item.Value;
                var boxMin = new Vector3(sourceStats.minX + 1, sourceStats.minY + 1, sourceStats.minZ + 1);
                var boxMax = new Vector3(sourceStats.maxX + 1, sourceStats.maxY + 1, sourceStats.maxZ + 1);
                var featureName = $"Masked Source #{item.Key}";
                var rawStrings = new [] {$"{sourceStats.sum}", $"{sourceStats.peak}", $"{sourceStats.channelVsys}", $"{sourceStats.channelW20}"};
                AddFeature(new Feature(boxMin, boxMax, FeatureColor, featureName, item.Key - 1, rawStrings, this, false));
            }
            SofiaMenuDataSource.InitData();
        }

        // Spawn Feature objects intro world from FileName
        public void SpawnFeaturesFromVOTable(Dictionary<SourceMappingOptions, string> mapping, VoTable voTable, bool[] columnsMask)
        {
            if (VolumeRenderer == null)
            {
                Debug.Log("No VolumeDataSetRenderer detected for Feature import... was FeatureSetRenderer initialized properly?");
                return;
            }
            List<string> rawDataKeysList = new List<string>();
            IntPtr volumeAstFrame = VolumeRenderer.AstFrame;
            var setCoordinates = mapping.Keys;
            bool containsBoxes = false;
            List<string>[] featureRawData = new List<string>[voTable.Rows.Count];
            ZType = CoordTypes.cartesian;
            int[] posIndices = new int[3];
            IntPtr astFrameSet = IntPtr.Zero;
            string[] colNames = new string[voTable.Column.Count];
            for (int i = 0; i < voTable.Column.Count; i++)
            {
                colNames[i] = voTable.Column[i].Name;
                if (columnsMask[i])
                    rawDataKeysList.Add(colNames[i]);
            }
            RawDataKeys = rawDataKeysList.ToArray();
            RawDataTypes = Enumerable.Repeat("string", RawDataKeys.Length).ToArray();
            if (setCoordinates.Contains(SourceMappingOptions.X))
            {
                posIndices[0] = Array.IndexOf(colNames, mapping[SourceMappingOptions.X]);
                posIndices[1] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Y]);
                posIndices[2] =Array.IndexOf(colNames, mapping[SourceMappingOptions.Z]);
            }
            else if (setCoordinates.Contains(SourceMappingOptions.Ra))
            {
                posIndices[0] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Ra]);
                posIndices[1] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Dec]); 
                if (setCoordinates.Contains(SourceMappingOptions.Velo))
                {
                    ZType = CoordTypes.velz;
                    if (AstTool.GetAltSpecSet(volumeAstFrame, out astFrameSet, new StringBuilder("VRAD"), new StringBuilder("m/s"), new StringBuilder(VolumeRenderer.StdOfRest)) != 0)
                    {
                        Debug.Log($"Error creating feature astframe!");
                        return;
                    }
                    posIndices[2] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Velo]); 
                }
                else if (setCoordinates.Contains(SourceMappingOptions.Freq))
                {
                    ZType = CoordTypes.freqz;
                    if (AstTool.GetAltSpecSet(volumeAstFrame, out astFrameSet, new StringBuilder("FREQ"), new StringBuilder("Hz"), new StringBuilder(VolumeRenderer.StdOfRest)) != 0)
                    {
                        Debug.Log($"Error creating feature astframe!");
                        return;
                    }
                    posIndices[2] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Freq]); 
                }
                else if (setCoordinates.Contains(SourceMappingOptions.Redshift))
                {
                    ZType = CoordTypes.redz;
                    if (AstTool.GetAltSpecSet(volumeAstFrame, out astFrameSet, new StringBuilder("REDSHIFT"), new StringBuilder(""), new StringBuilder(VolumeRenderer.StdOfRest)) != 0)
                    {
                        Debug.Log($"Error creating feature astframe!");
                        return;
                    }
                    posIndices[2] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Redshift]); 
                }
                if (AstTool.Invert(astFrameSet) != 0)
                {
                    Debug.Log("Error finding inverted frame set!");
                }
            }
            if (setCoordinates.Contains(SourceMappingOptions.Xmin))
                containsBoxes = true;
            if (voTable.Rows.Count == 0 || voTable.Column.Count == 0)
            {
                Debug.Log($"Error reading VOTable! Note: Currently the VOTable may not contain xmlns declarations.");
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
            FeatureNames = new string[voTable.Rows.Count];
            FeaturePositions = new Vector3[voTable.Rows.Count];
            BoxMinPositions = new Vector3[voTable.Rows.Count];
            BoxMaxPositions = new Vector3[voTable.Rows.Count];
            double xPhys, yPhys, zPhys;
            xPhys = yPhys = zPhys = double.NaN;
            for (int row = 0; row < voTable.Rows.Count; row++)   // For each row (feature)...
            {
                featureRawData[row] = new List<string>();
                for (int i = 0; i < voTable.Columns.Count; i++)
                {
                    if (columnsMask[i])
                        featureRawData[row].Add(voTable.Rows[row].ColumnData[i].ToString());
                }
                if (containsPositions && !containsBoxes)
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
                        FeaturePositions[row].Set((float)x, (float)y, (float)z);
                    }
                    else
                        FeaturePositions[row].Set((float)xPhys, (float)yPhys, (float)zPhys);
                    BoxMinPositions[row].Set(FeaturePositions[row].x - 1, FeaturePositions[row].y - 1, FeaturePositions[row].z - 1);
                    BoxMaxPositions[row].Set(FeaturePositions[row].x + 1, FeaturePositions[row].y + 1, FeaturePositions[row].z + 1);
                }
                // ...get box bounds if they exist
                else if (containsBoxes)
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
                    Debug.Log("Error: dimensionless features loaded!");
                    return;
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
            
            SetFeatureAsDirty();
            
            if (VolumeRenderer)
            {
                Vector3 cubeMin, cubeMax;
                for (int i = 0; i < voTable.Rows.Count; i++)
                {
                    cubeMin = BoxMinPositions[i];
                    cubeMax = BoxMaxPositions[i];
                    FeatureList.Add(new Feature(cubeMin, cubeMax, FeatureColor, FeatureNames[i], i, featureRawData[i].ToArray(), this, false));
                }
            }
            SofiaMenuDataSource.InitData();
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