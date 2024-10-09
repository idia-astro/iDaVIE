/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 Inter-University Institute for Data Intensive Astronomy
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
#ifndef NATIVE_PLUGINS_AST_TOOL_H
#define NATIVE_PLUGINS_AST_TOOL_H

#define DllExport __declspec (dllexport)

#include <cstring>
#include <iostream>

extern "C"
{
#include "ast.h"

DllExport int InitAstFrameSet(AstFrameSet**, const char*, double);

DllExport int GetAstFrame(AstFrameSet*, AstFrame**, int index);

DllExport int GetAltSpecSet(AstFrameSet*, AstFrameSet**, const char*, const char*, const char*);

DllExport int Show(AstObject*);

DllExport int Format(AstFrameSet*, int, double, char*, int);

DllExport int Set(AstFrameSet*, const char*);

DllExport int Clear(AstObject*, const char*);

DllExport void Dump(AstFrameSet*, char*);

DllExport int GetString(AstFrameSet*, const char*, char*, int);

DllExport int SetString(AstFrameSet*, const char*, const char*);

DllExport bool HasAttribute(AstFrameSet*, const char*);

DllExport int Norm(AstFrameSet*, double, double, double, double*, double*, double*);

DllExport int Distance1D(AstFrame*, double, double, int, double*);

DllExport int Distance2D(AstFrame*, double, double, double, double, double*);

DllExport int Transform(AstFrameSet*, int, const double[], const double[], int, double[], double[]);

DllExport int Transform3D(AstFrameSet*, double, double, double, const int, double*, double*, double*);

DllExport int SpectralTransform(AstFrameSet*, const char*, const char*, const char*, const double, const int, double*, char*, int);

DllExport void DeleteObject(AstFrameSet*);

DllExport int Copy(AstFrameSet*, AstFrameSet**);

DllExport int Invert(AstFrameSet*);

DllExport void AstEnd();

DllExport void FreeAstMemory(void*);

}

#endif //NATIVE_PLUGINS_AST_TOOL_H