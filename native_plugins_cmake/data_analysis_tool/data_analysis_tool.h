#ifndef NATIVE_PLUGINS_DATA_ANALYSIS_TOOL_H
#define NATIVE_PLUGINS_DATA_ANALYSIS_TOOL_H

#include <iostream>
#include <vector>
#include <algorithm>
#include <math.h>
#include <omp.h>

///Insert these three lines to debug directly out to a file:
//char* str = new char[70];
//freopen("debug.txt", "a", stdout);
//printf("%s\n", str);

#define DllExport __declspec (dllexport)

#define EXIT_SUCCESS 0
#define EXIT_FAILURE 1

struct SourceInfo
{
    int64_t minX, maxX;
    int64_t minY, maxY;
    int64_t minZ, maxZ;
    int16_t maskVal;
    char _padding[6];
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
};

extern "C"
{
DllExport int FindMaxMin(const float *, int64_t , float *, float *);
DllExport int FindStats(const float* , int64_t , float* , float* , float* , float* );
DllExport int GetVoxelFloatValue(const float *, float *, int64_t , int64_t , int64_t , int64_t , int64_t , int64_t );
DllExport int GetVoxelInt16Value(const int16_t *, int16_t *, int64_t , int64_t , int64_t , int64_t , int64_t , int64_t );
DllExport int GetXProfile(const float *, float **, int64_t , int64_t , int64_t , int64_t , int64_t );
DllExport int GetYProfile(const float *, float **, int64_t , int64_t , int64_t , int64_t , int64_t );
DllExport int GetZProfile(const float *, float **, int64_t , int64_t , int64_t , int64_t , int64_t );
DllExport int DataCropAndDownsample(const float *, float **, int64_t , int64_t , int64_t , int64_t , int64_t , int64_t , int64_t , int64_t , int64_t , int , int , int );
DllExport int MaskCropAndDownsample(const int16_t *, int16_t **, int64_t , int64_t , int64_t , int64_t , int64_t , int64_t , int64_t , int64_t , int64_t , int , int , int );
DllExport int GetHistogram(const float* , int64_t , int , float , float , int** );
DllExport int GetMaskedSources(const int16_t*, int64_t, int64_t, int64_t, int*, SourceInfo**);
DllExport int GetSourceStats(const float*, const int16_t*, int64_t, int64_t, int64_t, SourceInfo, SourceStats*);
DllExport int FreeMemory(void* );
}

#endif //NATIVE_PLUGINS_DATA_ANALYSIS_TOOL_H
