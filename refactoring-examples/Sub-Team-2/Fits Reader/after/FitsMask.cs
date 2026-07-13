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

public static class FitsMask
{
    [DllImport("idavie_fits")] public static extern int InsertSubArrayInt16(IntPtr mainArray, long mainArraySize, IntPtr subArray, long subArraySize, long startIndex);
    [DllImport("idavie_fits")] public static extern int WriteLogFile(char[] fileName, char[] content, int type);

  [Obsolete("SaveNewInt16Mask is deprecated, please use SaveNewInt16SubMask instead.")]
    public static int SaveNewInt16Mask(IntPtr cubeFitsPtr, IntPtr maskData, long[] maskDataDims, string fileName)
    {
        IntPtr maskPtr = IntPtr.Zero;
        IntPtr keyValue = Marshal.AllocHGlobal(sizeof(int));
        int status = 0;
        long nelements = maskDataDims[0] * maskDataDims[1] * maskDataDims[2];
        IntPtr naxes = Marshal.AllocHGlobal(3 * sizeof(long));
        Marshal.Copy(maskDataDims, 0, naxes, maskDataDims.Length);
        if (FitsCreateFile(out maskPtr, fileName, out status) != 0)
        {
            Console.Error.WriteLine($"Fits create file error {FitsHeader.FitsErrorMessage(status)}");
            Marshal.FreeHGlobal(naxes);
            Marshal.FreeHGlobal(keyValue);
            return status;
        }
        if (FitsImage.FitsCopyHeader(cubeFitsPtr, maskPtr, out status) != 0)
            {Console.Error.WriteLine($"Fits copy header error {FitsHeader.FitsErrorMessage(status)}");
            Marshal.FreeHGlobal(naxes);
            Marshal.FreeHGlobal(keyValue);
            return status;
        }
        Marshal.WriteInt32(keyValue, 16);
           if (FitsHeader.FitsUpdateKey(maskPtr, 21, "BITPIX", keyValue, null, out status) != 0)
        {
            Debug.LogError($"Fits update key error {FitsErrorMessage(status)}");
            Marshal.FreeHGlobal(naxes);
            Marshal.FreeHGlobal(keyValue);
            return status;
        }
        Marshal.WriteInt32(keyValue, 3);
        if (FitsHeader.FitsUpdateKey(maskPtr, 21, "NAXIS", keyValue, null, out status) != 0)
        {
            Console.Error.WriteLine($"Fits update key error {FitsHeader.FitsErrorMessage(status)}");
            Marshal.FreeHGlobal(naxes);
            Marshal.FreeHGlobal(keyValue);
            return status;
        }
        if (FitsHeader.FitsDeleteKey(maskPtr, "BUNIT", out status) != 0)
        {
            Console.Error.WriteLine("Could not delete fits unit key. It probably does not exist!");
            status = 0;
        }
          if (FitsImage.FitsWriteImageInt16(maskPtr, 3, nelements, maskData, out status) != 0)
        {
                 Console.Error.WriteLine("Fits write image error " + FitsHeader.FitsErrorMessage(status));
            Marshal.FreeHGlobal(naxes);
            Marshal.FreeHGlobal(keyValue);
            return status;
        }
        var historyTimeStamp = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
         if (FitsHeader.FitsWriteHistory(maskPtr, $"Edited by iDaVIE at {historyTimeStamp}", out status) != 0)
        {
            Console.Error.WriteLine("Error writing history!");
            Marshal.FreeHGlobal(naxes);
            Marshal.FreeHGlobal(keyValue);
            return status;
        }
            if (FitsFile.FitsFlushFile(maskPtr, out status) != 0))
        {
            Console.Error.WriteLine($"Fits flush file error {FitsHeader.FitsErrorMessage(status)}");
            Marshal.FreeHGlobal(naxes);
            Marshal.FreeHGlobal(keyValue);
            return status;
        }
        if (FitsFile.FitsCloseFile(maskPtr, out status) != 0)
        {
            Console.Error.WriteLine($"Fits close file error {FitsHeader.FitsErrorMessage(status)}");
            Marshal.FreeHGlobal(naxes);
            Marshal.FreeHGlobal(keyValue);
            return status;
        }
        if (keyValue != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(keyValue);
            keyValue = IntPtr.Zero;
        }
        Marshal.FreeHGlobal(naxes);
        return status;
    }

    [Obsolete("UpdateOldInt16Mask is deprecated, please use UpdateOldInt16SubMask instead.")]
    public static int UpdateOldInt16Mask(IntPtr oldMaskPtr, IntPtr maskDataToSave, long[] maskDataDims)
    {
        int status = 0;
        IntPtr keyValue = Marshal.AllocHGlobal(sizeof(int));
        long nelements = maskDataDims[0] * maskDataDims[1] * maskDataDims[2];
        Marshal.WriteInt32(keyValue, 3);
        if (FitsHeader.FitsUpdateKey(oldMaskPtr, 21, "NAXIS", keyValue, null, out status) != 0)
        {
            Console.Error.WriteLine($"Fits update key error {FitsHeader.FitsErrorMessage(status)}");
            return status;
        }
        if (FitsImage.FitsWriteImageInt16(oldMaskPtr, 3, nelements, maskDataToSave, out status) != 0)
        {
            Console.Error.WriteLine($"Fits write image error {FitsHeader.FitsErrorMessage(status)}");
            return status;
        }
        var historyTimeStamp = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
        if (FitsHeader.FitsWriteHistory(oldMaskPtr, $"Edited by iDaVIE at {historyTimeStamp}", out status) != 0)
        {
            Console.Error.WriteLine("Error writing history!");
            return status;
        }
        if (FitsFile.FitsFlushFile(oldMaskPtr, out status) != 0)
        {
            Console.Error.WriteLine($"Fits flush file error {FitsHeader.FitsErrorMessage(status)}");
            return status;
        }
        if (keyValue != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(keyValue);
        }
        return status;
    }

     public static int SaveNewInt16SubMask(IntPtr cubeFitsPtr, IntPtr maskData, IntPtr firstPix, IntPtr lastPix, string fileName)
    {
        IntPtr maskPtr = IntPtr.Zero;
        IntPtr keyValue = Marshal.AllocHGlobal(sizeof(int));
        int status = 0;
        if (FitsCreateFile(out maskPtr, fileName, out status) != 0)
        {
            Debug.LogError($"Fits create file error {FitsErrorMessage(status)}");
            Marshal.FreeHGlobal(keyValue);
            return status;
        }
        if (FitsCopyHeader(cubeFitsPtr, maskPtr, out status) != 0)
        {
            Debug.LogError($"Fits copy file error {FitsErrorMessage(status)}");
            Marshal.FreeHGlobal(keyValue);
            return status;
        }
        Marshal.WriteInt32(keyValue, 16);
        if (FitsUpdateKey(maskPtr, 21, "BITPIX", keyValue, null, out status) != 0)
        {
            Debug.LogError($"Fits update key error {FitsErrorMessage(status)}");
            Marshal.FreeHGlobal(keyValue);
            return status;
        }
        Marshal.WriteInt32(keyValue, 3);
        if (FitsUpdateKey(maskPtr, 21, "NAXIS", keyValue, null, out status) != 0)   //Make sure new header has 3 dimensions
        {
            Debug.LogError($"Fits update key error {FitsErrorMessage(status)}");
            Marshal.FreeHGlobal(keyValue);
            return status;
        }
        if (FitsDeleteKey(maskPtr, "BUNIT", out status) != 0)
        {
            Debug.LogWarning("Could not delete fits unit key. It probably does not exist!");
            status = 0;
        }
        if (FitsWriteSubImageInt16(maskPtr, firstPix, lastPix, maskData, out status) != 0)
        {
            Debug.LogError("Fits write subset error " + FitsErrorMessage(status));
            FitsCloseFile(maskPtr, out status);
            Marshal.FreeHGlobal(keyValue);
            return status;
        }
        var historyTimeStamp = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
        if (FitsWriteHistory(maskPtr, $"Edited by iDaVIE at {historyTimeStamp}", out status) != 0)
        {
            Debug.LogError("Error writing history!");
            Marshal.FreeHGlobal(keyValue);
            return status;
        }
        if (FitsFlushFile(maskPtr, out status) != 0)
        {
            Debug.LogError($"Fits flush file error {FitsErrorMessage(status)}");
            Marshal.FreeHGlobal(keyValue);
            return status;
        }
        if (FitsCloseFile(maskPtr, out status) != 0)
        {
            Debug.LogError($"Fits close file error {FitsErrorMessage(status)}");
            Marshal.FreeHGlobal(keyValue);
            return status;
        }
        if (keyValue != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(keyValue);
            keyValue = IntPtr.Zero;
        }
        return status;
    }

    public static int UpdateOldInt16SubMask(IntPtr oldMaskPtr, IntPtr maskDataToSave, IntPtr firstPix, IntPtr lastPix)
    {
        Console.WriteLine("Overwriting old mask");
        int status = 0;
        IntPtr keyValue = Marshal.AllocHGlobal(sizeof(int));
        Marshal.WriteInt32(keyValue, 3);
        if (FitsHeader.FitsUpdateKey(oldMaskPtr, 21, "NAXIS", keyValue, null, out status) != 0)
        {
            Console.Error.WriteLine($"Fits update key error {FitsHeader.FitsErrorMessage(status)}");
            return status;
        }
        Console.WriteLine("Keys updated, writing data.");
        if (FitsImage.FitsWriteSubImageInt16(oldMaskPtr, firstPix, lastPix, maskDataToSave, out status) != 0)
        {
            Console.Error.WriteLine($"Fits write image error {FitsHeader.FitsErrorMessage(status)}");
            FitsFile.FitsCloseFile(oldMaskPtr, out status);
            return status;
        }
        Console.WriteLine("Writing data complete, writing history.");
        var historyTimeStamp = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
        if (FitsHeader.FitsWriteHistory(oldMaskPtr, $"Edited by iDaVIE at {historyTimeStamp}", out status) != 0)
        {
            Console.Error.WriteLine("Error writing history!");
            return status;
        }
        Console.WriteLine("Writing history complete, flushing buffer.");
        if (FitsFile.FitsFlushFile(oldMaskPtr, out status) != 0)
        {
            Console.Error.WriteLine($"Fits flush file error {FitsHeader.FitsErrorMessage(status)}");
            return status;
        }
        if (keyValue != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(keyValue);
        }
        return status;
    }

    public static bool UpdateMaskVoxel(IntPtr maskDataPtr, long[] maskDims, int x, int y, int z, short value)
    {
        // Converts from 1-based FITS coordinates to 0-based array index
        long index = (x - 1) + (y - 1) * maskDims[0] + (z - 1) * (maskDims[0] * maskDims[1]);
        Marshal.WriteInt16(new IntPtr(maskDataPtr.ToInt64() + index * sizeof(short)), value);
        return true;
    }

    [Obsolete("SaveMask is obsolete, please use SaveSubMask instead.")]
    public static int SaveMask(IntPtr fitsPtr, IntPtr maskData, long[] maskDims, string fileName)
    {
        if (fileName != null)
            return SaveNewInt16Mask(fitsPtr, maskData, maskDims, fileName);
        else
            return UpdateOldInt16Mask(fitsPtr, maskData, maskDims);
    }

    public static int SaveSubMask(IntPtr fitsPtr, IntPtr maskData, int[] firstPix, int[] lastPix, string fileName, bool exporting)
    {
        bool isNewFile = fileName != null;
        IntPtr fPix = Marshal.AllocHGlobal(sizeof(int) * firstPix.Length);
        IntPtr lPix = Marshal.AllocHGlobal(sizeof(int) * lastPix.Length);
        Marshal.Copy(firstPix, 0, fPix, firstPix.Length);
        Marshal.Copy(lastPix, 0, lPix, lastPix.Length);
        Console.WriteLine("Writing submask from first pixel [" + String.Join(", ", firstPix) + "] and end pixel [" + String.Join(", ", lastPix) + "].");
        if (isNewFile)
        {
            if (exporting)
            {
                Console.WriteLine("Attempting to export mask to a new file " + fileName + ".");
                int status = 0;
                var historyTimeStamp = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
                FitsImage.FitsWriteNewCopySubImageInt16(fileName, fitsPtr, fPix, lPix, maskData, historyTimeStamp, out status);
                if (status != 0)
                    Console.Error.WriteLine($"Fits save new copy error {FitsHeader.FitsErrorMessage(status)}, see plugin log for details.");
                return status;
            }
            else
            {
                Console.WriteLine("Saving mask file " + fileName + " for the first time.");
                return SaveNewInt16SubMask(fitsPtr, maskData, fPix, lPix, fileName);
            }
        }
        else
        {
            Console.WriteLine("Overwriting existing mask file " + fileName + ".");
            return UpdateOldInt16SubMask(fitsPtr, maskData, fPix, lPix);
        }
    }
}
