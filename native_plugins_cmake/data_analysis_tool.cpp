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
#include "data_analysis_tool.h"
#include "cdl_zscale.h"

#include <unordered_map>
#include <limits>

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


int DataCropAndDownsample(const float* dataPtr, float** newDataPtr, int64_t dimX, int64_t dimY, int64_t dimZ, int64_t cropX1, int64_t cropY1, int64_t cropZ1, int64_t cropX2,
                          int64_t cropY2, int64_t cropZ2, int factorX, int factorY, int factorZ, bool maxDownsampling) {
    // Make use of templated function to allow constexpr branching in inner loops without reducing performance
    if (maxDownsampling) {
        return DataCropAndDownsample<true>(dataPtr, newDataPtr, dimX, dimY, dimZ, cropX1, cropY1, cropZ1, cropX2, cropY2, cropZ2, factorX, factorY, factorZ);
    } else {
        return DataCropAndDownsample<false>(dataPtr, newDataPtr, dimX, dimY, dimZ, cropX1, cropY1, cropZ1, cropX2, cropY2, cropZ2, factorX, factorY, factorZ);
    }
}

template<bool maxMode>
int DataCropAndDownsample(const float* dataPtr, float** newDataPtr, const int64_t dimX, const int64_t dimY, const int64_t dimZ, const int64_t cropX1, const int64_t cropY1,
                          const int64_t cropZ1, const int64_t cropX2, const int64_t cropY2, const int64_t cropZ2, const int factorX, const int factorY, const int factorZ)
{
    if (cropX1 > dimX || cropX2 > dimX || cropY1 > dimY || cropY2 > dimY || cropZ1 > dimZ || cropZ2 > dimZ || cropX1 < 1 || cropX2 < 1 || cropY1 < 1 || cropY2 < 1 || cropZ1 < 1 ||
        cropZ2 < 1)
    {
        return EXIT_FAILURE;
    }

    int64_t newDimX = (abs(cropX1 - cropX2) + 1) / factorX;
    int64_t newDimY = (abs(cropY1 - cropY2) + 1) / factorY;
    int64_t newDimZ = (abs(cropZ1 - cropZ2) + 1) / factorZ;
    if ((abs(cropX1 - cropX2) + 1) % factorX != 0)
        newDimX++;
    if ((abs(cropY1 - cropY2) + 1) % factorY != 0)
        newDimY++;
    if ((abs(cropZ1 - cropZ2) + 1) % factorZ != 0)
        newDimZ++;

    const int64_t newSize = newDimX * newDimY * newDimZ;
    float* reducedCube = new float[newSize]{};
    const int64_t smallX = min(cropX1, cropX2);
    const int64_t smallY = min(cropY1, cropY2);
    const int64_t smallZ = min(cropZ1, cropZ2);

#pragma omp parallel for
    for (auto newZ = 0; newZ < newDimZ; newZ++)
    {
        int64_t oldZ, oldY, oldX;
        int pixelCount;
        float pixelAccumulation;
        size_t blockSizeX, blockSizeY, blockSizeZ;
        for (auto newY = 0; newY < newDimY; newY++)
        {
            for (auto newX = 0; newX < newDimX; newX++)
            {
                if constexpr(maxMode)
                {
                    pixelAccumulation = -std::numeric_limits<float>::max();
                }
                else
                {
                    pixelAccumulation = 0;
                }
                pixelCount = 0;
                blockSizeX = factorX;
                blockSizeY = factorY;
                blockSizeZ = factorZ;
                if ((newX + 1) * factorX >= dimX)
                {
                    blockSizeX = dimX - (newX * factorX);
                }
                if ((newY + 1) * factorY >= dimY)
                {
                    blockSizeY = dimY - (newY * factorY);
                }
                if ((newZ + 1) * factorZ >= dimZ)
                {
                    blockSizeZ = dimZ - (newZ * factorZ);
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
                            auto pixVal = dataPtr[oldZ * dimX * dimY + oldY * dimX + oldX];
                            if (!isnan(pixVal))
                            {
                                pixelCount++;
                                if constexpr(maxMode)
                                {
                                    if (pixVal > pixelAccumulation)
                                    {
                                        pixelAccumulation = pixVal;
                                    }
                                }
                                else
                                {
                                    pixelAccumulation += pixVal;
                                }
                            }
                        }
                    }
                }
                auto index = newZ * newDimX * newDimY + newY * newDimX + newX;
                if (pixelCount)
                {
                    if constexpr(maxMode)
                    {
                        reducedCube[index] = pixelAccumulation;
                    }
                    else
                    {
                        reducedCube[index] = pixelAccumulation / (float) pixelCount;
                    }
                }
                else
                {
                    reducedCube[index] = NAN;
                }
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

//Function to more quickly get estimated values of given percentiles in the data from the histogram
int GetPercentileValuesFromHistogram(const int* histogram, int numBins, float minVal, float maxVal, float minPercentile, float maxPercentile, float* minPercentileValue, float* maxPercentileValue)
{
        const float binWidth = (maxVal - minVal) / numBins;
        std::vector<float> remainingRanks = {minPercentile, maxPercentile};
        int cumulativeSum = 0;

        int totalSum = 0;
        for (int i = 0; i < numBins; i++) {
            totalSum += histogram[i];
        }

        if (totalSum == 0) {
            return EXIT_FAILURE;
        }

        std::vector<float> calculatedPercentiles;
        for (int i = 0; i < numBins && !remainingRanks.empty(); i++) {
            float currentFraction = static_cast<float>(cumulativeSum) / totalSum;
            float nextFraction = static_cast<float>(cumulativeSum + histogram[i]) / totalSum;
            float nextRank = remainingRanks[0] / 100.0f;

            while (nextFraction >= nextRank && !remainingRanks.empty()) {
                float portion = (nextRank - currentFraction) / (nextFraction - currentFraction);
                calculatedPercentiles.push_back(minVal + binWidth * (i + portion));
                remainingRanks.erase(remainingRanks.begin());
                if (!remainingRanks.empty()) {
                    nextRank = remainingRanks[0] / 100.0f;
                }
            }
            cumulativeSum += histogram[i];
        }
        if (calculatedPercentiles.size() != 2) {
            return EXIT_FAILURE;
        }
        *minPercentileValue = calculatedPercentiles[0];
        *maxPercentileValue = calculatedPercentiles[1];
        return EXIT_SUCCESS;
    }

//Function to get values of given percentiles in the float array of data
int GetPercentileValuesFromData(const float* data, int64_t size, float minPercentile, float maxPercentile, float* minPercentileValue, float* maxPercentileValue) {
    std::vector<float> sortedData(data, data + size);

            std::sort(sortedData.begin(), sortedData.end());

    int minIndex = static_cast<int>(minPercentile * (sortedData.size() - 1) / 100.0);
    int maxIndex = static_cast<int>(maxPercentile * (sortedData.size() - 1) / 100.0);

    *minPercentileValue = sortedData[minIndex];
    *maxPercentileValue = sortedData[maxIndex];
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

int GetSourceStats(const float* dataPtr, const int16_t* maskDataPtr, int64_t dimX, int64_t dimY, int64_t dimZ, SourceInfo source, SourceStats* stats, AstFrameSet* frameSetPtr)
{
    if (stats && source.minX >= 0 && source.maxX < dimX && source.minY >= 0 && source.maxY < dimY && source.minZ >= 0 && source.maxZ < dimZ)
    {
        double totalFlux = 0.0;
        double sumX = 0.0;
        double sumY = 0.0;
        double sumZ = 0.0;
        double totalPositiveFlux = 0.0;
        //Arrays for ast velocity conversion
        double xInL[3];
        double xInR[3];
        double vOutL[3];
        double vOutR[3];

        double peakFlux = std::numeric_limits<double>::lowest();
        int64_t numVoxels = 0;

        int64_t numChannels = source.maxZ - source.minZ + 1;
        
        if (stats->spectralProfilePtr != nullptr) {
            // Clear memory from previous spectral profile calculations
            delete[] stats->spectralProfilePtr;
        }
        stats->spectralProfilePtr = new double[numChannels];

        stats->minX = source.maxX;
        stats->minY = source.maxY;
        stats->minZ = source.maxZ;

        stats->maxX = source.minX;
        stats->maxY = source.minY;
        stats->maxZ = source.minZ;


        for (int64_t k = source.minZ; k <= source.maxZ; k++)
        {
            double spectralSum = 0.0;
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
                            totalFlux += flux;
                            stats->minX = min(stats->minX, i);
                            stats->maxX = max(stats->maxX, i);
                            stats->minY = min(stats->minY, j);
                            stats->maxY = max(stats->maxY, j);
                            stats->minZ = min(stats->minZ, k);
                            stats->maxZ = max(stats->maxZ, k);

                            if (flux >= 0)
                            {
                              totalPositiveFlux += flux;
                              sumX += i * flux;
                              sumY += j * flux;
                              sumZ += k * flux;
                            }

                            spectralSum += flux;
                        }
                    }
                }
            }
            stats->spectralProfilePtr[k - source.minZ] = spectralSum;
        }

        if (numVoxels)
        {
            stats->numVoxels = numVoxels;
            stats->peak = peakFlux;
            stats->sum = totalFlux;
            stats->cX = sumX / totalPositiveFlux;
            stats->cY = sumY / totalPositiveFlux;
            stats->cZ = sumZ / totalPositiveFlux;

            // Find peak value
            double spectralPeak = std::numeric_limits<double>::lowest();

            for (auto i = 0; i < numChannels; i++)
            {
                auto val = stats->spectralProfilePtr[i];
                if (isfinite(val) && val > spectralPeak)
                {
                    spectralPeak = val;
                }
            }

            double leftChannel = 0;
            double rightChannel = 0;
            double w20Threshold = spectralPeak * 0.2;
            bool leftChannelFound = false;
            bool rightChannelFound = false;

            for (auto i = 0; i < numChannels - 1; i++)
            {
                auto y0 = stats->spectralProfilePtr[i];
                auto y1 = stats->spectralProfilePtr[i+1];
                if (y0 < w20Threshold && y1 >= w20Threshold) {
                    //leftChannel = source.minZ + i + 0.5;
                    leftChannel = source.minZ + i + (w20Threshold - y0) / (y1 - y0);
                    leftChannelFound = true;
                    break;
                }
            }

            for (auto i = numChannels - 2; i >= 0; i--)
            {
                auto y0 = stats->spectralProfilePtr[i];
                auto y1 = stats->spectralProfilePtr[i+1];
                if (y0 >= w20Threshold && y1 < w20Threshold) {
                    rightChannel = source.minZ + i + (w20Threshold - y0) / (y1 - y0);
                    rightChannelFound = true;
                    break;
                }
            }

            if (leftChannelFound && rightChannelFound)
            {
                stats->channelVsys = (leftChannel + rightChannel) / 2.0;
                stats->channelW20 = rightChannel - leftChannel;
                if (frameSetPtr != nullptr)
                {
                    xInL[0] = 1;
                    xInL[1] = 1;
                    xInL[2] = leftChannel;
                    xInR[0] = 1;
                    xInR[1] = 1;
                    xInR[2] = rightChannel;
                    astTranN(frameSetPtr, 1, 3, 1, xInL, 1, 3, 1, vOutL);
                    astTranN(frameSetPtr, 1, 3, 1, xInR, 1, 3, 1, vOutR);
                    stats->veloVsys = (vOutL[2] + vOutR[2]) / 2.0;
                    stats->veloW20 = abs(vOutR[2] - vOutL[2]);
                }
                else
                {
                    stats->veloVsys = NAN;
                    stats->veloW20 = NAN;
                }
            }
            else
            {
                stats->channelVsys = NAN;
                stats->channelW20 = NAN;
                stats->veloVsys = NAN;
                stats->veloW20 = NAN;
            }
            stats->spectralProfileSize = numChannels;
            return EXIT_SUCCESS;
        }
        else
        {
            stats->numVoxels = 0;
            stats->peak = NAN;
            stats->sum = NAN;
            stats->cX = NAN;
            stats->cY = NAN;
            stats->cZ = NAN;
            stats->channelVsys = NAN;
            stats->channelW20 = NAN;
            return EXIT_FAILURE;
        }
    }
    return EXIT_FAILURE;
}

int GetZScale(const float* data, int64_t width, int64_t height, float* z1, float* z2)
{
    cdl_zscale((unsigned char*)data, width, height, -32, z1, z2, 0.25, 600, 120);
    return EXIT_SUCCESS;
}

int FreeDataAnalysisMemory(void* ptrToDelete)
{
    delete[] ptrToDelete;
    return EXIT_SUCCESS;
}

