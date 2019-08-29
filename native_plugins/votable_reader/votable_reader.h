#pragma once


#define DllExport __declspec (dllexport)

#define EXIT_SUCCESS 0
#define EXIT_FAILURE 1

#include "XPathHelper.h"
#include "VTable.h"
#include "global.h"
#include "MissingValueException.h"
#include "DatatypeMismatchException.h"


extern "C"
{

	DllExport int VOTableInitialize(VTable**);

	DllExport int VOTableOpenFile(VTable*, char*, char*, int*);

	DllExport int FreeMemory(void*);

}


class votable_reader
{
};

