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
    public static extern int FitsGetNumRows(IntPtr fptr, out long nrows, out int status);

    [DllImport("fits_reader")]
    public static extern int FitsGetNumCols(IntPtr fptr, out int ncols, out int status);

    [DllImport("fits_reader")]
    public static extern int FitsMakeKeyN(string keyroot, int value, StringBuilder keyname, out int status);


    [DllImport("fits_reader")]
    public static extern int FitsReadKey(IntPtr fptr, int datatype, string keyname, StringBuilder colname, IntPtr comm, out int status);


    [DllImport("fits_reader")]
    public static extern int FitsReadCol(IntPtr fptr, int datatype, int colnum, long firstrow,
        long firstelem, long nelem,  ref IntPtr array,  out int status);
}
