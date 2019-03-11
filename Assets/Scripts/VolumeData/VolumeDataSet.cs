using System;
using System.Runtime.InteropServices;
using UnityEngine;


namespace VolumeData
{
    public class VolumeDataSet
    {
        public Texture3D DataCube { get; private set; }
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

        public void RenderVolume(TextureFilterEnum textureFilter, int xDownsample, int yDownsample, int zDownsample)
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
            int newCubeLength = cubeSize[0] * cubeSize[1] * cubeSize[2];
            float[] reducedCube = new float[newCubeLength];
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

        public void FindDownsampleFactors(long MaxCubeSizeInMB, out int Xfactor, out int Yfactor, out int Zfactor)
        {
            Xfactor = 1;
            Yfactor = 1;
            Zfactor = 1;
            while (XDim / Xfactor > 2048)
                Xfactor++;
            while (YDim / Yfactor > 2048)
                Yfactor++;
            while (ZDim / Zfactor > 2048)
                Zfactor++;
            long maximumElements = MaxCubeSizeInMB * 1000000 / 4;
            while (XDim * YDim * ZDim / (Xfactor * Yfactor * Zfactor) > maximumElements)
            {
                if (ZDim / Zfactor > XDim / Xfactor || ZDim / Zfactor > YDim / Yfactor)
                    Zfactor++;
                else if (YDim / Yfactor > XDim / Xfactor)
                    Yfactor++;
                else
                    Xfactor++;
            }
        }

        public void CleanUp()
        {
            FitsReader.FreeMemory(_fitsCubeData);
        }
    }
}