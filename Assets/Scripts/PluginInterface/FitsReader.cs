using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Collections;

public class FitsReader
{

    [DllImport("fits_reader")]
    public static extern int FitsOpenFileReadOnly(out IntPtr fptr, string filename, out int status);

    [DllImport("fits_reader")]
    public static extern int FitsOpenFileReadWrite(out IntPtr fptr, string filename, out int status);

    [DllImport("fits_reader")]
    public static extern int FitsCreateFile(out IntPtr fptr, string filename, out int status);

    [DllImport("fits_reader")]
    public static extern int FitsCloseFile(IntPtr fptr, out int status);

    [DllImport("fits_reader")]
    public static extern int FitsMovabsHdu(IntPtr fptr, int hdunum, out int hdutype, out int status);

    [DllImport("fits_reader")]
    public static extern int FitsGetNumHeaderKeys(IntPtr fptr, out int keysexist, out int morekeys, out int status);

    [DllImport("fits_reader")]
    public static extern int FitsGetNumRows(IntPtr fptr, out long nrows, out int status);

    [DllImport("fits_reader")]
    public static extern int FitsGetNumCols(IntPtr fptr, out int ncols, out int status);

    [DllImport("fits_reader")]
    public static extern int FitsGetImageDims(IntPtr fptr, out int dims, out int status);

    [DllImport("fits_reader")]
    public static extern int FitsGetImageSize(IntPtr fptr, int dims, out IntPtr naxes, out int status);

    [DllImport("fits_reader")]
    public static extern int FitsCreateImg(IntPtr fptr, int bitpix, int naxis, IntPtr naxes, out int status);

    [DllImport("fits_reader")]
    public static extern int FitsCopyHeader(IntPtr infptr, IntPtr outfptr, out int status);

    [DllImport("fits_reader")]
    public static extern int FitsWriteImageInt16(IntPtr fptr, int dims, long nelements, IntPtr array, out int status);

    [DllImport("fits_reader")]
    public static extern int FitsWriteKey(IntPtr fptr, int datatype, string keyname, IntPtr value, string comment, out int status);

    [DllImport("fits_reader")]
    public static extern int FitsUpdateKey(IntPtr fptr, int datatype, string keyname, IntPtr value, string comment, out int status);

    [DllImport("fits_reader")]
    public static extern int FitsMakeKeyN(string keyroot, int value, StringBuilder keyname, out int status);

    [DllImport("fits_reader")]
    public static extern int FitsReadKey(IntPtr fptr, int datatype, string keyname, StringBuilder colname, IntPtr comm, out int status);

    [DllImport("fits_reader")]
    public static extern int FitsReadKeyN(IntPtr fptr, int keynum, StringBuilder keyname, StringBuilder keyvalue, StringBuilder comment, out int status);

    [DllImport("fits_reader")]
    public static extern int FitsReadColFloat(IntPtr fptr, int colnum, long firstrow, long firstelem, long nelem, out IntPtr array, out int status);

    [DllImport("fits_reader")]
    public static extern int FitsReadColString(IntPtr fptr, int colnum, long firstrow, long firstelem, long nelem, out IntPtr ptrarray, out IntPtr chararray, out int status);

    [DllImport("fits_reader")]
    public static extern int FitsReadImageFloat(IntPtr fptr, int dims, long nelem, out IntPtr array, out int status);

    [DllImport("fits_reader")]
    public static extern int FitsReadImageInt16(IntPtr fptr, int dims, long nelem, out IntPtr array, out int status);

    [DllImport("fits_reader")]
    public static extern int CreateEmptyImageInt16(long sizeX, long sizeY, long sizeZ, out IntPtr array);

    [DllImport("fits_reader")]
    public static extern int FreeMemory(IntPtr pointerToDelete);

    [DllImport("fits_reader")]
    public static extern int InsertSubFloatArray(IntPtr mainArray, long mainArraySize, IntPtr subArray, long subArraySize, long startIndex, IntPtr resultArray);

    public static IDictionary<string, string> ExtractHeaders(IntPtr fptr, out int status)
    {
        int numberKeys, keysLeft;
        if (FitsGetNumHeaderKeys(fptr, out numberKeys, out keysLeft, out status) != 0)
        {
            Debug.Log("Fits extract header error #" + status.ToString());
            return null;
        }
        IDictionary<string, string> dict = new Dictionary<string, string>();
        for (int i = 1; i <= numberKeys; i++)
        {
            StringBuilder keyName = new StringBuilder(70);
            StringBuilder keyValue = new StringBuilder(70);
            FitsReadKeyN(fptr, i, keyName, keyValue, null, out status);
            string key = keyName.ToString();
            if (!dict.ContainsKey(key))
                dict.Add(key, keyValue.ToString());
            else
                dict[key] = dict[key] + keyValue.ToString();
            keyName.Clear();
            keyValue.Clear();
        }
        return dict;
    }

    public static void SaveNewInt16Mask(IntPtr cubeFitsPtr, IntPtr maskData, int[] maskDataDims, string fileName)
    {
        IntPtr maskPtr = IntPtr.Zero;
        IntPtr keyValue = (IntPtr)21;
        int status;
        long nelements = maskDataDims[0] * maskDataDims[1] * maskDataDims[2];
        IntPtr naxes = IntPtr.Zero;
        Marshal.Copy(maskDataDims, 0, naxes, maskDataDims.Length);
        if (FitsCreateFile(out maskPtr, fileName, out status) != 0)
        {
            Debug.Log("Fits create file error #" + status.ToString());
        }
        if (FitsCopyHeader(cubeFitsPtr, maskPtr, out status) != 0)
        {
            Debug.Log("Fits copy header error #" + status.ToString());
        }
        if (FitsUpdateKey(maskPtr, 14, "BITPIX", keyValue, null, out status) != 0)
        {
            Debug.Log("Fits update key error #" + status.ToString());
        }
        if (FitsCreateImg(maskPtr, 21, 3, naxes, out status) != 0)
        {
            Debug.Log("Fits create image error #" + status.ToString());
        }
        if (FitsWriteImageInt16(maskPtr, 3, nelements, maskData, out status) != 0)
        {
            Debug.Log("Fits write image error #" + status.ToString());
        }
        if (FitsCloseFile(maskPtr, out status) != 0)
        {
            Debug.Log("Fits close file error #" + status.ToString());

        }
    }

    public static void UpdateOldInt16Mask(IntPtr oldMaskPtr, IntPtr maskDataToSave, int[] maskDataDims)
    {
        int status;
        long nelements = maskDataDims[0] * maskDataDims[1] * maskDataDims[2];
        IntPtr naxes = IntPtr.Zero;
        Marshal.Copy(maskDataDims, 0, naxes, maskDataDims.Length);
        if (FitsWriteImageInt16(oldMaskPtr, 3, nelements, maskDataToSave, out status) != 0)
        {
            Debug.Log("Fits write image error #" + status.ToString());
        }
        if (FitsCloseFile(oldMaskPtr, out status) != 0)
        {
            Debug.Log("Fits close file error #" + status.ToString());

        }
    }

    unsafe public static void SaveMask(IntPtr fitsPtr, IntPtr oldMaskData, int[] oldMaskDims, short[] regionData, long[] regionDims, long[] regionStartPix, string fileName)
    {
        bool isNewFile = (fileName != null);
        IntPtr maskDataToSave = IntPtr.Zero;
        IntPtr regionDataPtr = IntPtr.Zero;
        using (var regionDataNArray = new NativeArray<short>(regionData, Allocator.TempJob))
            {
                regionDataPtr = (IntPtr)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(regionDataNArray);
            }
        long startIndex = regionStartPix[2] * oldMaskDims[0] * oldMaskDims[1] + regionStartPix[1] * oldMaskDims[0] + regionStartPix[0];
        InsertSubFloatArray(oldMaskData, oldMaskDims[0] * oldMaskDims[1] * oldMaskDims[2], regionDataPtr, regionDims[0] * regionDims[1] * regionDims[2], startIndex, maskDataToSave);
        if (isNewFile)
        {
            SaveNewInt16Mask(fitsPtr, maskDataToSave, oldMaskDims, fileName);
        }
        else
        {
            UpdateOldInt16Mask(fitsPtr, maskDataToSave, oldMaskDims);
        }
    }
}