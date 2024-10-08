﻿/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 *
 * This file is part of the iDaVIE project.
 *
 * iDaVIE is free software: you can redistribute it and/or modify it under the terms 
 * of the GNU Lesser General Public License (LGPL) as published by the Free Software 
 * Foundation, either version 3 of the License, or (at your option) any later version.
 *
 * iDaVIE is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR 
 * PURPOSE. See the GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License along with 
 * iDaVIE in the LICENSE file. If not, see <https://www.gnu.org/licenses/>.
 *
 * Additional information and disclaimers regarding liability and third-party 
 * components can be found in the DISCLAIMER and NOTICE files included with this project.
 *
 */
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
        public DataMapping DataMapping;        
        public bool LoadMetaColumns = true;
        [Range(0.0f, 1.0f)] public float ValueCutoffMin = 0;
        [Range(0.0f, 1.0f)] public float ValueCutoffMax = 1;
        public Texture2D ColorMapTexture;
        public Texture2D SpriteSheetTexture;
        
        // Vignette Rendering
        [Header("Vignette Rendering Controls")]
        [Range(0, 0.5f)] public float VignetteFadeStart = 0.15f;
        [Range(0, 0.5f)] public float VignetteFadeEnd = 0.40f;
        [Range(0, 1)] public float VignetteIntensity = 0.0f;
        public Color VignetteColor = Color.black;
        
        private ComputeBuffer[] _buffers;

        // The mapping buffer is used to store mapping configuration. Since each mapping has a similar set of options,
        // it's less verbose than storing a huge number of options individually
        private ComputeBuffer _mappingConfigBuffer;
        private readonly GPUMappingConfig[] _mappingConfigs = new GPUMappingConfig[7];
        private FileTypes _fileType = FileTypes.Ipac;

        private CatalogDataSet _dataSet;
        private Material _catalogMaterial;
        private ColorMapEnum _appliedColorMap = ColorMapEnum.None;
        
        private bool _visible = true;
        private Vector3 _initialLocalPosition;
        private Quaternion _initialLocalRotation;
        private Vector3 _initialLocalScale;
        private float _initialOpacity;

        #region Material Property IDs

        private int _idSpriteSheet, _idNumSprites, _idColorMap, _idColorMapIndex, _idNumColorMaps, _idDataSetMatrix, _idScalingFactor;
        private int _idDataX, _idDataY, _idDataZ, _idDataX2, _idDataY2, _idDataZ2, _idDataCmap, _idDataOpacity, _idDataPointSize, _idDataPointShape;
        private int _idCutoffMin, _idCutoffMax;
        private int _idUseUniformColor, _idUseUniformOpacity, _idUseUniformPointSize, _idUseUniformPointShape;
        private int _idColor, _idOpacity, _idPointSize, _idPointShape;
        private int _idMappingConfigs;
        private int _idVignetteFadeStart, _idVignetteFadeEnd, _idVignetteIntensity, _idVignetteColor;


        private void GetPropertyIds()
        {
            _idSpriteSheet = Shader.PropertyToID("_SpriteSheet");
            _idNumSprites = Shader.PropertyToID("_NumSprites");
            _idColorMap = Shader.PropertyToID("colorMap");
            _idColorMapIndex = Shader.PropertyToID("colorMapIndex");
            _idNumColorMaps = Shader.PropertyToID("numColorMaps");
            _idDataSetMatrix = Shader.PropertyToID("datasetMatrix");
            _idScalingFactor = Shader.PropertyToID("scalingFactor");

            _idDataX = Shader.PropertyToID("dataX");
            _idDataY = Shader.PropertyToID("dataY");
            _idDataZ = Shader.PropertyToID("dataZ");
            _idDataX2 = Shader.PropertyToID("dataX2");
            _idDataY2 = Shader.PropertyToID("dataY2");
            _idDataZ2 = Shader.PropertyToID("dataZ2");
            _idDataCmap = Shader.PropertyToID("dataCmap");
            _idDataOpacity = Shader.PropertyToID("dataOpacity");
            _idDataPointSize = Shader.PropertyToID("dataPointSize");
            _idDataPointShape = Shader.PropertyToID("dataPointShape");

            _idCutoffMin = Shader.PropertyToID("cutoffMin");
            _idCutoffMax = Shader.PropertyToID("cutoffMax");
            
            _idUseUniformColor = Shader.PropertyToID("useUniformColor");
            _idUseUniformOpacity = Shader.PropertyToID("useUniformOpacity");
            _idUseUniformPointSize = Shader.PropertyToID("useUniformPointSize");
            _idUseUniformPointShape = Shader.PropertyToID("useUniformPointShape");

            _idColor = Shader.PropertyToID("color");
            _idOpacity = Shader.PropertyToID("opacity");
            _idPointSize = Shader.PropertyToID("pointSize");
            _idPointShape = Shader.PropertyToID("pointShape");

            _idMappingConfigs = Shader.PropertyToID("mappingConfigs");
            
            _idVignetteFadeStart = Shader.PropertyToID("VignetteFadeStart");
            _idVignetteFadeEnd = Shader.PropertyToID("VignetteFadeEnd");
            _idVignetteIntensity = Shader.PropertyToID("VignetteIntensity");
            _idVignetteColor = Shader.PropertyToID("VignetteColor");
        }

        #endregion

        void Start()
        {
            FileInfo fileInfoOriginal = new FileInfo(TableFileName);
            if (!fileInfoOriginal.Exists)
            {
                Debug.Log($"File {TableFileName} not found");
                return;
            }

            if (string.IsNullOrEmpty(MappingFileName))
            {
                MappingFileName = $"{TableFileName}.json";
            }

            FileInfo fileInfoMapping = new FileInfo(MappingFileName);
            if (fileInfoMapping.Exists)
            {
                DataMapping = DataMapping.CreateFromFile(MappingFileName);
            }
            else
            {
                DataMapping = DataMapping.DefaultXyzMapping;
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
                string fileExt = Path.GetExtension(TableFileName).ToUpper();
                Stopwatch sw = new Stopwatch();
                sw.Start();
                switch (fileExt)
                {
                    case ".TBL":
                        _fileType = FileTypes.Ipac;
                        _dataSet = CatalogDataSet.LoadIpacTable(TableFileName);
                        _dataSet.WriteCacheFile();
                        break;
                    case ".FITS":
                    case ".FIT":
                        _fileType = FileTypes.Fits;
                        _dataSet = CatalogDataSet.LoadFitsTable(TableFileName, LoadMetaColumns);
                        break;
                    default:
                        Debug.Log($"Unrecognized file type!");
                        break;
                }
                sw.Stop();
                Debug.Log($"Table read in {sw.Elapsed.TotalSeconds} seconds");
            }

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
                if (DataMapping.RenderType == RenderType.Line)
                {
                    _catalogMaterial = new Material(Shader.Find("IDIA/CatalogLine"));
                }
                else
                {
                    _catalogMaterial = new Material(Shader.Find("IDIA/CatalogPoint"));
                    _catalogMaterial.SetTexture(_idSpriteSheet, SpriteSheetTexture);
                    _catalogMaterial.SetInt(_idNumSprites, 8);
                }

                _catalogMaterial.SetTexture(_idColorMap, ColorMapTexture);
                // Buffer holds XYZ, cmap, pointSize, pointShape and opacity mapping configs               
                _mappingConfigBuffer = new ComputeBuffer(32 * 7, 32);
                _catalogMaterial.SetBuffer(_idMappingConfigs, _mappingConfigBuffer);

                // Apply scaling from data set space to world space
                transform.localScale *= DataMapping.Uniforms.Scale;
                Debug.Log($"Scaling from data set space to world space: {ScalingString}");
                
                UpdateMappingColumns(true);
                UpdateMappingValues();
            }

            if (!DataMapping.UniformColor)
            {
                SetColorMap(DataMapping.ColorMap);
            }
            _initialLocalPosition = transform.localPosition;
            _initialLocalRotation = transform.localRotation;
            _initialLocalScale = transform.localScale;
            _initialOpacity = DataMapping.Uniforms.Opacity;
        }

        public bool UpdateMappingColumns(bool logErrors = false)
        {
            // Set the color map buffer if we're not using a uniform color
            if (!DataMapping.UniformColor)
            {
                if (DataMapping.Mapping.Cmap != null)
                {
                    int cmapColumnIndex = _dataSet.GetDataColumnIndex(DataMapping.Mapping.Cmap.Source);
                    if (cmapColumnIndex >= 0)
                    {
                        _catalogMaterial.SetBuffer(_idDataCmap, _buffers[cmapColumnIndex]);
                    }
                    else
                    {
                        if (logErrors)
                        {
                            Debug.Log($"Can't find column {DataMapping.Mapping.Cmap.Source} (mapped to Cmap)");
                        }

                        return false;
                    }
                }
                else
                {
                    if (logErrors)
                    {
                        Debug.Log("No mapping for Cmap");
                    }

                    return false;
                }
            }

            // Set the opacity buffer if we're not using a uniform opacity
            if (!DataMapping.UniformOpacity)
            {
                if (DataMapping.Mapping.Opacity != null)
                {
                    int opacityColumnIndex = _dataSet.GetDataColumnIndex(DataMapping.Mapping.Opacity.Source);
                    if (opacityColumnIndex >= 0)
                    {
                        _catalogMaterial.SetBuffer(_idDataOpacity, _buffers[opacityColumnIndex]);
                    }
                    else
                    {
                        if (logErrors)
                        {
                            Debug.Log($"Can't find column {DataMapping.Mapping.Opacity.Source} (mapped to Opacity)");
                        }

                        return false;
                    }
                }
                else
                {
                    if (logErrors)
                    {
                        Debug.Log("No mapping for Opacity");
                    }

                    return false;
                }
            }

            // Set the point size buffer if we're not using a uniform point size
            if (DataMapping.RenderType == RenderType.Billboard && !DataMapping.UniformPointSize)
            {
                if (DataMapping.Mapping.PointSize != null)
                {
                    int pointSizeColumnIndex = _dataSet.GetDataColumnIndex(DataMapping.Mapping.PointSize.Source);
                    if (pointSizeColumnIndex >= 0)
                    {
                        _catalogMaterial.SetBuffer(_idDataPointSize, _buffers[pointSizeColumnIndex]);
                    }
                    else
                    {
                        if (logErrors)
                        {
                            Debug.Log($"Can't find column {DataMapping.Mapping.PointSize.Source} (mapped to PointSize)");
                        }

                        return false;
                    }
                }
                else
                {
                    if (logErrors)
                    {
                        Debug.Log("No mapping for PointSize");
                    }

                    return false;
                }
            }

            // Set the point shape buffer if we're not using a uniform point shape
            if (DataMapping.RenderType == RenderType.Billboard && !DataMapping.UniformPointShape)
            {
                if (DataMapping.Mapping.PointShape != null)
                {
                    int pointShapeColumnIndex = _dataSet.GetDataColumnIndex(DataMapping.Mapping.PointShape.Source);
                    if (pointShapeColumnIndex >= 0)
                    {
                        _catalogMaterial.SetBuffer(_idDataPointShape, _buffers[pointShapeColumnIndex]);
                    }
                    else
                    {
                        if (logErrors)
                        {
                            Debug.Log($"Can't find column {DataMapping.Mapping.PointShape.Source} (mapped to PointShape)");
                        }

                        return false;
                    }
                }
                else
                {
                    if (logErrors)
                    {
                        Debug.Log("No mapping for PointShape");
                    }

                    return false;
                }
            }

            // Update Spherical mapping buffers if we're using spherical coordinates
            if (DataMapping.Spherical)
            {
                if (DataMapping.Mapping.Lat != null && DataMapping.Mapping.Lng != null && DataMapping.Mapping.R != null)
                {
                    int gLatColumnIndex = _dataSet.GetDataColumnIndex(DataMapping.Mapping.Lat.Source);
                    int gLongColumnIndex = _dataSet.GetDataColumnIndex(DataMapping.Mapping.Lng.Source);
                    int rColumnIndex = _dataSet.GetDataColumnIndex(DataMapping.Mapping.R.Source);
                    if (gLatColumnIndex >= 0 && gLongColumnIndex >= 0 && rColumnIndex >= 0)
                    {
                        // Spatial mapping and scaling
                        _catalogMaterial.SetBuffer(_idDataX, _buffers[gLatColumnIndex]);
                        _catalogMaterial.SetBuffer(_idDataY, _buffers[gLongColumnIndex]);
                        _catalogMaterial.SetBuffer(_idDataZ, _buffers[rColumnIndex]);
                        return true;
                    }
                    else
                    {
                        if (logErrors)
                        {
                            Debug.Log($"Can't find columns {DataMapping.Mapping.Lat.Source}, {DataMapping.Mapping.Lng.Source} and {DataMapping.Mapping.R.Source} (mapped to Lat, Long and R)");
                        }

                        return false;
                    }
                }

                if (logErrors)
                {
                    Debug.Log("Can't find mappings for Lat, Long and R");
                }

                return false;
            }

            // Otherwise default to Cartesian coordinates and mapping buffers
            if (DataMapping.Mapping.X != null && DataMapping.Mapping.Y != null && DataMapping.Mapping.Z != null)
            {
                int xColumnIndex = _dataSet.GetDataColumnIndex(DataMapping.Mapping.X.Source);
                int yColumnIndex = _dataSet.GetDataColumnIndex(DataMapping.Mapping.Y.Source);
                int zColumnIndex = _dataSet.GetDataColumnIndex(DataMapping.Mapping.Z.Source);
                if (xColumnIndex >= 0 && yColumnIndex >= 0 && zColumnIndex >= 0)
                {
                    // Spatial mapping and scaling
                    _catalogMaterial.SetBuffer(_idDataX, _buffers[xColumnIndex]);
                    _catalogMaterial.SetBuffer(_idDataY, _buffers[yColumnIndex]);
                    _catalogMaterial.SetBuffer(_idDataZ, _buffers[zColumnIndex]);
                }
                else
                {
                    if (logErrors)
                    {
                        Debug.Log($"Can't find columns {DataMapping.Mapping.X.Source}, {DataMapping.Mapping.Y.Source} and {DataMapping.Mapping.Z.Source} (mapped to X, Y and Z)");
                    }

                    return false;
                }

                // Update Line mapping buffers if we're rendering a line
                if (DataMapping.RenderType == RenderType.Line)
                {
                    if (DataMapping.Mapping.X2 != null && DataMapping.Mapping.Y2 != null && DataMapping.Mapping.Z2 != null)
                    {
                        int x2ColumnIndex = _dataSet.GetDataColumnIndex(DataMapping.Mapping.X2.Source);
                        int y2ColumnIndex = _dataSet.GetDataColumnIndex(DataMapping.Mapping.Y2.Source);
                        int z2ColumnIndex = _dataSet.GetDataColumnIndex(DataMapping.Mapping.Z2.Source);
                        if (x2ColumnIndex >= 0 && y2ColumnIndex >= 0 && z2ColumnIndex >= 0)
                        {
                            // Spatial mapping for the end points
                            _catalogMaterial.SetBuffer(_idDataX2, _buffers[x2ColumnIndex]);
                            _catalogMaterial.SetBuffer(_idDataY2, _buffers[y2ColumnIndex]);
                            _catalogMaterial.SetBuffer(_idDataZ2, _buffers[z2ColumnIndex]);
                            return true;
                        }
                        else
                        {
                            if (logErrors)
                            {
                                Debug.Log($"Can't find columns {DataMapping.Mapping.X2.Source}, {DataMapping.Mapping.Y2.Source} and {DataMapping.Mapping.Z2.Source} (mapped to X2, Y2 and Z2)");
                            }

                            return false;
                        }
                    }
                    else
                    {
                        if (logErrors)
                        {
                            Debug.Log("Can't find mappings for X2, Y2 and Z2");
                        }

                        return false;
                    }
                }

                return true;
            }

            if (logErrors)
            {
                Debug.Log("Can't find mappings for X, Y and Z");
            }

            return false;
        }

        public float GetInitialOpacity()
        {
            return _initialOpacity;
        }

        public void SetOpacity(float opacity)
        {
            DataMapping.Uniforms.Opacity = opacity;
        }

        public void SetVisibility(bool visible)
        {
            _visible = visible;
        }

        public void resetLocalPosition()
        {
            transform.localPosition = _initialLocalPosition;
            transform.localRotation = _initialLocalRotation;
            transform.localScale = _initialLocalScale;
        }

        public bool UpdateMappingValues()
        {
            if (!_catalogMaterial)
            {
                return false;
            }

            if (!DataMapping.UniformColor && DataMapping.Mapping.Cmap != null && !string.IsNullOrEmpty(DataMapping.Mapping.Cmap.Source))
            {
                _catalogMaterial.SetInt(_idUseUniformColor, 0);
                _mappingConfigs[3] = DataMapping.Mapping.Cmap.GpuMappingConfig;
                if (_appliedColorMap != DataMapping.ColorMap)
                {
                    _appliedColorMap = DataMapping.ColorMap;
                    int colorMapIndex = _appliedColorMap.GetHashCode();                    
                    OnColorMapChanged?.Invoke(_appliedColorMap);
                    _catalogMaterial.SetFloat(_idColorMapIndex, colorMapIndex);
                    _catalogMaterial.SetInt(_idNumColorMaps, ColorMapUtils.NumColorMaps);
                }                
            }
            else
            {
                _catalogMaterial.SetInt(_idUseUniformColor, 1);
                _catalogMaterial.SetColor(_idColor, DataMapping.Uniforms.Color);
            }

            if (!DataMapping.UniformOpacity && DataMapping.Mapping.Opacity != null && !string.IsNullOrEmpty(DataMapping.Mapping.Opacity.Source))
            {
                _catalogMaterial.SetInt(_idUseUniformOpacity, 0);
                if (_visible)
                    _mappingConfigs[4] = DataMapping.Mapping.Opacity.GpuMappingConfig;
                else
                    _catalogMaterial.SetFloat(_idOpacity, 0);
            }
            else
            { 
                _catalogMaterial.SetInt(_idUseUniformOpacity, 1);
                if (_visible)
                    _catalogMaterial.SetFloat(_idOpacity, DataMapping.Uniforms.Opacity);
                else
                    _catalogMaterial.SetFloat(_idOpacity, 0);
            }


            if (!DataMapping.UniformPointSize && DataMapping.Mapping.PointSize != null && !string.IsNullOrEmpty(DataMapping.Mapping.PointSize.Source))
            {
                _catalogMaterial.SetInt(_idUseUniformPointSize, 0);
                _mappingConfigs[5] = DataMapping.Mapping.PointSize.GpuMappingConfig;
            }
            else
            {
                _catalogMaterial.SetInt(_idUseUniformPointSize, 1);
                _catalogMaterial.SetFloat(_idPointSize, DataMapping.Uniforms.PointSize);
            }

            if (!DataMapping.UniformPointShape && DataMapping.Mapping.PointShape != null && !string.IsNullOrEmpty(DataMapping.Mapping.PointShape.Source))
            {
                _catalogMaterial.SetInt(_idUseUniformPointShape, 0);
                _mappingConfigs[6] = DataMapping.Mapping.PointShape.GpuMappingConfig;
            }
            else
            {
                _catalogMaterial.SetInt(_idUseUniformPointShape, 1);
                float shapeIndex = DataMapping.Uniforms.PointShape.GetHashCode();
                _catalogMaterial.SetFloat(_idPointShape, shapeIndex);
            }

            // Update spherical mapping properties if we're using spherical coordinates
            if (DataMapping.Spherical)
            {
                if (DataMapping.Mapping.Lat != null && DataMapping.Mapping.Lng != null && DataMapping.Mapping.R != null)
                {
                    // Spherical coordinate input assumes degrees for XY, while the shader assumes radians
                    var latConfig = DataMapping.Mapping.Lat.GpuMappingConfig;
                    latConfig.Scale *= Mathf.Deg2Rad;
                    latConfig.Offset *= Mathf.Deg2Rad;

                    var lngConfig = DataMapping.Mapping.Lng.GpuMappingConfig;
                    lngConfig.Scale *= Mathf.Deg2Rad;
                    lngConfig.Offset *= Mathf.Deg2Rad;

                    _mappingConfigs[0] = latConfig;
                    _mappingConfigs[1] = lngConfig;
                    _mappingConfigs[2] = DataMapping.Mapping.R.GpuMappingConfig;
                    _mappingConfigBuffer.SetData(_mappingConfigs);
                    return true;
                }

                return false;
            }

            // Otherwise default to Cartesian coordinates and update properties
            if (DataMapping.Mapping.X != null && DataMapping.Mapping.Y != null && DataMapping.Mapping.Z != null)
            {
                // Spatial mapping and scaling
                _mappingConfigs[0] = DataMapping.Mapping.X.GpuMappingConfig;
                _mappingConfigs[1] = DataMapping.Mapping.Y.GpuMappingConfig;
                _mappingConfigs[2] = DataMapping.Mapping.Z.GpuMappingConfig;
                _mappingConfigBuffer.SetData(_mappingConfigs);
                return true;
            }

            return false;
        }

        public string ScalingString
        {
            get
            {
                string unitString = _dataSet.GetColumnDefinition(DataMapping.Spherical ? DataMapping.Mapping.R.Source : DataMapping.Mapping.X.Source).Unit;
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
            DataMapping.ColorMap = newColorMap;
//            int numColorMaps = ColorMapUtils.NumColorMaps;
//            float colorMapPixelDeltaX = (float) (ColorMapTexture.width) / NumColorMapStops;
//            float colorMapPixelDeltaY = (float) (ColorMapTexture.height) / numColorMaps;
//            int colorMapIndex = newColorMap.GetHashCode();
//
//            for (var i = 0; i < NumColorMapStops; i++)
//            {
//                _colorMapData[i] = ColorMapTexture.GetPixel((int) (i * colorMapPixelDeltaX), (int) (colorMapIndex * colorMapPixelDeltaY));
//            }
//
//            _catalogMaterial.SetColorArray(_idColorMapData, _colorMapData);
        }

        public void ShiftColorMap(int delta)
        {
            int numColorMaps = ColorMapUtils.NumColorMaps;
            int currentIndex = DataMapping.ColorMap.GetHashCode();
            int newIndex = (currentIndex + delta + numColorMaps) % numColorMaps;
            SetColorMap(ColorMapUtils.FromHashCode(newIndex));
            OnColorMapChanged?.Invoke(DataMapping.ColorMap);
        }

        void Update()
        {
            if (_catalogMaterial != null)
            {
                _catalogMaterial.SetFloat(_idCutoffMin, ValueCutoffMin);
                _catalogMaterial.SetFloat(_idCutoffMax, ValueCutoffMax);
            }
            
            _catalogMaterial.SetFloat(_idVignetteFadeStart, VignetteFadeStart);
            _catalogMaterial.SetFloat(_idVignetteFadeEnd, VignetteFadeEnd);
            _catalogMaterial.SetFloat(_idVignetteIntensity, VignetteIntensity);
            _catalogMaterial.SetColor(_idVignetteColor, VignetteColor);

            UpdateMappingValues();
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
            _catalogMaterial.SetFloat(_idScalingFactor, transform.localScale.x);
            // Shader defines two passes: Pass #0 uses cartesian coordinates and Pass #1 uses spherical coordinates
            _catalogMaterial.SetPass(DataMapping.Spherical ? 1 : 0);
            // Render points on the GPU using vertex pulling
            Graphics.DrawProceduralNow(MeshTopology.Points, _dataSet.N);
        }

        void OnDestroy()
        {
            _mappingConfigBuffer.Release();
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