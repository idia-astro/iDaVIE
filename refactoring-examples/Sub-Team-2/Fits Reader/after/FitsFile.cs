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

public static class FitsFile
{
    public enum HduType
    {
        ImageHdu = 0,
        AsciiTbl = 1,
        BinaryTbl = 2,
        AnyHdu = -1
    }

    public static int FitsOpenFile(out IntPtr fptr, string filename, out int status, bool isReadOnly)
    {
        if (isReadOnly)
            return FitsOpenFileReadOnly(out fptr, filename, out status);
        else
            return FitsOpenFileReadWrite(out fptr, filename, out status);
    }

    [DllImport("idavie_fits")] public static extern int FitsOpenFileReadOnly(out IntPtr fptr, string filename, out int status);
    [DllImport("idavie_fits")] public static extern int FitsOpenFileReadWrite(out IntPtr fptr, string filename, out int status);
    [DllImport("idavie_fits")] public static extern int FitsCreateFile(out IntPtr fptr, string filename, out int status);
    [DllImport("idavie_fits")] public static extern int FitsCloseFile(IntPtr fptr, out int status);
    [DllImport("idavie_fits")] public static extern int FitsFlushFile(IntPtr fptr, out int status);
    [DllImport("idavie_fits")] public static extern int FitsGetHduCount(IntPtr fptr, out int hdunum, out int status);
    [DllImport("idavie_fits")] public static extern int FitsGetHduType(IntPtr fptr, out int hdutype, out int status);
    [DllImport("idavie_fits")] public static extern int FitsGetCurrentHdu(IntPtr fptr, out int hdunum);
    [DllImport("idavie_fits")] public static extern int FitsMoveToHdu(IntPtr fptr, int hdunum, out int status);
    [DllImport("idavie_fits")] public static extern int FitsMovabsHdu(IntPtr fptr, int hdunum, out int hdutype, out int status);
    [DllImport("idavie_fits")] public static extern int FitsGetNumHdus(IntPtr fptr, out int numhdus, out int status);
    [DllImport("idavie_fits")] public static extern int FitsGetNumRows(IntPtr fptr, out long nrows, out int status);
    [DllImport("idavie_fits")] public static extern int FitsGetNumCols(IntPtr fptr, out int ncols, out int status);
    [DllImport("idavie_fits")] public static extern int FitsGetImageDims(IntPtr fptr, out int dims, out int status);
    [DllImport("idavie_fits")] public static extern int FitsGetImageSize(IntPtr fptr, int dims, out IntPtr naxes, out int status);
}
