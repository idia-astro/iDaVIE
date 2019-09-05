using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Text;



public class VOTableReader
{

    [DllImport("votable_reader")]
    public static extern int VOTableInitialize(out IntPtr vptr);

    [DllImport("votable_reader")]
    public static extern int VOTableOpenFile(IntPtr vptr, string filename, string xpath, out int status);

    [DllImport("votable_reader")]
    public static extern int VOTableGetMetaData(IntPtr vptr, out IntPtr meta_ptr, out int status);

    [DllImport("votable_reader")]
    public static extern int VOTableGetName(IntPtr vptr, out IntPtr name_ptr, out int status);

    [DllImport("votable_reader")]
    public static extern int MetaDataGetNumCols(IntPtr meta_ptr, out int ncols, out int status);

    [DllImport("votable_reader")]
    public static extern int MetaDataGetField(IntPtr meta_ptr, out IntPtr field_ptr, int fieldNum, out int status);

    [DllImport("votable_reader")]
    public static extern int FieldGetName(IntPtr field_ptr, out IntPtr name_ptr, out int status);

    [DllImport("votable_reader")]
    public static extern int FreeMemory(IntPtr vptr);

}