using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;
using Debug = UnityEngine.Debug;


namespace VolumeData
{
    public class VolumeDataSet
    {
        public Texture3D DataCube { get; private set; }
        public Texture3D RegionCube { get; private set; }
        public string FileName { get; private set; }
        public long XDim { get; private set; }
        public long YDim { get; private set; }
        public long ZDim { get; private set; }

        public float CubeMin { get; private set; }
        public float CubeMax { get; private set; }

        public long NumPoints => XDim * YDim * ZDim;
        public long[] Dims => new[] {XDim, YDim, ZDim};

        private IntPtr _fitsCubeData;

        public static VolumeDataSet LoadDataFromFitsFile(string fileName)
        {
            VolumeDataSet volumeDataSet = new VolumeDataSet();
            IntPtr fptr;
            int status = 0;
            int cubeDimensions;
            IntPtr dataPtr;
            if (FitsReader.FitsOpenFile(out fptr, fileName, out status) != 0)
            {
                Debug.Log("Fits open failure... code #" + status.ToString());
            }
            if (FitsReader.FitsGetImageDims(fptr, out cubeDimensions, out status) != 0)
            {
                Debug.Log("Fits read image dimensions failed... code #" + status.ToString());
            }
            if (cubeDimensions < 3)
            {
                Debug.Log("Only " + cubeDimensions.ToString() +
                          " found. Please use Fits cube with at least 3 dimensions.");
            }
            if (FitsReader.FitsGetImageSize(fptr, cubeDimensions, out dataPtr, out status) != 0)
            {
                Debug.Log("Fits Read cube size error #" + status.ToString());
                FitsReader.FitsCloseFile(fptr, out status);
            }
            long[] cubeSize = new long[cubeDimensions];
            Marshal.Copy(dataPtr, cubeSize, 0, cubeDimensions);
            FitsReader.FreeMemory(dataPtr);
            long numberDataPoints = cubeSize[0] * cubeSize[1] * cubeSize[2];
            IntPtr fitsDataPtr;
            if (FitsReader.FitsReadImageFloat(fptr, cubeDimensions, numberDataPoints, out fitsDataPtr, out status) != 0)
            {
                Debug.Log("Fits Read cube data error #" + status.ToString());
                FitsReader.FitsCloseFile(fptr, out status);
            }
            FitsReader.FitsCloseFile(fptr, out status);
            volumeDataSet._fitsCubeData = fitsDataPtr;
            volumeDataSet.XDim = cubeSize[0];
            volumeDataSet.YDim = cubeSize[1];
            volumeDataSet.ZDim = cubeSize[2];
            volumeDataSet.findMinAndMax();


            return volumeDataSet;
        }

        public void GenerateVolumeTexture(TextureFilterEnum textureFilter, int xDownsample, int yDownsample, int zDownsample)
        {
            IntPtr reducedData;
            bool downsampled = false;
            if (xDownsample != 1 || yDownsample != 1 || zDownsample != 1)
            {
                if (DataAnalysis.DataDownsampleByFactor(_fitsCubeData, out reducedData, XDim, YDim, ZDim, xDownsample, yDownsample, zDownsample) != 0)
                {
                    Debug.Log("Data cube downsample error!");
                }
                downsampled = true;
            }
            else
                reducedData = _fitsCubeData;
            int[] cubeSize = new int[3];    //assume 3D cube
            cubeSize[0] = (int) XDim / xDownsample;
            cubeSize[1] = (int) YDim / yDownsample;
            cubeSize[2] = (int) ZDim / zDownsample;
            if (XDim % xDownsample != 0)
                cubeSize[0]++;
            if (YDim % yDownsample != 0)
                cubeSize[1]++;
            if (ZDim % zDownsample != 0)
                cubeSize[2]++;
            Texture3D dataCube = new Texture3D(cubeSize[0], cubeSize[1], cubeSize[2], TextureFormat.RFloat, false);
            switch (textureFilter)
            {
                case TextureFilterEnum.Point:
                    dataCube.filterMode = FilterMode.Point;
                    break;
                case TextureFilterEnum.Bilinear:
                    dataCube.filterMode = FilterMode.Bilinear;
                    break;
                case TextureFilterEnum.Trilinear:
                    dataCube.filterMode = FilterMode.Trilinear;
                    break;
            }
            int sliceSize = cubeSize[0] * cubeSize[1];
            Texture2D textureSlice = new Texture2D(cubeSize[0], cubeSize[1], TextureFormat.RFloat, false);
            for (int slice = 0; slice < cubeSize[2]; slice++)
            {
                textureSlice.LoadRawTextureData(IntPtr.Add(reducedData, slice * sliceSize * sizeof(float)),
                    sliceSize * sizeof(float));
                textureSlice.Apply();
                Graphics.CopyTexture(textureSlice, 0, 0, 0, 0, cubeSize[0], cubeSize[1], dataCube, slice, 0, 0, 0);
            }
            DataCube = dataCube;
            //TODO output cached file
            if (downsampled)
                DataAnalysis.FreeMemory(reducedData);
        }
        
        public void GenerateCroppedVolumeTexture(TextureFilterEnum textureFilter, Vector3Int cropStart, Vector3Int cropEnd, Vector3Int downsample)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            IntPtr regionData;
            if (DataAnalysis.DataCropAndDownsample(_fitsCubeData, out regionData, XDim, YDim, ZDim, cropStart.x, cropStart.y, cropStart.z, 
                cropEnd.x, cropEnd.y, cropEnd.z, downsample.x, downsample.y, downsample.z) != 0)                
            {
                Debug.Log("Data cube downsample error!");
            }

            Vector3Int cubeSize = new Vector3Int();
            cubeSize.x = (Math.Abs(cropStart.x - cropEnd.x) + 1) / downsample.x;
            cubeSize.y = (Math.Abs(cropStart.y - cropEnd.y) + 1) / downsample.y;
            cubeSize.z = (Math.Abs(cropStart.z - cropEnd.z) + 1) / downsample.z;
            if ((Math.Abs(cropStart.x - cropEnd.x) + 1) % downsample.x != 0)
                cubeSize.x++;
            if ((Math.Abs(cropStart.x - cropEnd.x) + 1) % downsample.y != 0)
                cubeSize.y++;
            if ((Math.Abs(cropStart.x - cropEnd.x) + 1) % downsample.z != 0)
                cubeSize.z++;
                               
            RegionCube = new Texture3D(cubeSize.x, cubeSize.y, cubeSize.z, TextureFormat.RFloat, false);
            switch (textureFilter)
            {
                case TextureFilterEnum.Point:
                    RegionCube.filterMode = FilterMode.Point;
                    break;
                case TextureFilterEnum.Bilinear:
                    RegionCube.filterMode = FilterMode.Bilinear;
                    break;
                case TextureFilterEnum.Trilinear:
                    RegionCube.filterMode = FilterMode.Trilinear;
                    break;
            }
            int sliceSize = cubeSize.x * cubeSize.y;
            Texture2D textureSlice = new Texture2D(cubeSize.x, cubeSize.y, TextureFormat.RFloat, false);
            
            for (int slice = 0; slice < cubeSize.z; slice++)
            {
                textureSlice.LoadRawTextureData(IntPtr.Add(regionData, slice * sliceSize * sizeof(float)),sliceSize * sizeof(float));
                textureSlice.Apply();
                Graphics.CopyTexture(textureSlice, 0, 0, 0, 0, cubeSize.x, cubeSize.y, RegionCube, slice, 0, 0, 0);
            }
            DataAnalysis.FreeMemory(regionData);
            sw.Stop();            
            Debug.Log($"Cropped into {cubeSize.x} x {cubeSize.y} x {cubeSize.z} region ({cubeSize.x * cubeSize.y * cubeSize.z * 4e-6} MB) in {sw.ElapsedMilliseconds} ms");
        }

        public float GetValue(int x, int y, int z)
        {
            if (x < 1 || x > XDim || y < 1 || y > YDim || z < 1 || z > ZDim)
            {
                return float.NaN;
            }

            float val;
            DataAnalysis.GetVoxelValue(_fitsCubeData, out val, (int)XDim, (int)YDim, (int)ZDim, x, y, z);
            return val;
        }

        private void findMinAndMax()
        {
            long numberDataPoints = XDim * YDim * ZDim;
            float minVal;
            float maxVal;
            DataAnalysis.FindMaxMin(_fitsCubeData, numberDataPoints, out maxVal, out minVal);
            CubeMin = minVal;
            CubeMax = maxVal;
            Debug.Log("max and min vals: " + CubeMax + " and " + CubeMin);
        }

        public void FindDownsampleFactors(long maxCubeSizeInMb, out int xFactor, out int yFactor, out int zFactor)
        {
            FindDownsampleFactors(maxCubeSizeInMb, XDim, YDim, ZDim, out xFactor, out yFactor, out zFactor);
        }

        public void FindDownsampleFactors(long maxCubeSizeInMB, long regionXDim, long regionYDim, long regionZDim, out int xFactor, out int yFactor, out int zFactor)
        {
            xFactor = 1;
            yFactor = 1;
            zFactor = 1;
            while (regionXDim / xFactor > 2048)
                xFactor++;
            while (regionYDim / yFactor > 2048)
                yFactor++;
            while (regionZDim / zFactor > 2048)
                zFactor++;
            long maximumElements = maxCubeSizeInMB * 1000000 / 4;
            while (regionXDim * regionYDim * regionZDim / (xFactor * yFactor * zFactor) > maximumElements)
            {
                if (regionZDim / zFactor > regionXDim / xFactor || regionZDim / zFactor > regionYDim / yFactor)
                    zFactor++;
                else if (regionYDim / yFactor > regionXDim / xFactor)
                    yFactor++;
                else
                    xFactor++;
            }
        }

        public void CleanUp()
        {
            FitsReader.FreeMemory(_fitsCubeData);
        }
    }
}