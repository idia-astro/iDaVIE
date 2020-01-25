using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Text;



public class VOTableReader
{
    public enum field_datatype
    {
        datatype_not_specified = 0,
        BooleanType = 1,
        BitType,
        UnsignedByteType,
        ShortType,
        IntType,
        LongType,
        CharType,
        UnicodeCharType,
        FloatType,
        DoubleType,
        FloatComplexType,
        DoubleComplexType
    };

    public IntPtr[] getFieldArray(IntPtr meta_ptr)
    {
        //get column number
        // new intptr array of field types
        return null;
    }


    [DllImport("votable_reader")]
    public static extern int VOTableInitialize(out IntPtr table_ptr);

    [DllImport("votable_reader")]
    public static extern int VOTableOpenFile(IntPtr table_ptr, string filename, string xpath, out int status);

    [DllImport("votable_reader")]
    public static extern int VOTableGetMetaData(IntPtr table_ptr, out IntPtr meta_ptr, out int status);

    [DllImport("votable_reader")]
    public static extern int VOTableGetName(IntPtr table_ptr, out IntPtr name_ptr, out int status);

    [DllImport("votable_reader")]
    public static extern int VOTableGetTableData(IntPtr table_ptr, out IntPtr data_ptr, out int status);

    [DllImport("votable_reader")]
    public static extern int TableDataGetNumRows(IntPtr data_ptr, out int nrows, out int status);

    [DllImport("votable_reader")]
    public static extern int TableDataGetRow(IntPtr data_ptr, out IntPtr row_ptr, out int status);

    [DllImport("votable_reader")]
    public static extern int RowGetColumn(IntPtr row_ptr, out IntPtr column_ptr, out int status);

    [DllImport("votable_reader")]
    public static extern int ColumnGetFloatArray(IntPtr col_ptr, out IntPtr float_array, out int numElements, out int status);

    [DllImport("votable_reader")]
    public static extern int MetaDataGetNumCols(IntPtr meta_ptr, out int ncols, out int status);

    [DllImport("votable_reader")]
    public static extern int MetaDataGetField(IntPtr meta_ptr, out IntPtr field_ptr, int fieldNum, out int status);

    [DllImport("votable_reader")]
    public static extern int FieldGetName(IntPtr field_ptr, out IntPtr name_ptr, out int status);

    [DllImport("votable_reader")]
    public static extern int FieldGetDataType(IntPtr field_ptr, out int datatype, out int status);

    [DllImport("votable_reader")]
    public static extern int ColumnGetFloatArray(IntPtr col_ptr, IntPtr float_array, out int number_elements, out int status);

    [DllImport("votable_reader")]
    public static extern int FreeMemory(IntPtr ptr);

}