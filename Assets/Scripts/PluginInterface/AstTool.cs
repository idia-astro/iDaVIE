using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;

public class AstTool
{
      [DllImport("ast_tool")]
      public static extern int InitFrame(out IntPtr astFramePtr, in string fitsHeader);


}