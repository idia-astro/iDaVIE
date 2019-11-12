using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
        public int XDimDecimal { get; private set; }
        public int YDimDecimal { get; private set; }
        public int ZDimDecimal { get; private set; }

        public int VelDecimal { get; private set; }

        public long NumPoints => XDim * YDim * ZDim;
        public long[] Dims => new[] {XDim, YDim, ZDim};

        public bool IsMask { get; private set; }

        private IDictionary<string, string> _headerDictionary;

            
        private double _xRef, _yRef, _zRef, _xRefPix, _yRefPix, _zRefPix, _xDelt, _yDelt, _zDelt, _rot;
        private string _xCoord, _yCoord, _zCoord, _wcsProj;

        public double NAxis;
        public IntPtr FitsData;



        public long[] cubeSize;



        public static VolumeDataSet LoadDataFromFitsFile(string fileName, bool isMask)
        {
            VolumeDataSet volumeDataSet = new VolumeDataSet();
            volumeDataSet.IsMask = isMask;
            volumeDataSet.FileName = fileName;
            IntPtr fptr;
            int status = 0;
            int cubeDimensions;
            IntPtr dataPtr;
            if (FitsReader.FitsOpenFile(out fptr, fileName, out status) != 0)
            {
                Debug.Log("Fits open failure... code #" + status.ToString());
            }
            if (!isMask)
            {
                volumeDataSet._headerDictionary = FitsReader.ExtractHeaders(fptr, out status);
                volumeDataSet.ParseHeaderDict();
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

            volumeDataSet.cubeSize = new long[cubeDimensions];
            Marshal.Copy(dataPtr, volumeDataSet.cubeSize, 0, cubeDimensions);
            FitsReader.FreeMemory(dataPtr);
            long numberDataPoints = volumeDataSet.cubeSize[0] * volumeDataSet.cubeSize[1] * volumeDataSet.cubeSize[2];
            IntPtr fitsDataPtr;
            if (isMask)
            {
                if (FitsReader.FitsReadImageInt16(fptr, cubeDimensions, numberDataPoints, out fitsDataPtr, out status) != 0)
                {
                    Debug.Log("Fits Read mask cube data error #" + status.ToString());
                    FitsReader.FitsCloseFile(fptr, out status);
                }
            }
                else
            {
                if (FitsReader.FitsReadImageFloat(fptr, cubeDimensions, numberDataPoints, out fitsDataPtr, out status) != 0)
                {
                    Debug.Log("Fits Read cube data error #" + status.ToString());
                    FitsReader.FitsCloseFile(fptr, out status);
                }
            }
            FitsReader.FitsCloseFile(fptr, out status);



            volumeDataSet.FitsData = fitsDataPtr;
            volumeDataSet.XDim = volumeDataSet.cubeSize[0];
            volumeDataSet.YDim = volumeDataSet.cubeSize[1];
            volumeDataSet.ZDim = volumeDataSet.cubeSize[2];
            volumeDataSet.XDimDecimal = volumeDataSet.cubeSize[0].ToString().Length;
            volumeDataSet.YDimDecimal = volumeDataSet.cubeSize[1].ToString().Length;
            volumeDataSet.ZDimDecimal = volumeDataSet.cubeSize[2].ToString().Length;
            return volumeDataSet;
        }

        public void GenerateVolumeTexture(TextureFilterEnum textureFilter, int xDownsample, int yDownsample, int zDownsample)
        {
            TextureFormat textureFormat;
            int elementSize;
            if (IsMask)
            {
                textureFormat = TextureFormat.R16;
                elementSize = sizeof(Int16);
            }
            else
            {
                textureFormat = TextureFormat.RFloat;
                elementSize = sizeof(float);
            }
            IntPtr reducedData;
            bool downsampled = false;
            if (xDownsample != 1 || yDownsample != 1 || zDownsample != 1)
            {
                if (IsMask)
                {
                    if (DataAnalysis.MaskCropAndDownsample(FitsData, out reducedData, XDim, YDim, ZDim, 1, 1, 1, XDim, YDim, ZDim, xDownsample, yDownsample, zDownsample) != 0)
                    {
                        Debug.Log("Data cube downsample error!");
                    }
                }
                else
                {
                    if (DataAnalysis.DataCropAndDownsample(FitsData, out reducedData, XDim, YDim, ZDim, 1, 1, 1, XDim, YDim, ZDim, xDownsample, yDownsample, zDownsample) != 0)
                    {
                        Debug.Log("Data cube downsample error!");
                    }
                }
                downsampled = true;
            }
            else
                reducedData = FitsData;
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
            Texture3D dataCube = new Texture3D(cubeSize[0], cubeSize[1], cubeSize[2], textureFormat, false);
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
            Texture2D textureSlice = new Texture2D(cubeSize[0], cubeSize[1], textureFormat, false);
            for (int slice = 0; slice < cubeSize[2]; slice++)
            {
                textureSlice.LoadRawTextureData(IntPtr.Add(reducedData, slice * sliceSize * elementSize),
                    sliceSize * elementSize);
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
            TextureFormat textureFormat;
            int elementSize;
            IntPtr regionData;
            if (IsMask)
            {
                textureFormat = TextureFormat.R16;
                elementSize = sizeof(Int16);
                if (DataAnalysis.MaskCropAndDownsample(FitsData, out regionData, XDim, YDim, ZDim, cropStart.x, cropStart.y, cropStart.z,
                    cropEnd.x, cropEnd.y, cropEnd.z, downsample.x, downsample.y, downsample.z) != 0)
                {
                    Debug.Log("Mask cube downsample error!");
                }
            }
            else
            {
                textureFormat = TextureFormat.RFloat;
                elementSize = sizeof(float);
                if (DataAnalysis.DataCropAndDownsample(FitsData, out regionData, XDim, YDim, ZDim, cropStart.x, cropStart.y, cropStart.z,
                    cropEnd.x, cropEnd.y, cropEnd.z, downsample.x, downsample.y, downsample.z) != 0)
                {
                    Debug.Log("Data cube downsample error!");
                }
            }
            Vector3Int cubeSize = new Vector3Int();
            cubeSize.x = (Math.Abs(cropStart.x - cropEnd.x) + 1) / downsample.x;
            cubeSize.y = (Math.Abs(cropStart.y - cropEnd.y) + 1) / downsample.y;
            cubeSize.z = (Math.Abs(cropStart.z - cropEnd.z) + 1) / downsample.z;
            if ((Math.Abs(cropStart.x - cropEnd.x) + 1) % downsample.x != 0)
                cubeSize.x++;
            if ((Math.Abs(cropStart.y - cropEnd.y) + 1) % downsample.y != 0)
                cubeSize.y++;
            if ((Math.Abs(cropStart.z - cropEnd.z) + 1) % downsample.z != 0)
                cubeSize.z++;
                               
            RegionCube = new Texture3D(cubeSize.x, cubeSize.y, cubeSize.z, textureFormat, false);
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
            Texture2D textureSlice = new Texture2D(cubeSize.x, cubeSize.y, textureFormat, false);
            
            for (int slice = 0; slice < cubeSize.z; slice++)
            {
                textureSlice.LoadRawTextureData(IntPtr.Add(regionData, slice * sliceSize * elementSize),sliceSize * elementSize);
                textureSlice.Apply();
                Graphics.CopyTexture(textureSlice, 0, 0, 0, 0, cubeSize.x, cubeSize.y, RegionCube, slice, 0, 0, 0);
            }
            DataAnalysis.FreeMemory(regionData);
            sw.Stop();            
            Debug.Log($"Cropped into {cubeSize.x} x {cubeSize.y} x {cubeSize.z} region ({cubeSize.x * cubeSize.y * cubeSize.z * 4e-6} MB) in {sw.ElapsedMilliseconds} ms");
        }

        public IDictionary<string, string> GetHeaderDictionary()
        {
            Debug.Log(_headerDictionary);
            return _headerDictionary;
        }

        public float GetDataValue(int x, int y, int z)
        {
            if (x < 1 || x > XDim || y < 1 || y > YDim || z < 1 || z > ZDim)
            {
                return float.NaN;
            }

            float val;
            DataAnalysis.GetVoxelFloatValue(FitsData, out val, (int)XDim, (int)YDim, (int)ZDim, x, y, z);
            return val;
        }

        public Int16 GetMaskValue(int x, int y, int z)
        {
            if (x < 1 || x > XDim || y < 1 || y > YDim || z < 1 || z > ZDim)
            {
                return 0;
            }

            Int16 val;
            DataAnalysis.GetVoxelInt16Value(FitsData, out val, (int)XDim, (int)YDim, (int)ZDim, x, y, z);
            return val;
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
            while (regionXDim / xFactor > 2048 || regionYDim / yFactor > 2048)
            {
                xFactor++;
                yFactor++;
            }
            while (regionZDim / zFactor > 2048)
                zFactor++;
            long maximumElements = maxCubeSizeInMB * 1000000 / 4;
            while (regionXDim * regionYDim * regionZDim / (xFactor * yFactor * zFactor) > maximumElements)
            {
                if (regionZDim / zFactor > regionXDim / xFactor || regionZDim / zFactor > regionYDim / yFactor)
                    zFactor++;
                else
                {
                    xFactor++;
                    yFactor++;
                }
            }
        }

        public Vector2 GetRADecFromXY(double X, double Y)
        {
            double XPos, YPos;
            MariusSoft.WCSTools.WCSUtil.ffwldp(X, Y, _xRef, _yRef, _xRefPix, _yRefPix, _xDelt, _yDelt, _rot, _wcsProj, out XPos, out YPos);
            Vector2 raDec= new Vector2((float)XPos, (float)YPos);
            return raDec;
        }

        public double GetVelocityFromZ(double z)
        {
            return _zRef + _zDelt * (z - _zRefPix);
        }

        public Vector3 GetWCSDeltas()
        {
            return new Vector3((float)_xDelt, (float)_yDelt, (float)_zDelt);
        }

        public void ParseHeaderDict()
        {
            string xProj = "";
            string yProj = "";
            string zProj = "";
            _rot = 0;
            foreach (KeyValuePair<string, string> entry in _headerDictionary)
            {
                switch (entry.Key)
                {
                    case "NAXIS":
                        NAxis = Convert.ToDouble(entry.Value, CultureInfo.InvariantCulture);
                        break;
                    case "CTYPE1":
                        _xCoord = entry.Value.Substring(0, 4);
                        xProj = entry.Value.Substring(5, 4);
                        break;
                    case "CRPIX1":
                        _xRefPix = Convert.ToDouble(entry.Value, CultureInfo.InvariantCulture);
                        break;
                    case "CDELT1":
                        _xDelt = Convert.ToDouble(entry.Value, CultureInfo.InvariantCulture);
                        break;
                    case "CRVAL1":
                        _xRef = Convert.ToDouble(entry.Value, CultureInfo.InvariantCulture);
                        break;
                    case "CTYPE2":
                        _yCoord = entry.Value.Substring(0, 4);
                        yProj = entry.Value.Substring(5, 4);
                        break;
                    case "CRPIX2":
                        _yRefPix = Convert.ToDouble(entry.Value, CultureInfo.InvariantCulture);
                        break;
                    case "CDELT2":
                        _yDelt = Convert.ToDouble(entry.Value, CultureInfo.InvariantCulture);
                        break;
                    case "CRVAL2":
                        _yRef = Convert.ToDouble(entry.Value, CultureInfo.InvariantCulture);
                        break;
                    case "CTYPE3":
                        _zCoord = entry.Value.Substring(0, 4); //Crashing with some data sets. Need to fix
                        zProj = entry.Value.Substring(5, 4);
                        break;
                    case "CRPIX3":
                        _zRefPix = Convert.ToDouble(entry.Value, CultureInfo.InvariantCulture);
                        break;
                    case "CDELT3":
                        _zDelt = Convert.ToDouble(entry.Value, CultureInfo.InvariantCulture);
                        break;
                    case "CRVAL3":
                        _zRef = Convert.ToDouble(entry.Value, CultureInfo.InvariantCulture);
                        break;
                    case "CROTA2":
                        _rot = Convert.ToDouble(entry.Value.Replace("'", ""), CultureInfo.InvariantCulture);
                        break;
                    default:
                        break;
                }
            }
            if (xProj != yProj)
                Debug.Log("Warning: WCS projection types do not agree for dimensions! x: " + xProj + ", y: " + yProj);
            _wcsProj = xProj;
        }

        public void CleanUp()
        {
            FitsReader.FreeMemory(FitsData);
        }
    }
}