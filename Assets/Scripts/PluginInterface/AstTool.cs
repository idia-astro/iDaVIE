using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using System.Text;

public class AstTool
{
      [DllImport("ast_tool")]
      public static extern int InitAstFrameSet(out IntPtr astFramePtr, IntPtr fitsHeader, double restFreq = 0);

      [DllImport("ast_tool")]
      public static extern int GetAstFrame(IntPtr astFramePtr, out IntPtr astFrame, int index);

      [DllImport("ast_tool")]
      public static extern int GetAltSpecSet(IntPtr frameSetPtr, out IntPtr specFrameSet, StringBuilder specSysTo, StringBuilder specUnitTo, StringBuilder specRestTo);
      
      [DllImport("ast_tool")]
      public static extern int Show(IntPtr astObject);

      [DllImport("ast_tool")]
      public static extern int Format(IntPtr wcsinfo, int axis, double value, StringBuilder formattedVal, int formattedValLength);

      [DllImport("ast_tool")]
      public static extern int Set(IntPtr wcsinfo, in string attrib);

      [DllImport("ast_tool")]
      public static extern int Clear(IntPtr obj, in string attrib);

      [DllImport("ast_tool")]
      public static extern void Dump(IntPtr wcsinfo, StringBuilder stringToReturn);

      [DllImport("ast_tool")]
      public static extern int GetString(IntPtr wcsinfo, StringBuilder attribute, StringBuilder stringToReturn, int stringToReturnLen);

      [DllImport("ast_tool")]
      public static extern int SetString(IntPtr wcsinfo, StringBuilder attribute, StringBuilder stringValue);

      [DllImport("ast_tool")]
      public static extern bool HasAttribute(IntPtr wcsinfo, StringBuilder attribute);

      [DllImport("ast_tool")]
      public static extern int Norm(IntPtr frameSetPtr, double xIn, double yIn, double zIn, out double xOut, out double yOut, out double zOut);

      [DllImport("ast_tool")]
      public static extern int Distance1D(IntPtr astFrame, double start, double end, int axis, out double distance);
      
      [DllImport("ast_tool")]
      public static extern int Distance2D(IntPtr astFrame, double startX, double startY, double endX, double endY, out double distance);

      [DllImport("ast_tool")]
      public static extern int Transform(IntPtr wcsinfo, int npoint, in IntPtr xint, in IntPtr yint, int forward, IntPtr xout, IntPtr yout);

      [DllImport("ast_tool")]
      public static extern int Transform3D(IntPtr wcsinfo, double xin, double yin, double zin, in int forward, out double xout, out double yout, out double zout);

      [DllImport("ast_tool")]
      public static extern int SpectralTransform(IntPtr astSpecFrame, StringBuilder specSysTo, StringBuilder specUnitTo, StringBuilder specRestTo, double zIn, int forward, out double zOut, StringBuilder formatZOut, int formatLength);

      [DllImport("ast_tool")]
      public static extern void DeleteObject(IntPtr src);

      [DllImport("ast_tool")]
      public static extern int Copy(IntPtr src, out IntPtr copy);

      [DllImport("ast_tool")]
      public static extern int Invert(IntPtr src);

      [DllImport("ast_tool")]
      public static extern void AstEnd();

      [DllImport("ast_tool")]
      public static extern void FreeMemory(IntPtr ptrToDelete);

}