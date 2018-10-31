using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace CatalogData
{
    public enum FileTypes { Ipac, Fits };
    public class CatalogDataSetRenderer : MonoBehaviour
    {

        

        public ColorMapDelegate OnColorMapChanged;

        public string TableFileName;
        public string MappingFileName;
        public bool SphericalCoordinates;
        public bool LineData;
        private FileTypes fileType = FileTypes.Ipac;
        [Range(0.0f, 1.0f)] public float ValueCutoffMin = 0;
        [Range(0.0f, 1.0f)] public float ValueCutoffMax = 1;
        public ColorMapEnum ColorMap = ColorMapEnum.Inferno;
        public Texture2D ColorMapTexture;


        private ComputeBuffer[] _buffers;

        private Color[] _colorMapData;
        private const int NumColorMapStops = 256;

        private CatalogDataSet _dataSet;
        private DataMapping _dataMapping;
        private Material _catalogMaterial;

        // Material property IDs
        private int _idNumDataPoints, _idPointSize, _idSpriteSheet, _idNumSprites, _idShapeIndex, _idColorMapData;
        private int _idDataX, _idDataY, _idDataZ, _idDataX2, _idDataY2, _idDataZ2, _dataVal;
        private int _idScalingX, _idScalingY, _idScalingZ, _idOffsetX, _idOffsetY, _idOffsetZ, _idScaleColorMap, _idOffsetColorMap;
        private int _idScalingTypeX, _idScalingTypeY, _idScalingTypeZ, _idScalingTypeColorMap, _idDataSetMatrix, _idPointScale;
        private int _idCutoffMin, _idCutoffMax;

        private void GetPropertyIds()
        {
            _idNumDataPoints = Shader.PropertyToID("numDataPoints");
            _idPointSize = Shader.PropertyToID("_PointSize");
            _idSpriteSheet = Shader.PropertyToID("_SpriteSheet");
            _idNumSprites = Shader.PropertyToID("_NumSprites");
            _idShapeIndex = Shader.PropertyToID("_ShapeIndex");
            _idColorMapData = Shader.PropertyToID("colorMapData");

            _idDataX = Shader.PropertyToID("dataX");
            _idDataY = Shader.PropertyToID("dataY");
            _idDataZ = Shader.PropertyToID("dataZ");
            _idDataX2 = Shader.PropertyToID("dataX2");
            _idDataY2 = Shader.PropertyToID("dataY2");
            _idDataZ2 = Shader.PropertyToID("dataZ2");
            _dataVal = Shader.PropertyToID("dataVal");

            _idScalingX = Shader.PropertyToID("scalingX");
            _idScalingY = Shader.PropertyToID("scalingY");
            _idScalingZ = Shader.PropertyToID("scalingZ");
            _idOffsetX = Shader.PropertyToID("offsetX");
            _idOffsetY = Shader.PropertyToID("offsetY");
            _idOffsetZ = Shader.PropertyToID("offsetZ");
            _idScaleColorMap = Shader.PropertyToID("scaleColorMap");
            _idOffsetColorMap = Shader.PropertyToID("offsetColorMap");

            _idScalingTypeX = Shader.PropertyToID("scalingTypeX");
            _idScalingTypeY = Shader.PropertyToID("scalingTypeY");
            _idScalingTypeZ = Shader.PropertyToID("scalingTypeZ");
            _idScalingTypeColorMap = Shader.PropertyToID("numDataPoints");
            _idDataSetMatrix = Shader.PropertyToID("datasetMatrix");
            _idPointScale = Shader.PropertyToID("pointScale");
            _idCutoffMin = Shader.PropertyToID("cutoffMin");
            _idCutoffMax = Shader.PropertyToID("cutoffMax");
        }

        void Start()
        {
            FileInfo fileInfoOriginal = new FileInfo(TableFileName);
            if (!fileInfoOriginal.Exists)
            {
                Debug.Log($"File {TableFileName} not found");
                return;
            }

            string cacheFileName = $"{TableFileName}.cache";
            FileInfo fileInfoCached = new FileInfo(cacheFileName);
            // Check if cache file exists and is more recent than the data table
            if (fileInfoCached.Exists && fileInfoCached.LastWriteTime > fileInfoOriginal.LastWriteTime)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                _dataSet = CatalogDataSet.LoadCacheFile(TableFileName);
                sw.Stop();
                Debug.Log($"Cached file read in {sw.Elapsed.TotalSeconds} seconds");
            }
            else
            {
                string fileExt = Path.GetExtension(TableFileName);
                Stopwatch sw = new Stopwatch();
                sw.Start();
                switch (fileExt)
                {
                    case ".tbl":
                        fileType = FileTypes.Ipac;
                        _dataSet = CatalogDataSet.LoadIpacTable(TableFileName);
                        break;
                    case ".fits":
                        fileType = FileTypes.Fits;
                        _dataSet = CatalogDataSet.LoadFitsTable(TableFileName);
                        break;
                    default:
                        Debug.Log($"Unrecognized file type!");
                        break;
                }
                sw.Stop();
                Debug.Log($"Table read in {sw.Elapsed.TotalSeconds} seconds");
                if (fileType == FileTypes.Ipac)
                {
                    sw.Restart();
                    _dataSet.WriteCacheFile();
                    sw.Stop();
                    Debug.Log($"Cached file written in {sw.Elapsed.TotalSeconds} seconds");
                }
            }

            // Spherical coordinates are not currently supported for line data
            if (LineData)
            {
                SphericalCoordinates = false;
            }

            _dataMapping = LineData ? DataMapping.DefaultXyzLineMapping : (SphericalCoordinates ? DataMapping.DefaultSphericalMapping : DataMapping.DefaultXyzMapping);

            if (_dataSet.DataColumns.Length == 0 || _dataSet.DataColumns[0].Length == 0)
            {
                Debug.Log($"Problem loading data catalog file {TableFileName}");
            }
            else
            {
                int numDataColumns = _dataSet.DataColumns.Length;
                _buffers = new ComputeBuffer[numDataColumns];

                for (var i = 0; i < numDataColumns; i++)
                {
                    _buffers[i] = new ComputeBuffer(_dataSet.N, 4);
                    _buffers[i].SetData(_dataSet.DataColumns[i]);
                }

                // Load instance of the material, so that each data set can have different material parameters
                GetPropertyIds();
                if (LineData)
                {
                    _catalogMaterial = new Material(Shader.Find("IDIA/CatalogLine"));
                }
                else
                {
                    _catalogMaterial = new Material(Shader.Find("IDIA/CatalogPoint"));
                    _catalogMaterial.SetFloat(_idPointSize, _dataMapping.Defaults.PointSize);
                    Texture2D spriteSheetTexture = (Texture2D) AssetDatabase.LoadAssetAtPath("Assets/Textures/billboard_textures.TGA", typeof(Texture2D));
                    _catalogMaterial.SetTexture(_idSpriteSheet, spriteSheetTexture);
                    _catalogMaterial.SetInt(_idNumSprites, 8);
                    _catalogMaterial.SetInt(_idShapeIndex, 2);
                }

                _catalogMaterial.SetInt(_idNumDataPoints, _dataSet.N);

                // Apply scaling from data set space to world space
                transform.localScale *= _dataMapping.Defaults.Scale;
                Debug.Log($"Scaling from data set space to world space: {ScalingString}");

                if (SphericalCoordinates)
                {
                    int gLatColumnIndex = _dataSet.GetDataColumnIndex(_dataMapping.Mapping.Lat.Source);
                    int gLongColumnIndex = _dataSet.GetDataColumnIndex(_dataMapping.Mapping.Long.Source);
                    int rColumnIndex = _dataSet.GetDataColumnIndex(_dataMapping.Mapping.R.Source);
                    if (gLatColumnIndex >= 0 && gLongColumnIndex >= 0 && rColumnIndex >= 0)
                    {
                        // Spatial mapping and scaling
                        _catalogMaterial.SetBuffer(_idDataX, _buffers[gLatColumnIndex]);
                        _catalogMaterial.SetBuffer(_idDataY, _buffers[gLongColumnIndex]);
                        _catalogMaterial.SetBuffer(_idDataZ, _buffers[rColumnIndex]);
                        // Spherical coordinate input assumes degrees for XY, while the shader assumes radians
                        _catalogMaterial.SetFloat(_idScalingX, _dataMapping.Mapping.Lat.Scale * Mathf.Deg2Rad);
                        _catalogMaterial.SetFloat(_idScalingY, _dataMapping.Mapping.Long.Scale * Mathf.Deg2Rad);
                        _catalogMaterial.SetFloat(_idScalingZ, _dataMapping.Mapping.R.Scale);
                        _catalogMaterial.SetFloat(_idOffsetX, _dataMapping.Mapping.Lat.Offset * Mathf.Deg2Rad);
                        _catalogMaterial.SetFloat(_idOffsetY, _dataMapping.Mapping.Long.Offset * Mathf.Deg2Rad);
                        _catalogMaterial.SetFloat(_idOffsetZ, _dataMapping.Mapping.R.Offset);
                    }
                }
                else
                {
                    int xColumnIndex = _dataSet.GetDataColumnIndex(_dataMapping.Mapping.X.Source);
                    int yColumnIndex = _dataSet.GetDataColumnIndex(_dataMapping.Mapping.Y.Source);
                    int zColumnIndex = _dataSet.GetDataColumnIndex(_dataMapping.Mapping.Z.Source);
                    if (xColumnIndex >= 0 && yColumnIndex >= 0 && zColumnIndex >= 0)
                    {
                        // Spatial mapping and scaling
                        _catalogMaterial.SetBuffer(_idDataX, _buffers[xColumnIndex]);
                        _catalogMaterial.SetBuffer(_idDataY, _buffers[yColumnIndex]);
                        _catalogMaterial.SetBuffer(_idDataZ, _buffers[zColumnIndex]);
                        _catalogMaterial.SetFloat(_idScalingX, _dataMapping.Mapping.X.Scale);
                        _catalogMaterial.SetFloat(_idScalingY, _dataMapping.Mapping.Y.Scale);
                        _catalogMaterial.SetFloat(_idScalingZ, _dataMapping.Mapping.Z.Scale);
                        _catalogMaterial.SetFloat(_idOffsetX, _dataMapping.Mapping.X.Offset);
                        _catalogMaterial.SetFloat(_idOffsetY, _dataMapping.Mapping.Y.Offset);
                        _catalogMaterial.SetFloat(_idOffsetZ, _dataMapping.Mapping.Z.Offset);
                    }

                    if (LineData)
                    {
                        int x2ColumnIndex = _dataSet.GetDataColumnIndex(_dataMapping.Mapping.X2.Source);
                        int y2ColumnIndex = _dataSet.GetDataColumnIndex(_dataMapping.Mapping.Y2.Source);
                        int z2ColumnIndex = _dataSet.GetDataColumnIndex(_dataMapping.Mapping.Z2.Source);
                        if (x2ColumnIndex >= 0 && y2ColumnIndex >= 0 && z2ColumnIndex >= 0)
                        {
                            // Spatial mapping for the end points
                            _catalogMaterial.SetBuffer(_idDataX2, _buffers[x2ColumnIndex]);
                            _catalogMaterial.SetBuffer(_idDataY2, _buffers[y2ColumnIndex]);
                            _catalogMaterial.SetBuffer(_idDataZ2, _buffers[z2ColumnIndex]);
                        }
                    }
                }

                int cmapColumnIndex = _dataSet.GetDataColumnIndex(_dataMapping.Mapping.Cmap.Source);
                if (cmapColumnIndex >= 0)
                {
                    // Color map mapping and scaling
                    _catalogMaterial.SetBuffer(_dataVal, _buffers[cmapColumnIndex]);
                    _catalogMaterial.SetFloat(_idScaleColorMap, _dataMapping.Mapping.Cmap.Scale);
                    _catalogMaterial.SetFloat(_idOffsetColorMap, _dataMapping.Mapping.Cmap.Offset);
                }
            }

            _colorMapData = new Color[NumColorMapStops];
            SetColorMap(ColorMap);
        }

        public string ScalingString
        {
            get
            {
                string unitString = _dataSet.GetColumnDefinition(SphericalCoordinates ? _dataMapping.Mapping.R.Source : _dataMapping.Mapping.X.Source).Unit;
                if (string.IsNullOrEmpty(unitString))
                {
                    unitString = "units";
                }

                float scaleMillimeters = transform.localScale.x * 1000;
                if (scaleMillimeters < 10)
                {
                    return $"1.0 {unitString} = {(scaleMillimeters).ToString("F2", CultureInfo.InvariantCulture)} mm";
                }
                else if (scaleMillimeters < 1000)
                {
                    return $"1.0 {unitString} = {(scaleMillimeters * 1e-1).ToString("F2", CultureInfo.InvariantCulture)} cm";
                }
                else
                {
                    return $"1.0 {unitString} = {(scaleMillimeters * 1e-3).ToString("F2", CultureInfo.InvariantCulture)} m";
                }
            }
        }

        // The color map array is calculated from the color map texture and sent to the GPU whenever the color map is changed
        public void SetColorMap(ColorMapEnum newColorMap)
        {
            ColorMap = newColorMap;
            int numColorMaps = ColorMapUtils.NumColorMaps;
            float colorMapPixelDeltaX = (float) (ColorMapTexture.width) / NumColorMapStops;
            float colorMapPixelDeltaY = (float) (ColorMapTexture.height) / numColorMaps;
            int colorMapIndex = newColorMap.GetHashCode();

            for (var i = 0; i < NumColorMapStops; i++)
            {
                _colorMapData[i] = ColorMapTexture.GetPixel((int) (i * colorMapPixelDeltaX), (int) (colorMapIndex * colorMapPixelDeltaY));
            }

            _catalogMaterial.SetColorArray(_idColorMapData, _colorMapData);
        }

        public void ShiftColorMap(int delta)
        {
            int numColorMaps = ColorMapUtils.NumColorMaps;
            int currentIndex = ColorMap.GetHashCode();
            int newIndex = (currentIndex + delta + numColorMaps) % numColorMaps;
            SetColorMap(ColorMapUtils.FromHashCode(newIndex));
            OnColorMapChanged?.Invoke(ColorMap);
        }

        void Update()
        {
            if (_catalogMaterial != null)
            {
                _catalogMaterial.SetFloat(_idCutoffMin, ValueCutoffMin);
                _catalogMaterial.SetFloat(_idCutoffMax, ValueCutoffMax);
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0, 1, 0, 0.5F);
            Gizmos.DrawWireCube(transform.position, transform.localScale);
        }

        void OnRenderObject()
        {
            // Update the object transform and point scale on the GPU
            _catalogMaterial.SetMatrix(_idDataSetMatrix, transform.localToWorldMatrix);
            _catalogMaterial.SetFloat(_idPointScale, transform.localScale.x);
            _catalogMaterial.SetInt(_idScalingTypeX, 0);
            _catalogMaterial.SetInt(_idScalingTypeY, 0);
            _catalogMaterial.SetInt(_idScalingTypeZ, 0);
            _catalogMaterial.SetInt(_idScalingTypeColorMap, 0);
            // Shader defines two passes: Pass #0 uses cartesian coordinates and Pass #1 uses spherical coordinates
            _catalogMaterial.SetPass(_dataMapping.Spherical ? 1 : 0);
            // Render points on the GPU using vertex pulling
            Graphics.DrawProcedural(MeshTopology.Points, _dataSet.N);
        }

        void OnDestroy()
        {
            if (_buffers != null)
            {
                for (var i = 0; i < _buffers.Length; i++)
                {
                    if (_buffers[i] != null)
                    {
                        _buffers[i].Release();
                    }
                }
            }
        }
    }
}