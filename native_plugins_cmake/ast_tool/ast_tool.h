#ifndef NATIVE_PLUGINS_AST_TOOL_H
#define NATIVE_PLUGINS_AST_TOOL_H

#define DllExport __declspec (dllexport)

#include <cstring>

extern "C"
{
#include "star/ast.h"
DllExport int InitFrame(AstFrameSet**, const char*);
DllExport int Format(AstFrameSet*, int, double, const char*);
DllExport int Set(AstFrameSet*, const char*);
}

#endif //NATIVE_PLUGINS_AST_TOOL_H