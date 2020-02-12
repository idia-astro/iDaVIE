﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Collections;

public class FitsReader
{
    public static int FitsOpenFile(out IntPtr fptr, string filename, out int status, bool isReadOnly)
    {
        if (isReadOnly)
            return FitsOpenFileReadOnly(out fptr, filename, out status);
        else
            return FitsOpenFileReadWrite(out fptr, filename, out status);
    }

    [DllImport("fits_reader")]
    public static extern int FitsOpenFileReadOnly(out IntPtr fptr, string filename, out int status);

    [DllImport("fits_reader")]
    public static extern int FitsOpenFileReadWrite(out IntPtr fptr, string filename, out int status);

    [DllImport("fits_reader")]
    public static extern int FitsCreateFile(out IntPtr fptr, string filename, out int status);

    [DllImport("fits_reader")]
    public static extern int FitsCloseFile(IntPtr fptr, out int status);

    [DllImport("fits_reader")]
    public static extern int FitsFlushFile(IntPtr fptr, out int status);

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
    public static extern int InsertSubArrayInt16(IntPtr mainArray, long mainArraySize, IntPtr subArray, long subArraySize, long startIndex);

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

    public static void SaveNewInt16Mask(IntPtr cubeFitsPtr, IntPtr maskData, long[] maskDataDims, string fileName)
    {
        IntPtr maskPtr = IntPtr.Zero;
        IntPtr keyValue = Marshal.AllocHGlobal(sizeof(int));
        Marshal.WriteInt32(keyValue, 16);
        int status = 0;
        long nelements = maskDataDims[0] * maskDataDims[1] * maskDataDims[2];
        IntPtr naxes = Marshal.AllocHGlobal(3 * sizeof(long));
        Marshal.Copy(maskDataDims, 0, naxes, maskDataDims.Length);
        if (FitsCreateFile(out maskPtr, fileName, out status) != 0)
        {
            Debug.Log("Fits create file error #" + status.ToString());
            return;
        }
        if (FitsCopyHeader(cubeFitsPtr, maskPtr, out status) != 0)
        {
            Debug.Log("Fits copy header error #" + status.ToString());
            return;
        }
        if (FitsUpdateKey(maskPtr, 21, "BITPIX", keyValue, null, out status) != 0)
        {
            Debug.Log("Fits update key error #" + status.ToString());
            return;
        }
        if (FitsWriteImageInt16(maskPtr, 3, nelements, maskData, out status) != 0)
        {
            Debug.Log("Fits write image error #" + status.ToString());
            return;
        }
        if (FitsFlushFile(maskPtr, out status) != 0)
        {
            Debug.Log("Fits flush file error #" + status.ToString());
            return;
        }
        if (FitsCloseFile(maskPtr, out status) != 0)
        {
            Debug.Log("Fits close file error #" + status.ToString());
            return;
        }
    }

    public static void UpdateOldInt16Mask(IntPtr oldMaskPtr, IntPtr maskDataToSave, long[] maskDataDims)
    {
        int status;
        long nelements = maskDataDims[0] * maskDataDims[1] * maskDataDims[2];
        if (FitsWriteImageInt16(oldMaskPtr, 3, nelements, maskDataToSave, out status) != 0) ////Try deleting hdu
        {
            Debug.Log("Fits write image error #" + status.ToString());
            return;
        }
        if (FitsFlushFile(oldMaskPtr, out status) != 0)
        {
            Debug.Log("Fits flush file error #" + status.ToString());
            return;
        }
        if (FitsCloseFile(oldMaskPtr, out status) != 0)
        {
            Debug.Log("Fits close file error #" + status.ToString());
            return;
        }
    }

    public static void SaveMask(IntPtr fitsPtrHeaderToCopy, IntPtr oldMaskData, long[] oldMaskDims, IntPtr regionData, long[] regionDims, long[] regionStartPix, string fileName)
    {
        bool isNewFile = (fileName != null);
        int srcJump = (int)(regionDims[0] * sizeof(short));
        IntPtr srcPtr = regionData;
        for (var z = 0; z < regionDims[2]; z++)
        {
            for (var y = 0; y < regionDims[1]; y++)
            {
                long startIndex = (z + regionStartPix[2] - 1) * oldMaskDims[0] * oldMaskDims[1] + (y +  regionStartPix[1] - 1) * oldMaskDims[0] + (regionStartPix[0] - 1);
                if (InsertSubArrayInt16(oldMaskData, oldMaskDims[0]* oldMaskDims[1]* oldMaskDims[2], 
                        srcPtr, regionDims[0], startIndex) != 0)
                {
                    Debug.Log("Error inserting submask into mask data!");
                    return;
                }
                srcPtr = IntPtr.Add(srcPtr, srcJump);
            }
        }
        if (isNewFile)
        {
            SaveNewInt16Mask(fitsPtrHeaderToCopy, oldMaskData, oldMaskDims, fileName);
        }
        else
        {
            UpdateOldInt16Mask(fitsPtrHeaderToCopy, oldMaskData, oldMaskDims);
        }
    }
}