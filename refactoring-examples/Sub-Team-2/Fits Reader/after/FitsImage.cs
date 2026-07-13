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

public static class FitsImage
{
    public enum BitpixDataType
    {
        BYTE_IMG = 8,
        SHORT_IMG = 16,
        LONG_IMG = 32,
        LONGLONG_IMG = 64,
        FLOAT_IMG = -32,
        DOUBLE_IMG = -64
    }

    [DllImport("idavie_fits")] public static extern int FitsCreateImg(IntPtr fptr, int bitpix, int naxis, IntPtr naxes, out int status);
    [DllImport("idavie_fits")] public static extern int FitsCopyHeader(IntPtr infptr, IntPtr outfptr, out int status);
    [DllImport("idavie_fits")] public static extern int FitsCopyFile(IntPtr infptr, IntPtr outfptr, out int status);
    [DllImport("idavie_fits")] public static extern int FitsCopyImageSection(string inFile, string outFile, string section, string historyTimeStamp, int selectedHDU, out int status);
    [DllImport("idavie_fits")] public static extern int FitsCopyCubeSection(IntPtr infptr, IntPtr outfptr, string section, out int status);

    [DllImport("idavie_fits")]
    [Obsolete("Replaced by FitsWriteSubImageInt16, which is more flexible.")]
    public static extern int FitsWriteImageInt16(IntPtr fptr, int dims, long nelements, IntPtr array, out int status);

    [DllImport("idavie_fits")] public static extern int FitsWriteSubImageInt16(IntPtr fptr, IntPtr cornerMin, IntPtr cornerMax, IntPtr array, out int status);
    [DllImport("idavie_fits")] public static extern int FitsWriteNewCopySubImageInt16(string newFileName, IntPtr fptr, IntPtr cornerMin, IntPtr cornerMax, IntPtr array, string historyTimeStamp, out int status);

    [Obsolete("FitsReadImageFloat is deprecated, please use FitsReadSubImageFloat instead.")]
    [DllImport("idavie_fits")] public static extern int FitsReadImageFloat(IntPtr fptr, int dims, long nelem, out IntPtr array, out int status);

    [DllImport("idavie_fits")] public static extern int FitsReadSubImageFloat(IntPtr fptr, int dims, int zAxis, IntPtr startPix, IntPtr finalPix, long nelem, out IntPtr array, out int status);

    [Obsolete("FitsReadImageInt16 is deprecated, please use FitsReadSubImageInt16 instead.")]
    [DllImport("idavie_fits")] public static extern int FitsReadImageInt16(IntPtr fptr, int dims, long nelem, out IntPtr array, out int status);

    [DllImport("idavie_fits")] public static extern int FitsReadSubImageInt16(IntPtr fptr, int dims, int zAxis, IntPtr startPix, IntPtr finalPix, long nelem, out IntPtr array, out int status);
    [DllImport("idavie_fits")] public static extern int CreateEmptyImageInt16(long sizeX, long sizeY, long sizeZ, out IntPtr array);
    [DllImport("idavie_fits")] public static extern int FreeFitsPtrMemory(IntPtr pointerToDelete);
    [DllImport("idavie_fits")] public static extern int WriteMomentMap(IntPtr mainFitsFile, string fileName, IntPtr imagePixelArray, long xDims, long yDims, int mapNumber);
}
