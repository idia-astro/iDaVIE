using System;
using Interaction.Interfaces;
using VolumeData;
using UnityEngine;

namespace Interaction
{
    public sealed class CursorInfoFormatter : ICursorInfoFormatter
    {
        public string FormatCursorInfo(VolumeDataSetRenderer dataSetRenderer, bool displayOutsideCube)
        {
            VolumeDataSet dataSet = dataSetRenderer.Data;
            if (dataSet == null)
            {
                return string.Empty;
            }

            var voxelCoordinate = dataSetRenderer.CursorVoxel;
            if (!displayOutsideCube && IsCursorOutsideCube(dataSetRenderer))
            {
                return string.Empty;
            }

            string text = string.Empty;
            if (dataSetRenderer.HasWCS)
            {
                double physX;
                double physY;
                double physZ;
                double normX;
                double normY;
                double normZ = 0;
                var dataCoordinate = dataSetRenderer.GetVoxelPositionDataSpace();
                dataSet.GetFitsCoordsAst(dataCoordinate.x, dataCoordinate.y, dataCoordinate.z, out physX, out physY, out physZ);
                dataSet.GetNormCoords(physX, physY, physZ, out normX, out normY, out normZ);
                text += $"WCS: ({dataSet.GetFormattedCoord(normX, 1)}, {dataSet.GetFormattedCoord(normY, 2)}){Environment.NewLine}";
                text += $"{dataSet.GetAstAttribute("System(3)")}: {dataSet.GetFormattedCoord(normZ, 3),10} {dataSet.GetAstAttribute("Unit(3)")}{Environment.NewLine}";
            }

            text += $"World: ({voxelCoordinate.x,5}, {voxelCoordinate.y,5}, {voxelCoordinate.z,5}){Environment.NewLine}";
            if (dataSet.isSubset())
            {
                Vector3Int dataVoxel = dataSetRenderer.GetVoxelPositionDataSpace();
                text += $"Data: ({dataVoxel.x,5}, {dataVoxel.y,5}, {dataVoxel.z,5}){Environment.NewLine}";
            }

            text += $"Value: {dataSetRenderer.CursorValue,16} {dataSet.GetPixelUnit()}";
            if (dataSet.HasRestFrequency)
            {
                text += $"{Environment.NewLine}{dataSet.GetConvertedDepth(voxelCoordinate.z)}";
            }

            if (dataSetRenderer.CursorSource != 0)
            {
                text += $"{Environment.NewLine}Source: {dataSetRenderer.CursorSource}";
            }

            return text;
        }

        public string FormatSelectionInfo(VolumeDataSetRenderer dataSetRenderer)
        {
            VolumeDataSet dataSet = dataSetRenderer.Data;
            var regionMax = Vector3.Max(dataSetRenderer.RegionStartVoxel, dataSetRenderer.RegionEndVoxel);
            var regionMin = Vector3.Min(dataSetRenderer.RegionStartVoxel, dataSetRenderer.RegionEndVoxel);
            var regionSize = regionMax - regionMin + Vector3.one;
            string text = $"Region: {regionSize.x} x {regionSize.y} x {regionSize.z}{Environment.NewLine}";

            if (dataSetRenderer.HasWCS)
            {
                double xLength;
                double yLength;
                double zLength;
                double angle;
                dataSet.GetFitsLengthsAst(regionMin, regionMax + Vector3.one, out xLength, out yLength, out zLength, out angle);
                text += $"Angle: {FormatAngle(angle)}{Environment.NewLine}";
                text += $"Depth: {dataSet.GetFormattedCoord(Math.Abs(zLength), 3),15} {dataSet.GetAstAttribute("Unit(3)")}";
            }

            return text;
        }

        public string FormatAngle(double angle)
        {
            double deg = angle / Math.PI * 180.0;
            if (deg >= 1)
            {
                return deg.ToString("N3") + "°";
            }

            double angleMin = (deg - Math.Truncate(deg)) * 60;
            double angleSec = Math.Truncate((angleMin - Math.Truncate(angleMin)) * 60 * 100) / 100;
            return Math.Truncate(angleMin).ToString("00") + "'" + angleSec.ToString("00.00") + "\"";
        }

        private static bool IsCursorOutsideCube(VolumeDataSetRenderer dataSetRenderer)
        {
            return dataSetRenderer.CursorVoxel.x < 1 ||
                   dataSetRenderer.CursorVoxel.y < 1 ||
                   dataSetRenderer.CursorVoxel.z < 1 ||
                   dataSetRenderer.CursorVoxel.x > dataSetRenderer.Data.XDim ||
                   dataSetRenderer.CursorVoxel.y > dataSetRenderer.Data.YDim ||
                   dataSetRenderer.CursorVoxel.z > dataSetRenderer.Data.ZDim;
        }
    }
}
