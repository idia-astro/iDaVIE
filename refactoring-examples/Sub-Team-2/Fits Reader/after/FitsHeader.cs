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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

public static class FitsHeader
{
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

    [DllImport("idavie_fits")] public static extern int FitsGetNumHeaderKeys(IntPtr fptr, out int keysexist, out int morekeys, out int status);
    [DllImport("idavie_fits")] public static extern int FitsMakeKeyN(string keyroot, int value, StringBuilder keyname, out int status);
    [DllImport("idavie_fits")] public static extern int FitsReadKeyString(IntPtr fptr, string keyname, StringBuilder colname, IntPtr comm, out int status);
    [DllImport("idavie_fits")] public static extern int FitsReadKey(IntPtr fptr, int datatype, string keyname, StringBuilder colname, IntPtr comm, out int status);
    [DllImport("idavie_fits")] public static extern int FitsReadKey(IntPtr fptr, int datatype, string keyname, IntPtr value, IntPtr comm, out int status);
    [DllImport("idavie_fits")] public static extern int FitsReadKeyN(IntPtr fptr, int keynum, StringBuilder keyname, StringBuilder keyvalue, StringBuilder comment, out int status);
    [DllImport("idavie_fits")] public static extern int FitsWriteKey(IntPtr fptr, int datatype, string keyname, IntPtr value, string comment, out int status);
    [DllImport("idavie_fits")] public static extern int FitsUpdateKey(IntPtr fptr, int datatype, string keyname, IntPtr value, string comment, out int status);
    [DllImport("idavie_fits")] public static extern int FitsDeleteKey(IntPtr fptr, string keyname, out int status);
    [DllImport("idavie_fits")] public static extern int FitsWriteHistory(IntPtr fptr, string history, out int status);
    [DllImport("idavie_fits")] public static extern int FitsCreateHdrPtrForAst(IntPtr fptr, out IntPtr header, out int nkeys, out int status);
    [DllImport("idavie_fits")] public static extern int FreeFitsMemory(IntPtr header, out int status);

    public static IDictionary<string, string> ExtractHeaders(IntPtr fptr, out int status)
    {
        int numberKeys, keysLeft;
        if (FitsGetNumHeaderKeys(fptr, out numberKeys, out keysLeft, out status) != 0)
        {
            Console.Error.WriteLine($"Fits extract header error {FitsErrorMessage(status)}");
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

    public static string FitsErrorMessage(int status)
    {
        return $"#{status} {ErrorCodes[status]}";
    }
}
