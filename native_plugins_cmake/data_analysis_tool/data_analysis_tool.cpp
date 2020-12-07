#include "data_analysis_tool.h"

#include <unordered_map>
#include <iostream>

using namespace std;

int FindMaxMin(const float *dataPtr, int64_t numberElements, float *maxResult, float *minResult)
{
    float maxVal = -numeric_limits<float>::max();
    float minVal = numeric_limits<float>::max();
    #pragma omp parallel
    {
        float currentMax = -numeric_limits<float>::max();
        float currentMin = numeric_limits<float>::max();
        #pragma omp for
        for (int64_t i = 0; i < numberElements; i++)
        {
            float val = dataPtr[i];
            currentMax = fmax(currentMax, val);
            currentMin = fmin(currentMin, val);
        }
        #pragma omp critical
        {
            maxVal = fmax(currentMax, maxVal);
            minVal = fmin(currentMin, minVal);
        }
        *maxResult = maxVal;
        *minResult = minVal;
    }
    return EXIT_SUCCESS;
}

int FindStats(const float* dataPtr, int64_t numberElements, float* maxResult, float* minResult, float* meanResult, float* stdDevResult)
{
    float maxVal = -numeric_limits<float>::max();
    float minVal = numeric_limits<float>::max();
    double sum = 0;
    double squareSum = 0;
    #pragma omp parallel
    {
        float currentMax = -numeric_limits<float>::max();
        float currentMin = numeric_limits<float>::max();
        #pragma omp for reduction(+:sum) reduction(+:squareSum)
        for (int64_t i = 0; i < numberElements; i++)
        {
            double val = dataPtr[i];
            if (!isnan(val))
            {
                sum += val;
                squareSum += val * val;
                currentMax = fmax(currentMax, val);
                currentMin = fmin(currentMin, val);
            }
        }
        #pragma omp critical
        {
            maxVal = fmax(currentMax, maxVal);
            minVal = fmin(currentMin, minVal);
        }
        *maxResult = maxVal;
        *minResult = minVal;
        *meanResult = sum / numberElements;
        *stdDevResult = sqrt((numberElements * squareSum - sum * sum) / (numberElements * (numberElements - 1)));
    }
    return EXIT_SUCCESS;
}

int GetVoxelFloatValue(const float *dataPtr, float *voxelValue, int64_t xDim, int64_t yDim, int64_t zDim, int64_t x, int64_t y, int64_t z)
{
    float outValue;
    if (x > xDim || y > yDim || z > zDim || x < 1 || y < 1 || z < 1)
        return EXIT_FAILURE;
    int64_t index = xDim * yDim * (z - 1) + xDim * (y - 1) + (x - 1);
    outValue = dataPtr[index];
    *voxelValue = outValue;
    return EXIT_SUCCESS;
}

int GetVoxelInt16Value(const int16_t *dataPtr, int16_t *voxelValue, int64_t xDim, int64_t yDim, int64_t zDim, int64_t x, int64_t y, int64_t z)
{
    int16_t outValue;
    if (x > xDim || y > yDim || z > zDim || x < 1 || y < 1 || z < 1)
        return EXIT_FAILURE;
    int64_t index = xDim * yDim * (z - 1) + xDim * (y - 1) + (x - 1);
    outValue = dataPtr[index];
    *voxelValue = outValue;
    return EXIT_SUCCESS;
}

//TODO make compatible with int64_t
int GetXProfile(const float *dataPtr, float **profile, int64_t xDim, int64_t yDim, int64_t zDim, int64_t y, int64_t z)
{
    float* newProfile = new float[xDim];
    if (y > yDim || z > zDim || y < 1 || z < 1)
        return EXIT_FAILURE;
    for (int i = 0; i < xDim; i++)
        newProfile[i] = dataPtr[(z - 1) * xDim * yDim + (y - 1) * xDim + i];
    *profile = newProfile;
    return EXIT_SUCCESS;
}

int GetYProfile(const float *dataPtr, float **profile, int64_t xDim, int64_t yDim, int64_t zDim, int64_t x, int64_t z)
{
    float* newProfile = new float[yDim];
    if (x > xDim || z > zDim || x < 1 || z < 1)
        return EXIT_FAILURE;
    for (int i = 0; i < yDim; i++)
        newProfile[i] = dataPtr[(z - 1) * xDim * yDim + yDim * i + (x - 1)];
    *profile = newProfile;
    return EXIT_SUCCESS;

}

int GetZProfile(const float *dataPtr, float **profile, int64_t xDim, int64_t yDim, int64_t zDim, int64_t x, int64_t y)
{
    float* newProfile = new float[xDim];
    if (x > xDim || y > yDim || x < 1 || y < 1)
        return EXIT_FAILURE;
    for (int i = 0; i < zDim; i++)
        newProfile[i] = dataPtr[i * xDim * yDim + (y - 1) * xDim + (x - 1)];
    *profile = newProfile;
    return EXIT_SUCCESS;
}

int DataCropAndDownsample(const float *dataPtr, float **newDataPtr, int64_t dimX, int64_t dimY, int64_t dimZ,
    int64_t cropX1, int64_t cropY1, int64_t cropZ1, int64_t cropX2, int64_t cropY2, int64_t cropZ2, int factorX, int factorY, int factorZ)
{
    if (cropX1 > dimX || cropX2 > dimX || cropY1 > dimY || cropY2 > dimY || cropZ1 > dimZ || cropZ2 > dimZ || cropX1 < 1 || cropX2 < 1 || cropY1 < 1 || cropY2 < 1 || cropZ1 < 1 || cropZ2 < 1)
        return EXIT_FAILURE;
    int64_t newDimX = (abs(cropX1 - cropX2) + 1) / factorX;
    int64_t newDimY = (abs(cropY1 - cropY2) + 1) / factorY;
    int64_t newDimZ = (abs(cropZ1 - cropZ2) + 1) / factorZ;
    if ((abs(cropX1 - cropX2) + 1) % factorX != 0)
        newDimX++;
    if ((abs(cropY1 - cropY2) + 1) % factorY != 0)
        newDimY++;
    if ((abs(cropZ1 - cropZ2) + 1) % factorZ != 0)
        newDimZ++;
    int64_t newSize = newDimX * newDimY * newDimZ;
    float* reducedCube = new float[newSize] {};
    int64_t smallX = min(cropX1, cropX2);
    int64_t smallY = min(cropY1, cropY2);
    int64_t smallZ = min(cropZ1, cropZ2);
    #pragma omp parallel
    #pragma omp for
    for (auto newZ = 0; newZ < newDimZ; newZ++)
    {
        int64_t  pixelCount, oldZ, oldY, oldX;
        float pixelSum, pixVal;
        size_t blockSizeX, blockSizeY, blockSizeZ;
        for (auto newY = 0; newY < newDimY; newY++)
        {
            for (auto newX = 0; newX < newDimX; newX++)
            {
                pixelSum = 0;
                pixelCount = 0;
                blockSizeX = factorX;
                blockSizeY = factorY;
                blockSizeZ = factorZ;
                if ((newX + 1) * factorX >= dimX) {
                    blockSizeX = dimX - (newX*factorX);
                }
                if ((newY + 1) * factorY >= dimY) {
                    blockSizeY = dimY - (newY*factorY);
                }
                if ((newZ + 1) * factorZ >= dimZ) {
                    blockSizeZ = dimZ - (newZ*factorZ);
                }
                for (auto pixelZ = 0; pixelZ < blockSizeZ; pixelZ++)
                {
                    oldZ = newZ * factorZ + pixelZ + smallZ - 1;
                    for (auto pixelY = 0; pixelY < blockSizeY; pixelY++)
                    {
                        oldY = newY * factorY + pixelY + smallY - 1;
                        for (auto pixelX = 0; pixelX < blockSizeX; pixelX++)
                        {
                            oldX = newX * factorX + pixelX + smallX - 1;
                            pixVal = dataPtr[oldZ * dimX * dimY + oldY * dimX + oldX];
                            if (!isnan(pixVal)) {
                                pixelCount++;
                                pixelSum += pixVal;
                            }
                        }
                    }
                }
                reducedCube[newZ * newDimX * newDimY + newY * newDimX + newX] = pixelCount ? pixelSum / pixelCount : NAN;
            }
        }
    }
    *newDataPtr = reducedCube;
    return EXIT_SUCCESS;
}

int MaskCropAndDownsample(const int16_t *dataPtr, int16_t **newDataPtr, int64_t dimX, int64_t dimY, int64_t dimZ,
    int64_t cropX1, int64_t cropY1, int64_t cropZ1, int64_t cropX2, int64_t cropY2, int64_t cropZ2, int factorX, int factorY, int factorZ)
{
    if (cropX1 > dimX || cropX2 > dimX || cropY1 > dimY || cropY2 > dimY || cropZ1 > dimZ || cropZ2 > dimZ || cropX1 < 1 || cropX2 < 1 || cropY1 < 1 || cropY2 < 1 || cropZ1 < 1 || cropZ2 < 1)
        return EXIT_FAILURE;
    int64_t newDimX = (abs(cropX1 - cropX2) + 1) / factorX;
    int64_t newDimY = (abs(cropY1 - cropY2) + 1) / factorY;
    int64_t newDimZ = (abs(cropZ1 - cropZ2) + 1) / factorZ;
    if ((abs(cropX1 - cropX2) + 1) % factorX != 0)
        newDimX++;
    if ((abs(cropY1 - cropY2) + 1) % factorY != 0)
        newDimY++;
    if ((abs(cropZ1 - cropZ2) + 1) % factorZ != 0)
        newDimZ++;
    int64_t newSize = newDimX * newDimY * newDimZ;
    int16_t* reducedCube = new int16_t[newSize] {};
    int64_t smallX = min(cropX1, cropX2);
    int64_t smallY = min(cropY1, cropY2);
    int64_t smallZ = min(cropZ1, cropZ2);
#pragma omp parallel 
#pragma omp for
    for (auto newZ = 0; newZ < newDimZ; newZ++)
    {
        int64_t  oldZ, oldY, oldX;
        int16_t pixVal;
        size_t blockSizeX, blockSizeY, blockSizeZ;
        for (auto newY = 0; newY < newDimY; newY++)
        {
            for (auto newX = 0; newX < newDimX; newX++)
            {
                blockSizeX = factorX;
                blockSizeY = factorY;
                blockSizeZ = factorZ;
                if ((newX + 1) * factorX >= dimX) {
                    blockSizeX = dimX - (newX*factorX);
                }
                if ((newY + 1) * factorY >= dimY) {
                    blockSizeY = dimY - (newY*factorY);
                }
                if ((newZ + 1) * factorZ >= dimZ) {
                    blockSizeZ = dimZ - (newZ*factorZ);
                }
                for (auto pixelZ = 0; pixelZ < blockSizeZ; pixelZ++)
                {
                    oldZ = newZ * factorZ + pixelZ + smallZ - 1;
                    for (auto pixelY = 0; pixelY < blockSizeY; pixelY++)
                    {
                        oldY = newY * factorY + pixelY + smallY - 1;
                        for (auto pixelX = 0; pixelX < blockSizeX; pixelX++)
                        {
                            oldX = newX * factorX + pixelX + smallX - 1;
                            pixVal = dataPtr[oldZ * dimX * dimY + oldY * dimX + oldX];
                            if (pixVal != 0)
                                break;
                        }
                        if (pixVal != 0)
                            break;
                    }
                    if (pixVal != 0)
                        break;
                }
                reducedCube[newZ * newDimX * newDimY + newY * newDimX + newX] = pixVal;
            }
        }
    }
    *newDataPtr = reducedCube;
    return EXIT_SUCCESS;
}

int GetHistogram(const float* dataPtr, int64_t numElements, int numBins, float minVal, float maxVal, int** histogram)
{
    int* histogramArray = new int[numBins]();
    int* hist_private;
    #pragma omp parallel
    {
        const int nthreads = omp_get_num_threads();
        const int ithread = omp_get_thread_num();
        #pragma omp single
        {
            hist_private = new int[numBins * nthreads]();
        }
        #pragma omp for
        for (int64_t n = 0; n < numElements; ++n)
        {
            float dataValue = dataPtr[n];
            if (isnan(dataValue) || dataValue < minVal || dataValue > maxVal)
                continue;
            if (dataValue == maxVal)			//inclusive of max value for final bin
            {
                hist_private[ithread * numBins + numBins - 1]++;
            }
            else
            {
                int histogramIndex = floor(((double)dataPtr[n] - (double)minVal) * (double)numBins / ((double)maxVal - (double)minVal));
                hist_private[ithread * numBins + histogramIndex]++;
            }
        }
        #pragma omp for
        for (int i = 0; i < numBins; i++) {
            for (int t = 0; t < nthreads; t++) {
                histogramArray[i] += hist_private[numBins * t + i];
            }
        }
    }
    delete[] hist_private;
    *histogram = histogramArray;
    return EXIT_SUCCESS;
}

int GetMaskedSources(const int16_t* maskDataPtr, int64_t dimX, int64_t dimY, int64_t dimZ, int* maskCount, SourceInfo** results)
{
    unordered_map<int16_t, SourceInfo> sourceMap;
    for (int64_t k = 0; k < dimZ; k++) {
        for (int64_t j = 0; j < dimY; j++) {
            int64_t index = dimX * j + dimX * dimY * k;
            for (int64_t i = 0; i < dimX; i++) {
                auto maskVal = maskDataPtr[index];
                if (maskVal) {
                    if (!sourceMap.count(maskVal)) {
                        sourceMap[maskVal] = {i, i, j, j, k, k, maskVal};
                    } else {
                        auto& source = sourceMap.at(maskVal);
                        source.minX = min(source.minX, i);
                        source.maxX = max(source.maxX, i);
                        source.minY = min(source.minY, j);
                        source.maxY = max(source.maxY, j);
                        source.minZ = min(source.minZ, k);
                        source.maxZ = max(source.maxZ, k);
                    }
                }
                index++;
            }
        }
    }

    *maskCount = sourceMap.size();
    SourceInfo* sources = new SourceInfo[sourceMap.size()];
    int n = 0;
    for (auto const& [sourceValue, source] : sourceMap) {
        sources[n] = source;
        n++;
    }

    *results = sources;
    return EXIT_SUCCESS;
}

int GetSourceStats(const float* dataPtr, const int16_t* maskDataPtr, int64_t dimX, int64_t dimY, int64_t dimZ, SourceInfo source, SourceStats* stats)
{
    if (stats && source.minX >= 0 && source.maxX < dimX && source.minY >= 0 && source.maxY < dimY && source.minZ >= 0 && source.maxZ < dimZ)
    {
        double integratedFlux = 0.0;
        double sumX = 0.0;
        double sumY = 0.0;
        double sumZ = 0.0;

        double peakFlux = std::numeric_limits<double>::lowest();
        int64_t numVoxels = 0;
        for (int64_t k = source.minZ; k <= source.maxZ; k++)
        {
            for (int64_t j = source.minY; j <= source.maxY; j++)
            {
                for (int64_t i = source.minX; i <= source.maxX; i++)
                {
                    int64_t index = i + dimX * j + dimX * dimY * k;
                    auto maskVal = maskDataPtr[index];
                    if (maskVal == source.maskVal)
                    {
                        double flux = dataPtr[index];
                        if (isfinite(flux))
                        {
                            numVoxels++;
                            peakFlux = std::max(peakFlux, flux);
                            integratedFlux += flux;
                            source.minX = min(source.minX, i);
                            source.maxX = max(source.maxX, i);
                            source.minY = min(source.minY, j);
                            source.maxY = max(source.maxY, j);
                            source.minZ = min(source.minZ, k);
                            source.maxZ = max(source.maxZ, k);

                            sumX += i * flux;
                            sumY += j * flux;
                            sumZ += k * flux;
                        }
                    }
                }
            }
        }

        // Bounding box
        stats->minX = source.minX;
        stats->maxX = source.maxX;
        stats->minY = source.minY;
        stats->maxY = source.maxY;
        stats->minZ = source.minZ;
        stats->maxZ = source.maxZ;

        if (numVoxels)
        {
            stats->numVoxels = numVoxels;
            stats->peakFlux = peakFlux;
            stats->integratedFlux = integratedFlux;
            stats->cX = sumX / integratedFlux;
            stats->cY = sumY / integratedFlux;
            stats->cZ = sumZ / integratedFlux;
            return EXIT_SUCCESS;
        }
        else
        {
            stats->numVoxels = 0;
            stats->peakFlux = NAN;
            stats->integratedFlux = NAN;
            stats->cX = NAN;
            stats->cY = NAN;
            stats->cZ = NAN;
            return EXIT_FAILURE;
        }
    }
    return EXIT_FAILURE;
}

int FreeMemory(void* ptrToDelete)
{
    delete[] ptrToDelete;
    return EXIT_SUCCESS;
}

