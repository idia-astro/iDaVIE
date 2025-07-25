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
#ifndef NATIVE_PLUGINS_DATA_ANALYSIS_TOOL_H
#define NATIVE_PLUGINS_DATA_ANALYSIS_TOOL_H

#include <iostream>
#include <vector>
#include <algorithm>
#include <math.h>
#include <omp.h>

#define DllExport __declspec (dllexport)

#define EXIT_SUCCESS 0
#define EXIT_FAILURE 1

/**
 * @brief Represents the bounding box and ID of a source region identified in a 3D mask volume.
 *
 * This struct holds the spatial extent of a connected region (or source) in the mask, defined
 * by its minimum and maximum coordinates in the X, Y, and Z dimensions. Each source is
 * associated with a unique, non-zero `maskVal`.
 */
struct SourceInfo
{
    int64_t minX;     /**< Minimum X coordinate of the source */
    int64_t maxX;     /**< Maximum X coordinate of the source */
    int64_t minY;     /**< Minimum Y coordinate of the source */
    int64_t maxY;     /**< Maximum Y coordinate of the source */
    int64_t minZ;     /**< Minimum Z coordinate of the source */
    int64_t maxZ;     /**< Maximum Z coordinate of the source */
    int16_t maskVal;  /**< Unique non-zero value representing the source in the mask */

    char _padding[6]; /**< Internal padding to maintain memory alignment (optional) */
};

struct SourceStats
{
    // Bounding box
    int64_t minX, maxX;
    int64_t minY, maxY;
    int64_t minZ, maxZ;
    // Number of finite voxels
    int64_t numVoxels;
    // Centroid
    double cX, cY, cZ;
    // Flux
    double sum;
    double peak;
    // Vsys (in channel units)
    double channelVsys;
    double channelW20;
    double veloVsys;
    double veloW20;

    double* spectralProfilePtr;
    int64_t spectralProfileSize;
};

template<bool maxMode> int DataCropAndDownsample(const float *, float **, int64_t , int64_t , int64_t , int64_t , int64_t , int64_t , int64_t , int64_t , int64_t , int , int , int);

extern "C"
{
#include "ast.h"
DllExport int FindMaxMin(const float *, int64_t , float *, float *);
DllExport int FindStats(const float* , int64_t , float* , float* , float* , float* );
DllExport int GetVoxelFloatValue(const float *, float *, int64_t , int64_t , int64_t , int64_t , int64_t , int64_t );
DllExport int GetVoxelInt16Value(const int16_t *, int16_t *, int64_t , int64_t , int64_t , int64_t , int64_t , int64_t );
DllExport int GetXProfile(const float *, float **, int64_t , int64_t , int64_t , int64_t , int64_t );
DllExport int GetYProfile(const float *, float **, int64_t , int64_t , int64_t , int64_t , int64_t );
DllExport int GetZProfile(const float *, float **, int64_t , int64_t , int64_t , int64_t , int64_t );
DllExport int DataCropAndDownsample(const float *, float **, int64_t , int64_t , int64_t , int64_t , int64_t , int64_t , int64_t , int64_t , int64_t , int , int , int, bool);
DllExport int MaskCropAndDownsample(const int16_t *, int16_t **, int64_t , int64_t , int64_t , int64_t , int64_t , int64_t , int64_t , int64_t , int64_t , int , int , int );
DllExport int GetPercentileValuesFromHistogram(const int*, int, float, float, float, float, float*, float*);
DllExport int GetPercentileValuesFromData(const float*, int64_t, float, float, float*, float*);
DllExport int GetHistogram(const float* , int64_t , int , float , float , int** );
DllExport int GetMaskedSources(const int16_t*, int64_t, int64_t, int64_t, int*, SourceInfo**);
DllExport int GetSourceStats(const float*, const int16_t*, int64_t, int64_t, int64_t, SourceInfo, SourceStats*, AstFrameSet*);
DllExport int GetZScale(const float*, int64_t, int64_t, float*, float*);
DllExport int FreeDataAnalysisMemory(void* );
}

#endif //NATIVE_PLUGINS_DATA_ANALYSIS_TOOL_H
