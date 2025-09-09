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

/**
 * @brief Computes the maximum and minimum values in a given array of floats.
 *
 * This function scans through an array of floating-point numbers and determines 
 * the maximum and minimum values. The computation is parallelised using OpenMP 
 * to improve performance on large datasets.
 *
 * @param dataPtr Pointer to the array of float values to analyze.
 * @param numberElements The number of elements in the data array.
 * @param maxResult Pointer to a float where the maximum value will be stored.
 * @param minResult Pointer to a float where the minimum value will be stored.
 *
 * @return EXIT_SUCCESS on successful completion.
 *
 * @note This function uses OpenMP for parallel execution. Ensure OpenMP is enabled 
 *       during compilation (e.g., with `-fopenmp` for GCC/Clang).
 * @warning The pointers @p maxResult and @p minResult must not be null.
 */
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

/**
 * @brief Computes basic statistics (maximum, minimum, mean, and standard deviation) for an array of floats.
 *
 * This function calculates the maximum, minimum, mean, and standard deviation of the values
 * in the given float array. It ignores NaN values during statistical computation. The computation
 * is parallelized using OpenMP to improve performance for large arrays.
 *
 * @param dataPtr Pointer to the array of float values to analyze.
 * @param numberElements The number of elements in the data array.
 * @param maxResult Pointer to a float where the maximum value will be stored.
 * @param minResult Pointer to a float where the minimum value will be stored.
 * @param meanResult Pointer to a float where the mean value will be stored.
 * @param stdDevResult Pointer to a float where the standard deviation will be stored.
 *
 * @return EXIT_SUCCESS on successful completion.
 *
 * @note NaN values in the input array are ignored during mean, standard deviation,
 *       min, and max calculations.
 * @note This function uses OpenMP. Ensure OpenMP is enabled during compilation (e.g., with `-fopenmp`).
 *
 * @warning The pointers @p maxResult, @p minResult, @p meanResult, and @p stdDevResult
 *          must not be null.
 */
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

/**
 * @brief Retrieves the value of a voxel at the given (x, y, z) coordinate from a 3D float array.
 *
 * This function calculates the 1D index for a 3D voxel coordinate in a flattened array
 * stored in z-major order (z, then y, then x), and retrieves the corresponding float value.
 * The coordinate system is assumed to be 1-based (i.e., (1,1,1) is the first voxel).
 *
 * @param dataPtr Pointer to the flattened 3D float array (size: xDim * yDim * zDim).
 * @param voxelValue Pointer to a float where the retrieved voxel value will be stored.
 * @param xDim Size of the X dimension.
 * @param yDim Size of the Y dimension.
 * @param zDim Size of the Z dimension.
 * @param x X coordinate (1-based).
 * @param y Y coordinate (1-based).
 * @param z Z coordinate (1-based).
 *
 * @return EXIT_SUCCESS if the voxel value was successfully retrieved;
 *         EXIT_FAILURE if the coordinates are out of bounds.
 *
 * @warning The pointer @p voxelValue must not be null.
 * @note Coordinates are expected to be in the range [1, xDim], [1, yDim], [1, zDim].
 */

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

/**
 * @brief Retrieves the value of a voxel at the given (x, y, z) coordinate from a 3D int16_t array.
 *
 * This function computes the 1D index corresponding to the specified 3D voxel position 
 * in a flattened array (z-major order: z → y → x), and retrieves the corresponding int16_t value.
 * The coordinate system is assumed to be 1-based (i.e., (1,1,1) is the first voxel).
 *
 * @param dataPtr Pointer to the flattened 3D int16_t array (size: xDim * yDim * zDim).
 * @param voxelValue Pointer to an int16_t where the retrieved voxel value will be stored.
 * @param xDim Size of the X dimension.
 * @param yDim Size of the Y dimension.
 * @param zDim Size of the Z dimension.
 * @param x X coordinate (1-based).
 * @param y Y coordinate (1-based).
 * @param z Z coordinate (1-based).
 *
 * @return EXIT_SUCCESS if the voxel value was successfully retrieved;
 *         EXIT_FAILURE if the coordinates are out of bounds.
 *
 * @warning The pointer @p voxelValue must not be null.
 * @note Coordinates must be in the range [1, xDim], [1, yDim], [1, zDim].
 */
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

/**
 * @brief Extracts a 1D profile along the X dimension at the specified Y and Z coordinates.
 *
 * This function allocates and returns a new float array containing values along the X-axis
 * for the given Y and Z indices in a flattened 3D volume stored in z-major order.
 * The coordinate system is 1-based (i.e., valid indices start at 1).
 *
 * @param dataPtr Pointer to the flattened 3D float array (size: xDim * yDim * zDim).
 * @param profile Output pointer to a float array that will hold the extracted profile.
 *                The caller is responsible for deleting this allocated array.
 * @param xDim Size of the X dimension.
 * @param yDim Size of the Y dimension.
 * @param zDim Size of the Z dimension.
 * @param y Y coordinate (1-based).
 * @param z Z coordinate (1-based).
 *
 * @return EXIT_SUCCESS if the profile was successfully extracted;
 *         EXIT_FAILURE if the Y or Z coordinates are out of bounds.
 *
 * @warning The pointer @p profile must not be null.
 * @note Coordinates must be in the range [1, yDim] and [1, zDim].
 */
int GetXProfile(const float *dataPtr, float **profile, int64_t xDim, int64_t yDim, int64_t zDim, int64_t y, int64_t z)
{
    if (y > yDim || z > zDim || y < 1 || z < 1)
        return EXIT_FAILURE;
    float* newProfile = new float[xDim];
    for (int64_t i = 0; i < xDim; i++)
    {
        int64_t index = (z - 1) * xDim * yDim + (y - 1) * xDim + i;
        newProfile[i] = dataPtr[index];
    }
    *profile = newProfile;
    return EXIT_SUCCESS;
}

/**
 * @brief Extracts a 1D profile along the Y dimension at the specified X and Z coordinates.
 *
 * This function allocates and returns a new float array containing values along the Y-axis
 * for the given X and Z indices in a flattened 3D volume stored in z-major order 
 * (i.e., Z → Y → X). The coordinate system is 1-based.
 *
 * @param dataPtr Pointer to the flattened 3D float array (size: xDim * yDim * zDim).
 * @param profile Output pointer to a float array that will hold the extracted profile.
 *                The caller is responsible for deleting this allocated array.
 * @param xDim Size of the X dimension.
 * @param yDim Size of the Y dimension.
 * @param zDim Size of the Z dimension.
 * @param x X coordinate (1-based).
 * @param z Z coordinate (1-based).
 *
 * @return EXIT_SUCCESS if the profile was successfully extracted;
 *         EXIT_FAILURE if the X or Z coordinates are out of bounds.
 *
 * @warning The pointer @p profile must not be null.
 * @note Coordinates must be in the range [1, xDim] and [1, zDim].
 */

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


/**
 * @brief Extracts a 1D profile along the Z dimension at the specified X and Y coordinates.
 *
 * This function allocates and returns a new float array containing values along the Z-axis
 * for the given X and Y indices in a flattened 3D volume stored in z-major order
 * (i.e., Z → Y → X). The coordinate system is 1-based.
 *
 * @param dataPtr Pointer to the flattened 3D float array (size: xDim * yDim * zDim).
 * @param profile Output pointer to a float array that will hold the extracted profile.
 *                The caller is responsible for deleting this allocated array.
 * @param xDim Size of the X dimension.
 * @param yDim Size of the Y dimension.
 * @param zDim Size of the Z dimension.
 * @param x X coordinate (1-based).
 * @param y Y coordinate (1-based).
 *
 * @return EXIT_SUCCESS if the profile was successfully extracted;
 *         EXIT_FAILURE if the X or Y coordinates are out of bounds.
 *
 * @warning The pointer @p profile must not be null.
 * @note Coordinates must be in the range [1, xDim] and [1, yDim].
 */
int GetZProfile(const float *dataPtr, float **profile, int64_t xDim, int64_t yDim, int64_t zDim, int64_t x, int64_t y)
{
    float* newProfile = new float[zDim];
    if (x > xDim || y > yDim || x < 1 || y < 1)
        return EXIT_FAILURE;
    for (int i = 0; i < zDim; i++)
        newProfile[i] = dataPtr[i * xDim * yDim + (y - 1) * xDim + (x - 1)];
    *profile = newProfile;
    return EXIT_SUCCESS;
}


/**
 * @brief Crops and downsamples a 3D float volume using either average or max pooling.
 * 
 * This is a wrapper function that selects the appropriate template instantiation
 * based on the maxDownsampling flag. The function performs spatial cropping and
 * downsampling on the input 3D volume and allocates a new downsampled data array.
 * 
 * @param dataPtr Pointer to the input 3D float volume (flattened in Z-Y-X order).
 * @param newDataPtr Output pointer to hold the resulting downsampled 3D volume.
 * @param dimX Original X-dimension of the input volume.
 * @param dimY Original Y-dimension of the input volume.
 * @param dimZ Original Z-dimension of the input volume.
 * @param cropX1 First X-coordinate for the cropping region (1-based index).
 * @param cropY1 First Y-coordinate for the cropping region (1-based index).
 * @param cropZ1 First Z-coordinate for the cropping region (1-based index).
 * @param cropX2 Second X-coordinate for the cropping region (1-based index).
 * @param cropY2 Second Y-coordinate for the cropping region (1-based index).
 * @param cropZ2 Second Z-coordinate for the cropping region (1-based index).
 * @param factorX Downsampling factor in the X direction.
 * @param factorY Downsampling factor in the Y direction.
 * @param factorZ Downsampling factor in the Z direction.
 * @param maxDownsampling If true, performs max pooling instead of average pooling.
 * 
 * @return int EXIT_SUCCESS on success, or EXIT_FAILURE on invalid input.
*/
int DataCropAndDownsample(const float* dataPtr, float** newDataPtr, int64_t dimX, int64_t dimY, int64_t dimZ, int64_t cropX1, int64_t cropY1, int64_t cropZ1, int64_t cropX2,
                          int64_t cropY2, int64_t cropZ2, int factorX, int factorY, int factorZ, bool maxDownsampling) {
    // Make use of templated function to allow constexpr branching in inner loops without reducing performance
    if (maxDownsampling) {
        return DataCropAndDownsample<true>(dataPtr, newDataPtr, dimX, dimY, dimZ, cropX1, cropY1, cropZ1, cropX2, cropY2, cropZ2, factorX, factorY, factorZ);
    } else {
        return DataCropAndDownsample<false>(dataPtr, newDataPtr, dimX, dimY, dimZ, cropX1, cropY1, cropZ1, cropX2, cropY2, cropZ2, factorX, factorY, factorZ);
    }
}

/**
 * @brief Internal templated function that crops and downsamples a 3D float volume.
 * 
 * Depending on the template parameter maxMode, this function performs either
 * average pooling (when false) or max pooling (when true) on the cropped 3D region.
 * The function handles partial blocks at the volume edges and skips NaN values
 * during accumulation.
 * 
 * @tparam maxMode If true, uses max pooling; otherwise, uses average pooling.
 * 
 * @param dataPtr Pointer to the input 3D float volume (flattened in Z-Y-X order).
 * @param newDataPtr Output pointer to hold the resulting downsampled 3D volume.
 * @param dimX Original X-dimension of the input volume.
 * @param dimY Original Y-dimension of the input volume.
 * @param dimZ Original Z-dimension of the input volume.
 * @param cropX1 First X-coordinate for the cropping region (1-based index).
 * @param cropY1 First Y-coordinate for the cropping region (1-based index).
 * @param cropZ1 First Z-coordinate for the cropping region (1-based index).
 * @param cropX2 Second X-coordinate for the cropping region (1-based index).
 * @param cropY2 Second Y-coordinate for the cropping region (1-based index).
 * @param cropZ2 Second Z-coordinate for the cropping region (1-based index).
 * @param factorX Downsampling factor in the X direction.
 * @param factorY Downsampling factor in the Y direction.
 * @param factorZ Downsampling factor in the Z direction.
 * 
 * @return int EXIT_SUCCESS on success, or EXIT_FAILURE on invalid cropping parameters.
 * 
 * @note Memory for the output is allocated internally and must be freed by the caller.
 * @note Uses OpenMP to parallelize over the Z-dimension for performance.
 * @note If all values in a downsampling block are NaN, the result will be NaN.
 */
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

/**
 * @brief Crops and downsamples a 3D int16_t mask volume, preserving the presence of non-zero values.
 *
 * This function extracts a cropped region from a 3D int16_t input volume and downsamples it using
 * a presence-based strategy: for each downsampling block, if any value in the block is non-zero,
 * the output voxel is set to that non-zero value (first encountered); otherwise, it is zero.
 *
 * The input volume is assumed to be in Z-Y-X flattened order.
 *
 * @param dataPtr Pointer to the input 3D volume (flattened Z-Y-X).
 * @param newDataPtr Output pointer to the downsampled 3D volume. Memory is allocated within the function.
 * @param dimX Original X-dimension size.
 * @param dimY Original Y-dimension size.
 * @param dimZ Original Z-dimension size.
 * @param cropX1 First X-coordinate of the crop region (1-based).
 * @param cropY1 First Y-coordinate of the crop region (1-based).
 * @param cropZ1 First Z-coordinate of the crop region (1-based).
 * @param cropX2 Second X-coordinate of the crop region (1-based).
 * @param cropY2 Second Y-coordinate of the crop region (1-based).
 * @param cropZ2 Second Z-coordinate of the crop region (1-based).
 * @param factorX Downsampling factor in the X dimension.
 * @param factorY Downsampling factor in the Y dimension.
 * @param factorZ Downsampling factor in the Z dimension.
 * @return int Returns EXIT_SUCCESS on success, or EXIT_FAILURE if crop bounds are invalid.
 *
 * @note Output memory must be freed by the caller.
 * @note Uses OpenMP to parallelize processing over the Z-dimension for performance.
 * @note The function ensures that partial downsampling blocks at the crop edges are handled safely.
 */
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

/**
 * @brief Estimates the values at given percentiles from a histogram.
 *
 * This function computes approximate values corresponding to specified percentiles
 * (e.g., 2nd and 98th) using a histogram representation of the data. It assumes a
 * uniform bin width between a known minimum and maximum value.
 *
 * @param histogram Pointer to the array containing histogram counts.
 * @param numBins Number of bins in the histogram.
 * @param minVal Minimum data value represented by the histogram.
 * @param maxVal Maximum data value represented by the histogram.
 * @param minPercentile The lower percentile to calculate (e.g., 2.0 for 2nd percentile).
 * @param maxPercentile The upper percentile to calculate (e.g., 98.0 for 98th percentile).
 * @param minPercentileValue Output pointer to store the estimated value at minPercentile.
 * @param maxPercentileValue Output pointer to store the estimated value at maxPercentile.
 * @return int Returns EXIT_SUCCESS on success, or EXIT_FAILURE if the histogram is empty
 *             or percentile calculation fails.
 *
 * @note Percentiles must be between 0 and 100. The function will return failure if
 *       the histogram contains no data or if the desired percentiles could not be
 *       computed (e.g., due to numerical instability).
 * 
 * @note The returned percentile values are interpolated within histogram bins for better accuracy.
 */
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

/**
 * @brief Computes the values at specified percentiles from a float array of data.
 *
 * This function calculates the values corresponding to given percentiles (e.g., 2nd and 98th)
 * by sorting a copy of the input data and selecting the appropriate ranked elements.
 *
 * @param data Pointer to the input array of floating-point values.
 * @param size Number of elements in the input data array.
 * @param minPercentile The lower percentile to compute (e.g., 2.0 for the 2nd percentile).
 * @param maxPercentile The upper percentile to compute (e.g., 98.0 for the 98th percentile).
 * @param minPercentileValue Output pointer to store the value at minPercentile.
 * @param maxPercentileValue Output pointer to store the value at maxPercentile.
 * @return int Returns EXIT_SUCCESS upon success.
 *
 * @note Percentile values should be in the range [0, 100]. The function does not check bounds,
 *       so values outside this range may lead to undefined behavior.
 *
 * @warning This function creates a sorted copy of the input data, which requires additional memory.
 *          It performs an in-place sort, so performance may be affected for large datasets.
 */

int GetPercentileValuesFromData(const float* data, int64_t size, float minPercentile, float maxPercentile, float* minPercentileValue, float* maxPercentileValue) {
    std::vector<float> sortedData(data, data + size);

            std::sort(sortedData.begin(), sortedData.end());

    int minIndex = static_cast<int>(minPercentile * (sortedData.size() - 1) / 100.0);
    int maxIndex = static_cast<int>(maxPercentile * (sortedData.size() - 1) / 100.0);

    *minPercentileValue = sortedData[minIndex];
    *maxPercentileValue = sortedData[maxIndex];
    return EXIT_SUCCESS;
}

/**
 * @brief Computes a histogram from a given array of float data.
 *
 * This function calculates the frequency distribution of `dataPtr` into a specified number of bins
 * between `minVal` and `maxVal`, inclusive. The result is written to a dynamically allocated
 * histogram array, which must be freed by the caller. NaN values and values outside the [minVal, maxVal]
 * range are ignored. The last bin includes values equal to `maxVal`.
 *
 * @param dataPtr Pointer to the input data array.
 * @param numElements Number of elements in the input data array.
 * @param numBins Number of bins in the output histogram.
 * @param minVal Minimum value to include in the histogram range.
 * @param maxVal Maximum value to include in the histogram range.
 * @param histogram Output pointer to the resulting histogram array (size `numBins`).
 * @return int Returns EXIT_SUCCESS if the histogram is successfully computed.
 *
 * @note The histogram array is allocated inside the function. The caller is responsible
 *       for deleting it using `delete[]`.
 *
 * @warning If `minVal` == `maxVal`, or if `numBins <= 0`, the behavior is undefined.
 * @warning This function uses OpenMP for parallel computation. Ensure OpenMP is enabled during compilation.
 */
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

/**
 * @brief Identifies distinct non-zero mask values in a 3D volume and extracts their bounding boxes.
 *
 * Iterates over a 3D mask array and collects metadata (min/max X, Y, Z extents) for each unique
 * non-zero mask value. The output is an array of `SourceInfo` structures, one per distinct source ID.
 *
 * @param maskDataPtr Pointer to the 3D mask data (flattened 1D array of size dimX * dimY * dimZ).
 * @param dimX The size of the X dimension.
 * @param dimY The size of the Y dimension.
 * @param dimZ The size of the Z dimension.
 * @param maskCount Output pointer to the number of unique non-zero mask values found.
 * @param results Output pointer to an array of `SourceInfo` structures (allocated internally).
 *                Caller is responsible for freeing this array with `delete[]`.
 * @return int Returns `EXIT_SUCCESS` if processing completes successfully.
 *
 * @note The `SourceInfo` struct is assumed to include:
 *       - `minX`, `maxX`: range in X dimension
 *       - `minY`, `maxY`: range in Y dimension
 *       - `minZ`, `maxZ`: range in Z dimension
 *       - `sourceValue`: the unique mask ID
 *
 * @warning The function assumes that the input data is ordered in X-fastest format
 *          (i.e., [x + dimX * (y + dimY * z)]).
 * @warning Mask values of 0 are ignored.
 */
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

/**
 * @brief Computes statistical and spectral properties of a 3D source region in volumetric data.
 *
 * This function calculates flux statistics and spectral information for a source defined by a
 * `SourceInfo` struct within a 3D data cube. It supports optional spectral coordinate conversion
 * using an AST FrameSet.
 *
 * The following statistics are computed and stored in the provided `SourceStats` struct:
 * - Total flux (sum of finite values) (does not account for beam size)
 * - Number of contributing voxels
 * - Peak flux (does not account for beam size)
 * - Flux-weighted centroid (only using positive flux)
 * - Spatial bounding box (min/max X/Y/Z)
 * - Spectral profile (flux summed per channel Z)
 * - Spectral line properties: central velocity and W20 width (in channel and optionally velocity units)
 *
 * @param[in]  dataPtr        Pointer to 3D float array of intensity/flux values (size: dimX * dimY * dimZ)
 * @param[in]  maskDataPtr    Pointer to 3D int16_t array of mask values (same dimensions as dataPtr)
 * @param[in]  dimX           Size of the X dimension of the cube
 * @param[in]  dimY           Size of the Y dimension of the cube
 * @param[in]  dimZ           Size of the Z dimension (e.g. spectral axis)
 * @param[in]  source         A `SourceInfo` struct specifying the region of interest and mask value
 * @param[out] stats          Pointer to a `SourceStats` struct where the results will be stored
 * @param[in]  frameSetPtr    Optional pointer to an AST FrameSet for spectral coordinate transformation (may be NULL)
 *
 * @return `EXIT_SUCCESS` (0) if statistics were computed successfully, 
 *          or `EXIT_FAILURE` (1) if no voxels were found or inputs were invalid.
 *
 * @note The function dynamically allocates memory for `stats->spectralProfilePtr`.
 *       Any previous allocation is deleted.
 *       The caller is responsible for eventually freeing this memory.
 */

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
        stats->beamUnit = "";


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
            stats->beamUnit = "JY/BEAM";
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
            stats->beamUnit = "";
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

/**
 * @brief Computes the z-scale (contrast stretch) limits for an image using the cdl_zscale algorithm.
 *
 * This function calculates suitable lower and upper limits (`z1` and `z2`) for scaling image intensity values,
 * commonly used for display contrast adjustment, based on the input float image data.
 *
 * @param data Pointer to the image data array of floats (assumed contiguous, size = width * height).
 * @param width Width of the image in pixels.
 * @param height Height of the image in pixels.
 * @param[out] z1 Pointer to store the computed lower z-scale limit.
 * @param[out] z2 Pointer to store the computed upper z-scale limit.
 * 
 * @return int Returns EXIT_SUCCESS upon completion.
 *
 * @note Internally, the data pointer is cast to unsigned char* for use with `cdl_zscale`.
 *       Ensure `cdl_zscale` is compatible with this cast or adjust accordingly.
 * @note The parameters `-32`, `0.25`, `600`, and `120` are fixed arguments to `cdl_zscale` controlling its behavior.
 */
int GetZScale(const float* data, int64_t width, int64_t height, float* z1, float* z2)
{
    cdl_zscale((unsigned char*)data, width, height, -32, z1, z2, 0.25, 600, 120);
    return EXIT_SUCCESS;
}

/**
 * @brief Frees dynamically allocated array memory.
 *
 * This function deletes a memory block allocated with `new[]`. The pointer
 * must point to memory allocated as an array; otherwise, behavior is undefined.
 *
 * @param ptrToDelete Pointer to the memory block to be freed.
 * 
 * @return int Returns `EXIT_SUCCESS` upon successful deletion.
 *
 * @note The pointer must not be `nullptr` or already deleted.
 * @warning This function assumes the pointer was allocated with `new[]`.
 *          Using it on memory allocated differently (e.g., malloc) is unsafe.
 */
int FreeDataAnalysisMemory(void* ptrToDelete)
{
    delete[] ptrToDelete;
    return EXIT_SUCCESS;
}

