using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PointData
{
    public class PointDataSet : MonoBehaviour
    {
        public string TableFileName;
        public string MappingFileName;
        public bool SphericalCoordinates;
        public bool LineData;
        public ColorMapEnum ColorMap = ColorMapEnum.Inferno;
        public Texture2D ColorMapTexture;

        private ComputeBuffer[] _buffers;

        private Color[] _colorMapData;
        private const int NumColorMapStops = 256;

        private DataCatalog _dataCatalog;
        private DataMapping _dataMapping;
        private Material _catalogMaterial;

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
                _dataCatalog = DataCatalog.LoadCacheFile(TableFileName);
                sw.Stop();
                Debug.Log($"Cached file read in {sw.Elapsed.TotalSeconds} seconds");
            }
            else
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                _dataCatalog = DataCatalog.LoadIpacTable(TableFileName);
                sw.Stop();
                Debug.Log($"IPAC table read in {sw.Elapsed.TotalSeconds} seconds");
                sw.Restart();
                _dataCatalog.WriteCacheFile();
                sw.Stop();
                Debug.Log($"Cached file written in {sw.Elapsed.TotalSeconds} seconds");
            }

            // Spherical coordinates are not currently supported for line data
            if (LineData)
            {
                SphericalCoordinates = false;
            }
            _dataMapping = LineData ? DataMapping.DefaultXyzLineMapping : (SphericalCoordinates ? DataMapping.DefaultSphericalMapping : DataMapping.DefaultXyzMapping);
            
            if (_dataCatalog.DataColumns.Length == 0 || _dataCatalog.DataColumns[0].Length == 0)
            {
                Debug.Log($"Problem loading data catalog file {TableFileName}");
            }
            else
            {
                int numDataColumns = _dataCatalog.DataColumns.Length;
                _buffers = new ComputeBuffer[numDataColumns];

                for (var i = 0; i < numDataColumns; i++)
                {
                    _buffers[i] = new ComputeBuffer(_dataCatalog.N, 4);
                    _buffers[i].SetData(_dataCatalog.DataColumns[i]);
                }

                // Load instance of the material, so that each data set can have different material parameters
                if (LineData)
                {
                    _catalogMaterial = new Material(Shader.Find("IDIA/CatalogLine"));
                    _catalogMaterial.SetInt("numDataPoints", _dataCatalog.N);
                }
                else
                {
                    _catalogMaterial = new Material(Shader.Find("IDIA/CatalogPoint"));
                    _catalogMaterial.SetInt("numDataPoints", _dataCatalog.N);
                    _catalogMaterial.SetFloat("_PointSize", _dataMapping.Defaults.PointSize);
                    Texture2D spriteSheetTexture = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Textures/billboard_textures.TGA", typeof(Texture2D));
                    _catalogMaterial.SetTexture("_SpriteSheet", spriteSheetTexture);
                    _catalogMaterial.SetInt("_NumSprites", 8);
                    _catalogMaterial.SetInt("_ShapeIndex", 2);
                }
                
                // Apply scaling from data set space to world space
                transform.localScale *= _dataMapping.Defaults.Scale;
                Debug.Log($"Scaling from data set space to world space: {ScalingString}");

                if (SphericalCoordinates)
                {
                    int gLatColumnIndex = _dataCatalog.GetDataColumnIndex(_dataMapping.Mapping.Lat.Source);
                    int gLongColumnIndex = _dataCatalog.GetDataColumnIndex(_dataMapping.Mapping.Long.Source);
                    int rColumnIndex = _dataCatalog.GetDataColumnIndex(_dataMapping.Mapping.R.Source);
                    if (gLatColumnIndex >= 0 && gLongColumnIndex >= 0 && rColumnIndex >= 0)
                    {
                        // Spatial mapping and scaling
                        _catalogMaterial.SetBuffer("dataX", _buffers[gLatColumnIndex]);
                        _catalogMaterial.SetBuffer("dataY", _buffers[gLongColumnIndex]);
                        _catalogMaterial.SetBuffer("dataZ", _buffers[rColumnIndex]);
                        // Spherical coordinate input assumes degrees for XY, while the shader assumes radians
                        _catalogMaterial.SetFloat("scalingX", _dataMapping.Mapping.Lat.Scale * Mathf.Deg2Rad);
                        _catalogMaterial.SetFloat("scalingY", _dataMapping.Mapping.Long.Scale * Mathf.Deg2Rad);
                        _catalogMaterial.SetFloat("scalingZ", _dataMapping.Mapping.R.Scale);
                        _catalogMaterial.SetFloat("offsetX", _dataMapping.Mapping.Lat.Offset * Mathf.Deg2Rad);
                        _catalogMaterial.SetFloat("offsetY", _dataMapping.Mapping.Long.Offset * Mathf.Deg2Rad);
                        _catalogMaterial.SetFloat("offsetZ", _dataMapping.Mapping.R.Offset);
                    }
                }
                else
                {
                    int xColumnIndex = _dataCatalog.GetDataColumnIndex(_dataMapping.Mapping.X.Source);
                    int yColumnIndex = _dataCatalog.GetDataColumnIndex(_dataMapping.Mapping.Y.Source);
                    int zColumnIndex = _dataCatalog.GetDataColumnIndex(_dataMapping.Mapping.Z.Source);
                    if (xColumnIndex >= 0 && yColumnIndex >= 0 && zColumnIndex >= 0)
                    {
                        // Spatial mapping and scaling
                        _catalogMaterial.SetBuffer("dataX", _buffers[xColumnIndex]);
                        _catalogMaterial.SetBuffer("dataY", _buffers[yColumnIndex]);
                        _catalogMaterial.SetBuffer("dataZ", _buffers[zColumnIndex]);
                        _catalogMaterial.SetFloat("scalingX", _dataMapping.Mapping.X.Scale);
                        _catalogMaterial.SetFloat("scalingY", _dataMapping.Mapping.Y.Scale);
                        _catalogMaterial.SetFloat("scalingZ", _dataMapping.Mapping.Z.Scale);
                        _catalogMaterial.SetFloat("offsetX", _dataMapping.Mapping.X.Offset);
                        _catalogMaterial.SetFloat("offsetY", _dataMapping.Mapping.Y.Offset);
                        _catalogMaterial.SetFloat("offsetZ", _dataMapping.Mapping.Z.Offset);
                    }

                    if (LineData)
                    {
                        int x2ColumnIndex = _dataCatalog.GetDataColumnIndex(_dataMapping.Mapping.X2.Source);
                        int y2ColumnIndex = _dataCatalog.GetDataColumnIndex(_dataMapping.Mapping.Y2.Source);
                        int z2ColumnIndex = _dataCatalog.GetDataColumnIndex(_dataMapping.Mapping.Z2.Source);
                        if (x2ColumnIndex >= 0 && y2ColumnIndex >= 0 && z2ColumnIndex >= 0)
                        {
                            // Spatial mapping for the end points
                            _catalogMaterial.SetBuffer("dataX2", _buffers[x2ColumnIndex]);
                            _catalogMaterial.SetBuffer("dataY2", _buffers[y2ColumnIndex]);
                            _catalogMaterial.SetBuffer("dataZ2", _buffers[z2ColumnIndex]);                            
                        }
                    }
                }

                int cmapColumnIndex = _dataCatalog.GetDataColumnIndex(_dataMapping.Mapping.Cmap.Source);
                if (cmapColumnIndex >= 0)
                {
                    // Color map mapping and scaling
                    _catalogMaterial.SetBuffer("dataVal", _buffers[cmapColumnIndex]);
                    _catalogMaterial.SetFloat("scaleColorMap", _dataMapping.Mapping.Cmap.Scale);
                    _catalogMaterial.SetFloat("offsetColorMap", _dataMapping.Mapping.Cmap.Offset);
                }
            }

            _colorMapData = new Color[NumColorMapStops];
            SetColorMap(ColorMap);
        }

        public string ScalingString
        {
            get
            {
                string unitString = _dataCatalog.GetColumnDefinition(SphericalCoordinates ? _dataMapping.Mapping.R.Source : _dataMapping.Mapping.X.Source).Unit;
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

            _catalogMaterial.SetColorArray("colorMapData", _colorMapData);
        }

        public void ShiftColorMap(int delta)
        {
            int numColorMaps = ColorMapUtils.NumColorMaps;
            int currentIndex = ColorMap.GetHashCode();
            int newIndex = (currentIndex + delta + numColorMaps) % numColorMaps;
            SetColorMap(ColorMapUtils.FromHashCode(newIndex));
        }

        void Update()
        {
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0, 1, 0, 0.5F);
            Gizmos.DrawWireCube(transform.position, transform.localScale);
        }

        void OnRenderObject()
        {
            // Update the object transform and point scale on the GPU
            _catalogMaterial.SetMatrix("datasetMatrix", transform.localToWorldMatrix);
            _catalogMaterial.SetFloat("pointScale", transform.localScale.x);
            _catalogMaterial.SetInt("scalingTypeX", 0);
            _catalogMaterial.SetInt("scalingTypeY", 0);
            _catalogMaterial.SetInt("scalingTypeZ", 0);
            _catalogMaterial.SetInt("scalingTypeColorMap", 0);
            // Shader defines two passes: Pass #0 uses cartesian coordinates and Pass #1 uses spherical coordinates
            _catalogMaterial.SetPass(_dataMapping.Spherical ? 1 : 0);
            // Render points on the GPU using vertex pulling
            Graphics.DrawProcedural(MeshTopology.Points, _dataCatalog.N);
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