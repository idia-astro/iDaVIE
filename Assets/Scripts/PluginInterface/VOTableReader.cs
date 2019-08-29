using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;


public class VOTableReader
{

    [DllImport("votable_reader")]
    public static extern int VOTableInitialize(out IntPtr vptr);

    [DllImport("votable_reader")]
    public static extern int VOTableOpenFile(IntPtr vptr, string filename, string xpath, out int status);

    [DllImport("votable_reader")]
    public static extern int FreeMemory(IntPtr vptr);

}