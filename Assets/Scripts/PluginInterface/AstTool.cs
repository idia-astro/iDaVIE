/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 *
 * This file is part of the iDaVIE project.
 *
 * iDaVIE is free software: you can redistribute it and/or modify it under the terms 
 * of the GNU Lesser General Public License (LGPL) as published by the Free Software 
 * Foundation, either version 3 of the License, or (at your option) any later version.
 *
 * iDaVIE is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR 
 * PURPOSE. See the GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License along with 
 * iDaVIE in the LICENSE file. If not, see <https://www.gnu.org/licenses/>.
 *
 * Additional information and disclaimers regarding liability and third-party 
 * components can be found in the DISCLAIMER and NOTICE files included with this project.
 *
 */
using System;
using System.Runtime.InteropServices;
using System.Text;

public class AstTool
{
      [DllImport("idavie_native")]
      public static extern int InitAstFrameSet(out IntPtr astFramePtr, IntPtr fitsHeader, double restFreq = 0);

      [DllImport("idavie_native")]
      public static extern int GetAstFrame(IntPtr astFramePtr, out IntPtr astFrame, int index);

      [DllImport("idavie_native")]
      public static extern int GetAltSpecSet(IntPtr frameSetPtr, out IntPtr specFrameSet, StringBuilder specSysTo, StringBuilder specUnitTo, StringBuilder specRestTo);
      
      [DllImport("idavie_native")]
      public static extern int Show(IntPtr astObject);

      [DllImport("idavie_native")]
      public static extern int Format(IntPtr wcsinfo, int axis, double value, StringBuilder formattedVal, int formattedValLength);

      [DllImport("idavie_native")]
      public static extern int Set(IntPtr wcsinfo, in string attrib);

      [DllImport("idavie_native")]
      public static extern int Clear(IntPtr obj, in string attrib);

      [DllImport("idavie_native")]
      public static extern void Dump(IntPtr wcsinfo, StringBuilder stringToReturn);

      [DllImport("idavie_native")]
      public static extern int GetString(IntPtr wcsinfo, StringBuilder attribute, StringBuilder stringToReturn, int stringToReturnLen);

      [DllImport("idavie_native")]
      public static extern int SetString(IntPtr wcsinfo, StringBuilder attribute, StringBuilder stringValue);

      [DllImport("idavie_native")]
      public static extern bool HasAttribute(IntPtr wcsinfo, StringBuilder attribute);

      [DllImport("idavie_native")]
      public static extern int Norm(IntPtr frameSetPtr, double xIn, double yIn, double zIn, out double xOut, out double yOut, out double zOut);

      [DllImport("idavie_native")]
      public static extern int Distance1D(IntPtr astFrame, double start, double end, int axis, out double distance);
      
      [DllImport("idavie_native")]
      public static extern int Distance2D(IntPtr astFrame, double startX, double startY, double endX, double endY, out double distance);

      [DllImport("idavie_native")]
      public static extern int Transform(IntPtr wcsinfo, int npoint, in IntPtr xint, in IntPtr yint, int forward, IntPtr xout, IntPtr yout);

      [DllImport("idavie_native")]
      public static extern int Transform3D(IntPtr wcsinfo, double xin, double yin, double zin, in int forward, out double xout, out double yout, out double zout);

      [DllImport("idavie_native")]
      public static extern int SpectralTransform(IntPtr astSpecFrame, StringBuilder specSysTo, StringBuilder specUnitTo, StringBuilder specRestTo, double zIn, int forward, out double zOut, StringBuilder formatZOut, int formatLength);

      [DllImport("idavie_native")]
      public static extern void DeleteObject(IntPtr src);

      [DllImport("idavie_native")]
      public static extern int Copy(IntPtr src, out IntPtr copy);

      [DllImport("idavie_native")]
      public static extern int Invert(IntPtr src);

      [DllImport("idavie_native")]
      public static extern void AstEnd();

      [DllImport("idavie_native")]
      public static extern void FreeAstMemory(IntPtr ptrToDelete);

}