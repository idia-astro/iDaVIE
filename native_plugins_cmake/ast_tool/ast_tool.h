#ifndef NATIVE_PLUGINS_AST_TOOL_H
#define NATIVE_PLUGINS_AST_TOOL_H

#define DllExport __declspec (dllexport)

#include <cstring>

extern "C"
{
#include "star/ast.h"

int InitFrame(AstFrameSet**, const char*);

int Format(AstFrameSet*, int, double, char*, int);

int Set(AstFrameSet*, const char*);

int Clear(AstObject*, const char*);

int GetString(AstFrameSet*, const char*, char*, int);

int Norm(AstFrameSet*, double[]);

int Transform(AstFrameSet*, int, const double[], const double[], int, double[], double[]);

int Transform3D(AstSpecFrame*, double, double, double, const int, double**);

void DeleteObject(AstFrameSet*);

int Copy(AstFrameSet*, AstFrameSet**);

void Invert(AstFrameSet*);

void FreeMemory(void*);

}

#endif //NATIVE_PLUGINS_AST_TOOL_H