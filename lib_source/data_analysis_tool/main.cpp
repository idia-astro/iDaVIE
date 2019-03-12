#include <iostream>
#include <vector>
#include <math.h>
#include <omp.h>

#define DllExport __declspec (dllexport)

#define EXIT_SUCCESS 0
#define EXIT_FAILURE 1


extern "C"
{
	DllExport int FindMaxMin(float *dataPtr, long long numberElements, float *maxResult, float *minResult)
	{
		float maxVal = -std::numeric_limits<float>::max();
		float minVal = std::numeric_limits<float>::max();
		#pragma omp parallel 
		{
			float currentMax = -std::numeric_limits<float>::max();
			float currentMin = std::numeric_limits<float>::max();
			#pragma omp for
			for (long long i = 0; i < numberElements; i++)
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

	DllExport int GetVoxelValue(float *dataPtr, float *voxelValue, int xDim, int yDim, int zDim, int x, int y, int z)
	{
		float outValue;
		if (x > xDim || y > yDim || z > zDim || x < 1 || y < 1 || z < 1)
			return EXIT_FAILURE;
		long long index = (long long)xDim * (long long)yDim * ((long long)z - 1) + (long long)xDim * ((long long)y - 1) + ((long long)x - 1);
		outValue = dataPtr[index];
		*voxelValue = outValue;
		return EXIT_SUCCESS;
	}
	
	//TODO make compatible with long long
	DllExport int GetXProfile(float *dataPtr, float **profile, int xDim, int yDim, int zDim, int y, int z)
	{
		float* newProfile = new float[xDim];
		if (y > yDim || z > zDim || y < 1 || z < 1)
			return EXIT_FAILURE;
		for (int i = 0; i < xDim; i++)
			newProfile[i] = dataPtr[(z - 1) * xDim * yDim + (y - 1) * xDim + i];
		*profile = newProfile;
		return EXIT_SUCCESS;
	}

	DllExport int GetYProfile(float *dataPtr, float **profile, int xDim, int yDim, int zDim, int x, int z)
	{
		float* newProfile = new float[yDim];
		if (x > xDim || z > zDim || x < 1 || z < 1)
			return EXIT_FAILURE;
		for (int i = 0; i < yDim; i++)
			newProfile[i] = dataPtr[(z - 1) * xDim * yDim + yDim * i + (x - 1)];
		*profile = newProfile;
		return EXIT_SUCCESS;

	}

	DllExport int GetZProfile(float *dataPtr, float **profile, int xDim, int yDim, int zDim, int x, int y)
	{
		float* newProfile = new float[xDim];
		if (x > xDim || y > yDim || x < 1 || y < 1)
			return EXIT_FAILURE;
		for (int i = 0; i < zDim; i++)
			newProfile[i] = dataPtr[i * xDim * yDim + (y - 1) * xDim + (x - 1)];
		*profile = newProfile;
		return EXIT_SUCCESS;
	}

    
	DllExport int DataDownsampleByFactor(const float *dataPtr, float **newDataPtr, long long dimX, long long dimY, long long dimZ, int factorX, int factorY, int factorZ)
	{
		long long newDimX = dimX / factorX;
		long long newDimY = dimY / factorY;
		long long newDimZ = dimZ / factorZ;
		if (dimX % factorX != 0)
			newDimX = newDimX + 1;
		if (dimY % factorY != 0)
			newDimY = newDimY + 1;
		if (dimZ % factorZ != 0)
			newDimZ = newDimZ + 1;
		long long newSize = newDimX * newDimY * newDimZ;
		float* reducedCube = new float[newSize] {};
		#pragma omp parallel 
		#pragma omp for
		for (auto newZ = 0; newZ < newDimZ; newZ++)
		{
			long long  pixelCount, oldZ, oldY, oldX;
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
