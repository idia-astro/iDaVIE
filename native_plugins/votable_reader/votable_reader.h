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

	DllExport int FieldGetName(Field*, char*&, int*);

	DllExport int FreeMemory(void*);

}


class votable_reader
{
};

