#pragma once


#define DllExport __declspec (dllexport)

#define EXIT_SUCCESS 0
#define EXIT_FAILURE 1

#define STRING_SIZE 70

#include "XPathHelper.h"
#include "VTable.h"
#include "global.h"
#include "MissingValueException.h"
#include "DatatypeMismatchException.h"


extern "C"
{

	DllExport int VOTableInitialize(VTable**);

	DllExport int VOTableOpenFile(VTable*, char*, char*, int*);

	DllExport int VOTableGetMetaData(VTable*, TableMetaData**, int*);

	DllExport int VOTableGetTableData(VTable*, TableData**, int*);

	DllExport int VOTableGetName(VTable*, char*&, int*);

	DllExport int MetaDataGetNumCols(TableMetaData*, int*, int*);

	DllExport int MetaDataGetField(TableMetaData*, Field**, int, int*);

	DllExport int TableDataGetRow(TableData*, Row**, int, int*);

	DllExport int TableDataGetNumRows(TableData*, int*, int*);

	DllExport int RowGetColumn(Row*, Column**, int, int*);

	DllExport int FieldGetName(Field*, char*&, int*);

	DllExport int FieldGetDataType(Field*, int*, int*);

	DllExport int ColumnGetBoolArray(Column*, Bool*&, int*, int*);

	DllExport int ColumnGetBitArray(Column*, char*&, int*, int*);

	DllExport int ColumnGetByteArray(Column*, unsigned char*&, int*, int*);

	DllExport int ColumnGetShortArray(Column*, short*&, int*, int*);

	DllExport int ColumnGetIntArray(Column*, int*&, int*, int*);

	DllExport int ColumnGetLongArray(Column*, long*&, int*, int*);

	DllExport int ColumnGetCharArray(Column*, char*&, int*, int*);

	DllExport int ColumnGetUnicodeArray(Column*, unsigned short*&, int*, int*);

	DllExport int ColumnGetFloatArray(Column*, float*&, int*, int*);

	DllExport int ColumnGetDoubleArray(double*&, int*, int*);

	DllExport int FreeMemory(void*);

}


