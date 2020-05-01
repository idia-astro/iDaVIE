#ifndef NATIVE_PLUGINS_AST_TOOL_H
#define NATIVE_PLUGINS_AST_TOOL_H

#define DllExport __declspec (dllexport)


#include <cfitsio/fitsio.h>

extern "C"
{
#include "star/ast.h"
DllExport int WCSRead();
DllExport int FitsOpenFileReadOnly(fitsfile**, char*,  int*);
}

#endif //NATIVE_PLUGINS_AST_TOOL_H