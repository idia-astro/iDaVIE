#ifndef NATIVE_PLUGINS_AST_TOOL_H
#define NATIVE_PLUGINS_AST_TOOL_H

#define DllExport __declspec (dllexport)

#include <cstring>

extern "C"
{
#include "star/ast.h"
DllExport int initFrame(const char*, AstFrameSet**);
DllExport int format(AstFrameSet*, int, double, const char*);
DllExport int set(AstFrameSet*, const char*)
}

#endif //NATIVE_PLUGINS_AST_TOOL_H