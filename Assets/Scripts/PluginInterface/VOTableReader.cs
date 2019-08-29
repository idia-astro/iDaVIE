using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Text;


public class VOTableReader
{

    [DllImport("votable_reader")]
    public static extern int FitsOpenFile(out IntPtr vptr, string filename, string xpath, out int status);

}