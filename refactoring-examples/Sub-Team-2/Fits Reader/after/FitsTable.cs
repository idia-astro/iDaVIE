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

public static class FitsTable
{
    [DllImport("idavie_fits")] public static extern int FitsReadColFloat(IntPtr fptr, int colnum, long firstrow, long firstelem, long nelem, out IntPtr array, out int status);
    [DllImport("idavie_fits")] public static extern int FitsReadColString(IntPtr fptr, int colnum, long firstrow, long firstelem, long nelem, out IntPtr ptrarray, out IntPtr chararray, out int status);

    public static string FitsTableGetColName(IntPtr fitsPtr, int col)
    {
        int status = 0;
        StringBuilder keyword = new StringBuilder(75);
        StringBuilder colName = new StringBuilder(71);
        FitsHeader.FitsMakeKeyN("TTYPE", col + 1, keyword, out status);
        if (FitsHeader.FitsReadKeyString(fitsPtr, keyword.ToString(), colName, IntPtr.Zero, out status) != 0)
        {
            Console.Error.WriteLine("Fits Read column name error #" + status.ToString());
            FitsFile.FitsCloseFile(fitsPtr, out status);
            return "";
        }
        return colName.ToString();
    }

    public static string FitsTableGetColUnit(IntPtr fitsPtr, int col)
    {
        int status = 0;
        StringBuilder keyword = new StringBuilder(75);
        StringBuilder colUnit = new StringBuilder(71);
        FitsHeader.FitsMakeKeyN("TUNIT", col + 1, keyword, out status);
        if (FitsHeader.FitsReadKeyString(fitsPtr, keyword.ToString(), colUnit, IntPtr.Zero, out status) != 0)
        {
            if (status == 202)
            {
                Console.WriteLine("No unit in column #" + col);
                status = 0;
            }
            else
            {
                Console.Error.WriteLine("Fits Read unit error #" + status.ToString());
                FitsFile.FitsCloseFile(fitsPtr, out status);
                return null;
            }
        }
        return colUnit.ToString();
    }

    public static string FitsTableGetColFormat(IntPtr fitsPtr, int col)
    {
        int status = 0;
        StringBuilder keyword = new StringBuilder(75);
        StringBuilder colFormat = new StringBuilder(71);
        FitsHeader.FitsMakeKeyN("TFORM", col + 1, keyword, out status);
        if (FitsHeader.FitsReadKeyString(fitsPtr, keyword.ToString(), colFormat, IntPtr.Zero, out status) != 0)
        {
            Console.Error.WriteLine("Fits Read column format error #" + status.ToString());
            FitsFile.FitsCloseFile(fitsPtr, out status);
            return "";
        }
        return colFormat.ToString();
    }
}
