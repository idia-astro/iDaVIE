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


        public static VolumeDataSet LoadFromFitsFile(string fileName)
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

            int[] cubeSize = new int[cubeDimensions];
            Marshal.Copy(dataPtr, cubeSize, 0, cubeDimensions);
            FitsReader.FreeMemory(dataPtr);
            long numberDataPoints = cubeSize[0] * cubeSize[1] * cubeSize[2];
            IntPtr fitsDataPtr;
            if (FitsReader.FitsReadImageFloat(fptr, cubeDimensions, numberDataPoints, out fitsDataPtr, out status) != 0)
            {
                Debug.Log("Fits Read cube data error #" + status.ToString());
                FitsReader.FitsCloseFile(fptr, out status);
            }

            Texture3D dataCube = new Texture3D(cubeSize[0], cubeSize[1], cubeSize[2], TextureFormat.RFloat, false);
            int sliceSize = cubeSize[0] * cubeSize[1];
            Texture2D textureSlice = new Texture2D(cubeSize[0], cubeSize[1], TextureFormat.RFloat, false);
            for (int slice = 0; slice < cubeSize[2]; slice++)
            {
                textureSlice.LoadRawTextureData(IntPtr.Add(fitsDataPtr, slice * sliceSize * sizeof(float)),
                    sliceSize * sizeof(float));
                textureSlice.Apply();
                Graphics.CopyTexture(textureSlice, 0, 0, 0, 0, cubeSize[0], cubeSize[1], dataCube, slice, 0, 0, 0);
            }

            volumeDataSet.DataCube = dataCube;
            volumeDataSet._fitsCubeData = fitsDataPtr;
            volumeDataSet.XDim = cubeSize[0];
            volumeDataSet.YDim = cubeSize[1];
            volumeDataSet.ZDim = cubeSize[2];
            volumeDataSet.findMinAndMax();

            return volumeDataSet;
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
    }
}