using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Text;


public class FitsReader {

    [DllImport("fits_reader")]
    public static extern int FitsOpenFile(out IntPtr fptr, string filename, out int status);

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
    public static extern int FitsMakeKeyN(string keyroot, int value, StringBuilder keyname, out int status);

    [DllImport("fits_reader")]
    public static extern int FitsReadKey(IntPtr fptr, int datatype, string keyname, StringBuilder colname, IntPtr comm, out int status);

    [DllImport("fits_reader")]
    public static extern int FitsReadKeyN(IntPtr fptr, int keynum, StringBuilder keyname, StringBuilder keyvalue, StringBuilder comment, out int status);

    [DllImport("fits_reader")]
    public static extern int FitsReadColFloat(IntPtr fptr, int colnum, long firstrow, long firstelem, long nelem,  out IntPtr array,  out int status);

    [DllImport("fits_reader")]
    public static extern int FitsReadColString(IntPtr fptr, int colnum, long firstrow, long firstelem, long nelem, out IntPtr ptrarray, out IntPtr chararray, out int status);

    [DllImport("fits_reader")]
    public static extern int FitsReadImageFloat(IntPtr fptr, int dims, long nelem, out IntPtr array, out int status);

    [DllImport("fits_reader")]
    public static extern int FitsReadImageInt16(IntPtr fptr, int dims, long nelem, out IntPtr array, out int status);

    [DllImport("fits_reader")]
    public static extern int FreeMemory(IntPtr pointerToDelete);

    public static IDictionary<string,string> ExtractHeaders(IntPtr fptr, out int status)
    {
        int numberKeys, keysLeft;
        StringBuilder keyName = new StringBuilder(70);
        StringBuilder keyValue = new StringBuilder(70);
        if (FitsGetNumHeaderKeys(fptr, out numberKeys, out keysLeft, out status) != 0)
        {
            Debug.Log("Fits extract header error #" + status.ToString());
            return null;
        }
        IDictionary<string, string> dict = new Dictionary<string, string>();
        for (int i = 1; i <= numberKeys; i++)
        {
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
}
