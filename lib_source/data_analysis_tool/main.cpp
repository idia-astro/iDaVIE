#include <iostream>
#include <vector>
#include <math.h>
#include <omp.h>

#define DllExport __declspec (dllexport)

#define EXIT_SUCCESS 0
#define EXIT_FAILURE 1


extern "C"
{
	DllExport int FindMaxMin(float *dataPtr, int64_t numberElements, float *maxResult, float *minResult)
	{
		float maxVal = -std::numeric_limits<float>::max();
		float minVal = std::numeric_limits<float>::max();
		#pragma omp parallel 
		{
			float currentMax = -std::numeric_limits<float>::max();
			float currentMin = std::numeric_limits<float>::max();
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

	DllExport int GetVoxelValue(float *dataPtr, float *voxelValue, int64_t xDim, int64_t yDim, int64_t zDim, int64_t x, int64_t y, int64_t z)
	{
		float outValue;
		if (x > xDim || y > yDim || z > zDim || x < 1 || y < 1 || z < 1)
			return EXIT_FAILURE;
		int64_t index = xDim * yDim * (z - 1) + xDim * (y - 1) + (x - 1);
		outValue = dataPtr[index];
		*voxelValue = outValue;
		return EXIT_SUCCESS;
	}
	
	//TODO make compatible with int64_t
	DllExport int GetXProfile(float *dataPtr, float **profile, int64_t xDim, int64_t yDim, int64_t zDim, int64_t y, int64_t z)
	{
		float* newProfile = new float[xDim];
		if (y > yDim || z > zDim || y < 1 || z < 1)
			return EXIT_FAILURE;
		for (int i = 0; i < xDim; i++)
			newProfile[i] = dataPtr[(z - 1) * xDim * yDim + (y - 1) * xDim + i];
		*profile = newProfile;
		return EXIT_SUCCESS;
	}

	DllExport int GetYProfile(float *dataPtr, float **profile, int64_t xDim, int64_t yDim, int64_t zDim, int64_t x, int64_t z)
	{
		float* newProfile = new float[yDim];
		if (x > xDim || z > zDim || x < 1 || z < 1)
			return EXIT_FAILURE;
		for (int i = 0; i < yDim; i++)
			newProfile[i] = dataPtr[(z - 1) * xDim * yDim + yDim * i + (x - 1)];
		*profile = newProfile;
		return EXIT_SUCCESS;

	}

	DllExport int GetZProfile(float *dataPtr, float **profile, int64_t xDim, int64_t yDim, int64_t zDim, int64_t x, int64_t y)
	{
		float* newProfile = new float[xDim];
		if (x > xDim || y > yDim || x < 1 || y < 1)
			return EXIT_FAILURE;
		for (int i = 0; i < zDim; i++)
			newProfile[i] = dataPtr[i * xDim * yDim + (y - 1) * xDim + (x - 1)];
		*profile = newProfile;
		return EXIT_SUCCESS;
	}

    
	DllExport int DataDownsampleByFactor(const float *dataPtr, float **newDataPtr, int64_t dimX, int64_t dimY, int64_t dimZ, int factorX, int factorY, int factorZ)
	{
		int64_t newDimX = dimX / factorX;
		int64_t newDimY = dimY / factorY;
		int64_t newDimZ = dimZ / factorZ;
		if (dimX % factorX != 0)
			newDimX++;
		if (dimY % factorY != 0)
			newDimY++;
		if (dimZ % factorZ != 0)
			newDimZ++;
		int64_t newSize = newDimX * newDimY * newDimZ;
		float* reducedCube = new float[newSize] {};
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
						oldZ = newZ * factorZ + pixelZ;
						for (auto pixelY = 0; pixelY < blockSizeY; pixelY++)
						{
							oldY = newY * factorY + pixelY;
							for (auto pixelX = 0; pixelX < blockSizeX; pixelX++)
							{
								oldX = newX * factorX + pixelX;
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
	
	DllExport int FreeMemory(void* ptrToDelete)
	{
		delete[] ptrToDelete;
		return EXIT_SUCCESS;
	}
}
