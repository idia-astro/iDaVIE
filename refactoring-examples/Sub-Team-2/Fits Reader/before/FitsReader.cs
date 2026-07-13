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
 */
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

public class FitsReader
{
    // HDU types
    public enum HduType
    {
        ImageHdu = 0,
        AsciiTbl = 1,
        BinaryTbl = 2,
        AnyHdu = -1
    }

    // Bitpix data types
    public enum BitpixDataType
    {
        BYTE_IMG = 8,
        SHORT_IMG = 16,
        LONG_IMG = 32,
        LONGLONG_IMG = 64,
        FLOAT_IMG = -32,
        DOUBLE_IMG = -64
    }

    // Header data types
    public enum HeaderDataType
    {
        TBIT = 1,
        TBYTE = 11,
        TLOGICAL = 14,
        TSTRING = 16,
        TSHORT = 21,
        TLONG = 41,
        TLONGLONG = 81,
        TFLOAT = 42,
        TDOUBLE = 82,
        TCOMPLEX = 83,
        TDBLCOMPLEX = 163,
        TINT = 31,
        TSBYTE = 12,
        TUINT = 30,
        TUSHORT = 20,
        TULONG = 40,
        TULONGLONG = 80
    }

    public static readonly Dictionary<int, string> ErrorCodes = new()
    {
        { 101, "input and output files are the same" },
        { 103, "tried to open too many FITS files at once" },
        { 104, "could not open the named file" },
        { 105, "could not create the named file" },
        { 106, "error writing to FITS file" },
        { 107, "tried to move past end of file" },
        { 108, "error reading from FITS file" },
        { 110, "could not close the file" },
        { 111, "array dimensions exceed internal limit" },
        { 112, "Cannot write to readonly file" },
        { 113, "Could not allocate memory" },
        { 114, "invalid fitsfile pointer" },
        { 115, "NULL input pointer to routine" },
        { 116, "error seeking position in file" },
        { 121, "invalid URL prefix on file name" },
        { 122, "tried to register too many IO drivers" },
        { 123, "driver initialization failed" },
        { 124, "matching driver is not registered" },
        { 125, "failed to parse input file URL" },
        { 151, "bad argument in shared memory driver" },
        { 152, "null pointer passed as an argument" },
        { 153, "no more free shared memory handles" },
        { 154, "shared memory driver is not initialized" },
        { 155, "IPC error returned by a system call" },
        { 156, "no memory in shared memory driver" },
        { 157, "resource deadlock would occur" },
        { 158, "attempt to open/create lock file failed" },
        { 159, "shared memory block cannot be resized at the moment" },
        { 201, "header already contains keywords" },
        { 202, "keyword not found in header" },
        { 203, "keyword record number is out of bounds" },
        { 204, "keyword value field is blank" },
        { 205, "string is missing the closing quote" },
        { 207, "illegal character in keyword name or card" },
        { 208, "required keywords out of order" },
        { 209, "keyword value is not a positive integer" },
        { 210, "couldn't find END keyword" },
        { 211, "illegal BITPIX keyword value" },
        { 212, "illegal NAXIS keyword value" },
        { 213, "illegal NAXISn keyword value" },
        { 214, "illegal PCOUNT keyword value" },
        { 215, "illegal GCOUNT keyword value" },
        { 216, "illegal TFIELDS keyword value" },
        { 217, "negative table row size" },
        { 218, "negative number of rows in table" },
        { 219, "column with this name not found in table" },
        { 220, "illegal value of SIMPLE keyword" },
        { 221, "Primary array doesn't start with SIMPLE" },
        { 222, "Second keyword not BITPIX" },
        { 223, "Third keyword not NAXIS" },
        { 224, "Couldn't find all the NAXISn keywords" },
        { 225, "HDU doesn't start with XTENSION keyword" },
        { 226, "the CHDU is not an ASCII table extension" },
        { 227, "the CHDU is not a binary table extension" },
        { 228, "couldn't find PCOUNT keyword" },
        { 229, "couldn't find GCOUNT keyword" },
        { 230, "couldn't find TFIELDS keyword" },
        { 231, "couldn't find TBCOLn keyword" },
        { 232, "couldn't find TFORMn keyword" },
        { 233, "the CHDU is not an IMAGE extension" },
        { 234, "TBCOLn keyword value < 0 or > rowlength" },
        { 235, "the CHDU is not a table" },
        { 236, "column is too wide to fit in table" },
        { 237, "more than 1 column name matches template" },
        { 241, "sum of column widths not = NAXIS1" },
        { 251, "unrecognizable FITS extension type" },
        { 252, "unknown record; 1st keyword not SIMPLE or XTENSION" },
        { 253, "END keyword is not blank" },
        { 254, "Header fill area contains non-blank chars" },
        { 255, "Illegal data fill bytes (not zero or blank)" },
        { 261, "illegal TFORM format code" },
        { 262, "unrecognizable TFORM datatype code" },
        { 263, "illegal TDIMn keyword value" },
        { 264, "invalid BINTABLE heap pointer is out of range" },
        { 301, "HDU number < 1 or > MAXHDU" },
        { 302, "column number < 1 or > tfields" },
        { 304, "tried to move to negative byte location in file" },
        { 306, "tried to read or write negative number of bytes" },
        { 307, "illegal starting row number in table" },
        { 308, "illegal starting element number in vector" },
        { 309, "this is not an ASCII string column" },
        { 310, "this is not a logical datatype column" },
        { 311, "ASCII table column has wrong format" },
        { 312, "Binary table column has wrong format" },
        { 314, "null value has not been defined" },
        { 317, "this is not a variable length column" },
        { 320, "illegal number of dimensions in array" },
        { 321, "first pixel number greater than last pixel" },
        { 322, "illegal BSCALE or TSCALn keyword = 0" },
        { 323, "illegal axis length < 1" }
    };

    public enum HDUType
    {
        IMAGE_HDU = 0,
        ASCII_TBL = 1,
        BINARY_TBL = 2
    }

    public enum DataType
    {
        TBIT = 1,
        TBYTE = 11,
        TSBYTE = 12,
        TLOGICAL = 14,
        TSTRING = 16,
        TUSHORT = 20,
        TSHORT = 21,
        TUINT = 30,
        TINT = 31,
        TULONG = 40,
        TLONG = 41,
        TINT32BIT = 41,
        TFLOAT = 42,
        TULONGLONG = 80,
        TLONGLONG = 81,
        TDOUBLE = 82,
        TCOMPLEX = 83,
        TDBLCOMPLEX = 163
    }

    // --- File operations ---

    public static int FitsOpenFile(out IntPtr fptr, string filename, out int status, bool isReadOnly)
    {
        if (isReadOnly)
            return FitsOpenFileReadOnly(out fptr, filename, out status);
        else
            return FitsOpenFileReadWrite(out fptr, filename, out status);
    }

    [DllImport("idavie_native")] public static extern int FitsOpenFileReadOnly(out IntPtr fptr, string filename, out int status);
    [DllImport("idavie_native")] public static extern int FitsOpenFileReadWrite(out IntPtr fptr, string filename, out int status);
    [DllImport("idavie_native")] public static extern int FitsCreateFile(out IntPtr fptr, string filename, out int status);
    [DllImport("idavie_native")] public static extern int FitsCloseFile(IntPtr fptr, out int status);
    [DllImport("idavie_native")] public static extern int FitsFlushFile(IntPtr fptr, out int status);

    // --- HDU navigation ---

    [DllImport("idavie_native")] public static extern int FitsGetHduCount(IntPtr fptr, out int hdunum, out int status);
    [DllImport("idavie_native")] public static extern int FitsGetHduType(IntPtr fptr, out int hdutype, out int status);
    [DllImport("idavie_native")] public static extern int FitsGetCurrentHdu(IntPtr fptr, out int hdunum);
    [DllImport("idavie_native")] public static extern int FitsMoveToHdu(IntPtr fptr, int hdunum, out int status);
    [DllImport("idavie_native")] public static extern int FitsMovabsHdu(IntPtr fptr, int hdunum, out int hdutype, out int status);
    [DllImport("idavie_native")] public static extern int FitsGetNumHdus(IntPtr fptr, out int numhdus, out int status);

    // --- Header operations ---

    [DllImport("idavie_native")] public static extern int FitsGetNumHeaderKeys(IntPtr fptr, out int keysexist, out int morekeys, out int status);
    [DllImport("idavie_native")] public static extern int FitsMakeKeyN(string keyroot, int value, StringBuilder keyname, out int status);
    [DllImport("idavie_native")] public static extern int FitsReadKeyString(IntPtr fptr, string keyname, StringBuilder colname, IntPtr comm, out int status);
    [DllImport("idavie_native")] public static extern int FitsReadKey(IntPtr fptr, int datatype, string keyname, StringBuilder colname, IntPtr comm, out int status);
    [DllImport("idavie_native")] public static extern int FitsReadKey(IntPtr fptr, int datatype, string keyname, IntPtr value, IntPtr comm, out int status);
    [DllImport("idavie_native")] public static extern int FitsReadKeyN(IntPtr fptr, int keynum, StringBuilder keyname, StringBuilder keyvalue, StringBuilder comment, out int status);
    [DllImport("idavie_native")] public static extern int FitsWriteKey(IntPtr fptr, int datatype, string keyname, IntPtr value, string comment, out int status);
    [DllImport("idavie_native")] public static extern int FitsUpdateKey(IntPtr fptr, int datatype, string keyname, IntPtr value, string comment, out int status);
    [DllImport("idavie_native")] public static extern int FitsDeleteKey(IntPtr fptr, string keyname, out int status);
    [DllImport("idavie_native")] public static extern int FitsWriteHistory(IntPtr fptr, string history, out int status);
    [DllImport("idavie_native")] public static extern int FitsCreateHdrPtrForAst(IntPtr fptr, out IntPtr header, out int nkeys, out int status);
    [DllImport("idavie_native")] public static extern int FreeFitsMemory(IntPtr header, out int status);

    // --- Image / table metadata ---

    [DllImport("idavie_native")] public static extern int FitsGetNumRows(IntPtr fptr, out long nrows, out int status);
    [DllImport("idavie_native")] public static extern int FitsGetNumCols(IntPtr fptr, out int ncols, out int status);
    [DllImport("idavie_native")] public static extern int FitsGetImageDims(IntPtr fptr, out int dims, out int status);
    [DllImport("idavie_native")] public static extern int FitsGetImageSize(IntPtr fptr, int dims, out IntPtr naxes, out int status);

    // --- Image operations ---

    [DllImport("idavie_native")] public static extern int FitsCreateImg(IntPtr fptr, int bitpix, int naxis, IntPtr naxes, out int status);
    [DllImport("idavie_native")] public static extern int FitsCopyHeader(IntPtr infptr, IntPtr outfptr, out int status);
    [DllImport("idavie_native")] public static extern int FitsCopyFile(IntPtr infptr, IntPtr outfptr, out int status);
    [DllImport("idavie_native")] public static extern int FitsCopyImageSection(string inFile, string outFile, string section, string historyTimeStamp, int selectedHDU, out int status);
    [DllImport("idavie_native")] public static extern int FitsCopyCubeSection(IntPtr infptr, IntPtr outfptr, string section, out int status);

    [DllImport("idavie_native")]
    [Obsolete("Replaced by FitsWriteSubImageInt16, which is more flexible.")]
    public static extern int FitsWriteImageInt16(IntPtr fptr, int dims, long nelements, IntPtr array, out int status);

    [DllImport("idavie_native")] public static extern int FitsWriteSubImageInt16(IntPtr fptr, IntPtr cornerMin, IntPtr cornerMax, IntPtr array, out int status);
    [DllImport("idavie_native")] public static extern int FitsWriteNewCopySubImageInt16(string newFileName, IntPtr fptr, IntPtr cornerMin, IntPtr cornerMax, IntPtr array, string historyTimeStamp, out int status);

    [Obsolete("FitsReadImageFloat is deprecated, please use FitsReadSubImageFloat instead.")]
    [DllImport("idavie_native")] public static extern int FitsReadImageFloat(IntPtr fptr, int dims, long nelem, out IntPtr array, out int status);

    [DllImport("idavie_native")] public static extern int FitsReadSubImageFloat(IntPtr fptr, int dims, int zAxis, IntPtr startPix, IntPtr finalPix, long nelem, out IntPtr array, out int status);

    [Obsolete("FitsReadImageInt16 is deprecated, please use FitsReadSubImageInt16 instead.")]
    [DllImport("idavie_native")] public static extern int FitsReadImageInt16(IntPtr fptr, int dims, long nelem, out IntPtr array, out int status);

    [DllImport("idavie_native")] public static extern int FitsReadSubImageInt16(IntPtr fptr, int dims, int zAxis, IntPtr startPix, IntPtr finalPix, long nelem, out IntPtr array, out int status);
    [DllImport("idavie_native")] public static extern int CreateEmptyImageInt16(long sizeX, long sizeY, long sizeZ, out IntPtr array);
    [DllImport("idavie_native")] public static extern int FreeFitsPtrMemory(IntPtr pointerToDelete);
    [DllImport("idavie_native")] public static extern int WriteMomentMap(IntPtr mainFitsFile, string fileName, IntPtr imagePixelArray, long xDims, long yDims, int mapNumber);

    // --- Table operations ---

    [DllImport("idavie_native")] public static extern int FitsReadColFloat(IntPtr fptr, int colnum, long firstrow, long firstelem, long nelem, out IntPtr array, out int status);
    [DllImport("idavie_native")] public static extern int FitsReadColString(IntPtr fptr, int colnum, long firstrow, long firstelem, long nelem, out IntPtr ptrarray, out IntPtr chararray, out int status);

    // --- Mask / utility ---

    [DllImport("idavie_native")] public static extern int WriteLogFile(char[] fileName, char[] content, int type);
    [DllImport("idavie_native")] public static extern int InsertSubArrayInt16(IntPtr mainArray, long mainArraySize, IntPtr subArray, long subArraySize, long startIndex);

    // --- Helper methods (all concerns mixed together) ---

    public static IDictionary<string, string> ExtractHeaders(IntPtr fptr, out int status)
    {
        int numberKeys, keysLeft;
        if (FitsGetNumHeaderKeys(fptr, out numberKeys, out keysLeft, out status) != 0)
        {
            Debug.LogError($"Fits extract header error {FitsErrorMessage(status)}");
            return null;
        }
        IDictionary<string, string> dict = new Dictionary<string, string>();
        for (int i = 1; i <= numberKeys; i++)
        {
            StringBuilder keyName = new StringBuilder(70);
            StringBuilder keyValue = new StringBuilder(70);
            FitsReadKeyN(fptr, i, keyName, keyValue, null, out status);
            string key = keyName.ToString();
            if (!dict.ContainsKey(key))
                dict.Add(key, keyValue.ToString());
            else
                dict[key] = dict[key] + keyValue.ToString();
            keyName.Clear();
            keyValue.Clear();
        }
        return dict;
    }

    [Obsolete("SaveNewInt16Mask is deprecated, please use SaveNewInt16SubMask instead.")]
    public static int SaveNewInt16Mask(IntPtr cubeFitsPtr, IntPtr maskData, long[] maskDataDims, string fileName)
    {
        IntPtr maskPtr = IntPtr.Zero;
        IntPtr keyValue = Marshal.AllocHGlobal(sizeof(int));
        int status = 0;
        long nelements = maskDataDims[0] * maskDataDims[1] * maskDataDims[2];
        IntPtr naxes = Marshal.AllocHGlobal(3 * sizeof(long));
        Marshal.Copy(maskDataDims, 0, naxes, maskDataDims.Length);
        if (FitsCreateFile(out maskPtr, fileName, out status) != 0) { Debug.LogError($"Fits create file error {FitsErrorMessage(status)}"); return status; }
        if (FitsCopyHeader(cubeFitsPtr, maskPtr, out status) != 0) { Debug.LogError($"Fits copy header error {FitsErrorMessage(status)}"); return status; }
        Marshal.WriteInt32(keyValue, 16);
        if (FitsUpdateKey(maskPtr, 21, "BITPIX", keyValue, null, out status) != 0) { Debug.LogError($"Fits update key error {FitsErrorMessage(status)}"); return status; }
        Marshal.WriteInt32(keyValue, 3);
        if (FitsUpdateKey(maskPtr, 21, "NAXIS", keyValue, null, out status) != 0) { Debug.LogError($"Fits update key error {FitsErrorMessage(status)}"); return status; }
        if (FitsDeleteKey(maskPtr, "BUNIT", out status) != 0) { Debug.LogWarning("Could not delete fits unit key. It probably does not exist!"); status = 0; }
        if (FitsWriteImageInt16(maskPtr, 3, nelements, maskData, out status) != 0) { Debug.LogError("Fits write image error " + FitsErrorMessage(status)); return status; }
        var historyTimeStamp = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
        if (FitsWriteHistory(maskPtr, $"Edited by iDaVIE at {historyTimeStamp}", out status) != 0) { Debug.LogError("Error writing history!"); return status; }
        if (FitsFlushFile(maskPtr, out status) != 0) { Debug.LogError($"Fits flush file error {FitsErrorMessage(status)}"); return status; }
        if (FitsCloseFile(maskPtr, out status) != 0) { Debug.LogError($"Fits close file error {FitsErrorMessage(status)}"); return status; }
        if (keyValue != IntPtr.Zero) { Marshal.FreeHGlobal(keyValue); keyValue = IntPtr.Zero; }
        return status;
    }

    public static int SaveNewInt16SubMask(IntPtr cubeFitsPtr, IntPtr maskData, IntPtr firstPix, IntPtr lastPix, string fileName)
    {
        IntPtr maskPtr = IntPtr.Zero;
        IntPtr keyValue = Marshal.AllocHGlobal(sizeof(int));
        int status = 0;
        if (FitsCreateFile(out maskPtr, fileName, out status) != 0) { Debug.LogError($"Fits create file error {FitsErrorMessage(status)}"); return status; }
        if (FitsCopyHeader(cubeFitsPtr, maskPtr, out status) != 0) { Debug.LogError($"Fits copy file error {FitsErrorMessage(status)}"); return status; }
        Marshal.WriteInt32(keyValue, 16);
        if (FitsUpdateKey(maskPtr, 21, "BITPIX", keyValue, null, out status) != 0) { Debug.LogError($"Fits update key error {FitsErrorMessage(status)}"); return status; }
        Marshal.WriteInt32(keyValue, 3);
        if (FitsUpdateKey(maskPtr, 21, "NAXIS", keyValue, null, out status) != 0) { Debug.LogError($"Fits update key error {FitsErrorMessage(status)}"); return status; }
        if (FitsDeleteKey(maskPtr, "BUNIT", out status) != 0) { Debug.LogWarning("Could not delete fits unit key. It probably does not exist!"); status = 0; }
        if (FitsWriteSubImageInt16(maskPtr, firstPix, lastPix, maskData, out status) != 0) { Debug.LogError("Fits write subset error " + FitsErrorMessage(status)); FitsCloseFile(maskPtr, out status); return status; }
        var historyTimeStamp = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
        if (FitsWriteHistory(maskPtr, $"Edited by iDaVIE at {historyTimeStamp}", out status) != 0) { Debug.LogError("Error writing history!"); return status; }
        if (FitsFlushFile(maskPtr, out status) != 0) { Debug.LogError($"Fits flush file error {FitsErrorMessage(status)}"); return status; }
        if (FitsCloseFile(maskPtr, out status) != 0) { Debug.LogError($"Fits close file error {FitsErrorMessage(status)}"); return status; }
        if (keyValue != IntPtr.Zero) { Marshal.FreeHGlobal(keyValue); keyValue = IntPtr.Zero; }
        return status;
    }

    public static int UpdateOldInt16SubMask(IntPtr oldMaskPtr, IntPtr maskDataToSave, IntPtr firstPix, IntPtr lastPix)
    {
        Debug.Log("Overwriting old mask");
        int status = 0;
        IntPtr keyValue = Marshal.AllocHGlobal(sizeof(int));
        Marshal.WriteInt32(keyValue, 3);
        if (FitsUpdateKey(oldMaskPtr, 21, "NAXIS", keyValue, null, out status) != 0) { Debug.LogError($"Fits update key error {FitsErrorMessage(status)}"); return status; }
        Debug.Log("Keys updated, writing data.");
        if (FitsWriteSubImageInt16(oldMaskPtr, firstPix, lastPix, maskDataToSave, out status) != 0) { Debug.LogError($"Fits write image error {FitsErrorMessage(status)}"); FitsCloseFile(oldMaskPtr, out status); return status; }
        Debug.Log("Writing data complete, writing history.");
        var historyTimeStamp = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
        if (FitsWriteHistory(oldMaskPtr, $"Edited by iDaVIE at {historyTimeStamp}", out status) != 0) { Debug.LogError("Error writing history!"); return status; }
        Debug.Log("Writing history complete, flushing buffer.");
        if (FitsFlushFile(oldMaskPtr, out status) != 0) { Debug.LogError($"Fits flush file error {FitsErrorMessage(status)}"); return status; }
        if (keyValue != IntPtr.Zero) { Marshal.FreeHGlobal(keyValue); }
        return status;
    }

    public static bool UpdateMaskVoxel(IntPtr maskDataPtr, long[] maskDims, Vector3Int location, short value)
    {
        location -= Vector3Int.one;
        long index = location.x + location.y * maskDims[0] + location.z * (maskDims[0] * maskDims[1]);
        Marshal.WriteInt16(new IntPtr(maskDataPtr.ToInt64() + index * sizeof(short)), value);
        return true;
    }

    public static int SaveSubMask(IntPtr fitsPtr, IntPtr maskData, int[] firstPix, int[] lastPix, string fileName, bool exporting)
    {
        bool isNewFile = fileName != null;
        IntPtr fPix = Marshal.AllocHGlobal(sizeof(int) * firstPix.Length);
        IntPtr lPix = Marshal.AllocHGlobal(sizeof(int) * lastPix.Length);
        Marshal.Copy(firstPix, 0, fPix, firstPix.Length);
        Marshal.Copy(lastPix, 0, lPix, lastPix.Length);
        Debug.Log("Writing submask from first pixel [" + String.Join(", ", firstPix) + "] and end pixel [" + String.Join(", ", lastPix) + "].");
        if (isNewFile)
        {
            if (exporting)
            {
                Debug.Log("Attempting to export mask to a new file " + fileName + ".");
                int status = 0;
                var historyTimeStamp = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
                FitsWriteNewCopySubImageInt16(fileName, fitsPtr, fPix, lPix, maskData, historyTimeStamp, out status);
                if (status != 0) Debug.LogError($"Fits save new copy error {FitsErrorMessage(status)}, see plugin log for details.");
                return status;
            }
            else
            {
                Debug.Log("Saving mask file " + fileName + " for the first time.");
                return SaveNewInt16SubMask(fitsPtr, maskData, fPix, lPix, fileName);
            }
        }
        else
        {
            Debug.Log("Overwriting existing mask file " + fileName + ".");
            return UpdateOldInt16SubMask(fitsPtr, maskData, fPix, lPix);
        }
    }

    public static string FitsErrorMessage(int status)
    {
        return $"#{status} {ErrorCodes[status]}";
    }

    public static string FitsTableGetColName(IntPtr fitsPtr, int col)
    {
        int status = 0;
        StringBuilder keyword = new StringBuilder(75);
        StringBuilder colName = new StringBuilder(71);
        FitsMakeKeyN("TTYPE", col + 1, keyword, out status);
        if (FitsReadKeyString(fitsPtr, keyword.ToString(), colName, IntPtr.Zero, out status) != 0)
        {
            Debug.Log("Fits Read column name error #" + status.ToString());
            FitsCloseFile(fitsPtr, out status);
            return "";
        }
        return colName.ToString();
    }

    public static string FitsTableGetColUnit(IntPtr fitsPtr, int col)
    {
        int status = 0;
        StringBuilder keyword = new StringBuilder(75);
        StringBuilder colUnit = new StringBuilder(71);
        FitsMakeKeyN("TUNIT", col + 1, keyword, out status);
        if (FitsReadKeyString(fitsPtr, keyword.ToString(), colUnit, IntPtr.Zero, out status) != 0)
        {
            if (status == 202) { Debug.Log("No unit in column #" + col); status = 0; }
            else { Debug.Log("Fits Read unit error #" + status.ToString()); FitsCloseFile(fitsPtr, out status); return null; }
        }
        return colUnit.ToString();
    }

    public static string FitsTableGetColFormat(IntPtr fitsPtr, int col)
    {
        int status = 0;
        StringBuilder keyword = new StringBuilder(75);
        StringBuilder colFormat = new StringBuilder(71);
        FitsMakeKeyN("TFORM", col + 1, keyword, out status);
        if (FitsReadKeyString(fitsPtr, keyword.ToString(), colFormat, IntPtr.Zero, out status) != 0)
        {
            Debug.Log("Fits Read column unit error #" + status.ToString());
            FitsCloseFile(fitsPtr, out status);
            return "";
        }
        return colFormat.ToString();
    }
}
