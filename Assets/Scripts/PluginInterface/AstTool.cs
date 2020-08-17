using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using System.Text;

public class AstTool
{
      [DllImport("ast_tool")]
      public static extern int InitFrame(out IntPtr astFramePtr, in string fitsHeader);

      [DllImport("ast_tool")]
      public static extern int Format(IntPtr wcsinfo, int axis, double value, StringBuilder formattedVal, int formattedValLength);

      [DllImport("ast_tool")]
      public static extern int Set(IntPtr wcsinfo, in string attrib);

      [DllImport("ast_tool")]
      public static extern int Clear(IntPtr obj, in string attrib);

      [DllImport("ast_tool")]
      public static extern int GetString(IntPtr wcsinfo, in string attribute, StringBuilder stringToReturn, int stringToReturnLen);

      [DllImport("ast_tool")]
      public static extern int Norm(IntPtr wcsinfo, double[] inout);

      [DllImport("ast_tool")]
      public static extern int Transform(IntPtr wcsinfo, int npoint, in IntPtr xint, in IntPtr yint, int forward, IntPtr xout, IntPtr yout);

      [DllImport("ast_tool")]
      public static extern int Transform3D(IntPtr wcsinfo, double x, double y, double z, in int forward, IntPtr output);

      [DllImport("ast_tool")]
      public static extern void DeleteObject(IntPtr src);

      [DllImport("ast_tool")]
      public static extern int Copy(IntPtr src, out IntPtr copy);

      [DllImport("ast_tool")]
      public static extern void Invert(IntPtr src);

      [DllImport("ast_tool")]
      public static extern void FreeMemory(IntPtr ptrToDelete);

}