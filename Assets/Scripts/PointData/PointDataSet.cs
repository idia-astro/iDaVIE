using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace PointData
{
    public class PointDataSet : MonoBehaviour
    {
        public string TableFileName;
        public string MappingFileName;
        public ColorMapEnum ColorMap = ColorMapEnum.Inferno;
        public Texture2D ColorMapTexture;
        public Material BillboardMaterial;

        private ComputeBuffer[] _buffers;

        private Color[] _colorMapData;
        private const int NumColorMapStops = 256;

        private DataCatalog _dataCatalog;
        private DataMapping _dataMapping;

        void Start()
        {
            _dataCatalog = new DataCatalog(TableFileName);
            _dataMapping = DataMapping.DefaultXyzMapping;

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

                // Create an instance of the material, so that each data set can have different material parameters
                BillboardMaterial = Instantiate(BillboardMaterial);
                BillboardMaterial.SetInt("numDataPoints", _dataCatalog.N);
                BillboardMaterial.SetFloat("_PointSize", _dataMapping.Defaults.PointSize);
                // Apply scaling from data set space to world space
                transform.localScale *= _dataMapping.Defaults.Scale;
                Debug.Log($"Scaling from data set space to world space: {ScalingString}");

                int xColumnIndex = _dataCatalog.GetDataColumnIndex(_dataMapping.Mapping.X.Source);
                int yColumnIndex = _dataCatalog.GetDataColumnIndex(_dataMapping.Mapping.Y.Source);
                int zColumnIndex = _dataCatalog.GetDataColumnIndex(_dataMapping.Mapping.Z.Source);
                int cmapColumnIndex = _dataCatalog.GetDataColumnIndex(_dataMapping.Mapping.Cmap.Source);

                if (xColumnIndex >= 0 && yColumnIndex >= 0 && zColumnIndex >= 0 && cmapColumnIndex >= 0)
                {
                    // Spatial mapping and scaling
                    BillboardMaterial.SetBuffer("dataX", _buffers[xColumnIndex]);
                    BillboardMaterial.SetBuffer("dataY", _buffers[yColumnIndex]);
                    BillboardMaterial.SetBuffer("dataZ", _buffers[zColumnIndex]);
                    BillboardMaterial.SetFloat("scalingX", _dataMapping.Mapping.X.Scale);
                    BillboardMaterial.SetFloat("scalingY", _dataMapping.Mapping.Y.Scale);
                    BillboardMaterial.SetFloat("scalingZ", _dataMapping.Mapping.Z.Scale);
                    BillboardMaterial.SetFloat("offsetX", _dataMapping.Mapping.X.Offset);
                    BillboardMaterial.SetFloat("offsetY", _dataMapping.Mapping.Y.Offset);
                    BillboardMaterial.SetFloat("offsetZ", _dataMapping.Mapping.Z.Offset);          

                    // Color map mapping and scaling
                    BillboardMaterial.SetBuffer("dataVal", _buffers[cmapColumnIndex]);
                    BillboardMaterial.SetFloat("scaleColorMap", _dataMapping.Mapping.Cmap.Scale);
                    BillboardMaterial.SetFloat("offsetColorMap", _dataMapping.Mapping.Cmap.Offset);
                }
                else
                {
                    Debug.Log($"Problem mapping data catalog file {TableFileName}");
                }
            }

            _colorMapData = new Color[NumColorMapStops];
            SetColorMap(ColorMap);
        }

        public string ScalingString
        {
            get
            {
                string unitString = _dataCatalog.GetColumnDefinition(_dataMapping.Mapping.X.Source).Unit;
                if (unitString.Length == 0)
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

            BillboardMaterial.SetColorArray("colorMapData", _colorMapData);
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
            BillboardMaterial.SetMatrix("datasetMatrix", transform.localToWorldMatrix);
            BillboardMaterial.SetFloat("pointScale", transform.localScale.x);
            BillboardMaterial.SetInt("scalingTypeX", 0);
            BillboardMaterial.SetInt("scalingTypeY", 0);
            BillboardMaterial.SetInt("scalingTypeZ", 0);
            BillboardMaterial.SetInt("scalingTypeColorMap", 0);
            BillboardMaterial.SetPass(0);
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