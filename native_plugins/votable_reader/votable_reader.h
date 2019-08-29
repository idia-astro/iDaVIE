#pragma once


#define DllExport __declspec (dllexport)

#define EXIT_SUCCESS 0
#define EXIT_FAILURE 1

#include "XPathHelper.h"
#include "VTable.h"


extern "C"
{

	DllExport int VOTableOpenFile(VTable*, char*, char*, int*);

}


class votable_reader
{
};

