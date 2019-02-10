#include <iostream>
#include <vector>
#include <math.h>
#include <omp.h>

#define DllExport __declspec (dllexport)

#define EXIT_SUCCESS 0
#define EXIT_FAILURE 1


extern "C"
{
	DllExport int FindMaxMin(float *dataPtr, long numberElements, float *maxResult, float *minResult)
	{
		float maxVal = -std::numeric_limits<float>::max();
		float minVal = std::numeric_limits<float>::max();
		#pragma omp parallel 
		{
			float currentMax = -std::numeric_limits<float>::max();
			float currentMin = std::numeric_limits<float>::max();
			#pragma omp for
			for (int i = 0; i < numberElements; i++)
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
		int index = xDim * yDim * (z - 1) + xDim * (y - 1) + (x - 1);
		outValue = dataPtr[index];
		*voxelValue = outValue;
		return EXIT_SUCCESS;
	}

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

    //
	/*
	DllExport int NearNeighborScale(float *dataPtr, float **newDataPtr, int dimX, int dimY, int dimZ, int windowX, int windowY, int windowZ)
	{
		int newDimX = dimX / windowX;
		int newDimY = dimY / windowY;
		int newDimZ = dimZ / windowZ;
		int extraX = dimX % windowX;
		int extraY = dimY % windowY;
		int extraZ = dimZ % windowZ;
		int windowSize = windowX * windowY * windowZ;
		std::vector<float> windowVector;
		if (extraX)
			newDimX++;
		if (extraY)
			newDimY++;
		if (extraZ)
			newDimZ++;
		int newCubeSize = newDimX * newDimY * newDimZ;
		float* reducedCube = new float[newCubeSize];
		
		int sliceSize = dimX * dimY;
		int newSliceSize = newDimX * newDimY;
		int rowSize = dimY;
		int newRowSize = newDimX;

		int zStart, yStart, xStart, zEnd, yEnd, xEnd;
		float sum;
		for (int i = 0; i < newCubeSize; i++)
		{
			zStart = i / newSliceSize * windowZ;
			zEnd = (extraZ ? zStart + extraZ : zStart + windowZ);
			for (int z = zStart; z < zEnd; z++)
			{
				yStart = i % newSliceSize / newRowSize * windowY;
				yEnd = (extraY ? yStart + extraY : yStart + windowY);

				for (int y = yStart; yEnd; y++)
				{
					xStart = i % newRowSize * windowX;
					xEnd = (extraX ? xStart + extraX : xStart + windowX);

					for (int x = xStart; x < xEnd; x++)
					{
						float valueToInsert = dataPtr[z * sliceSize + y * rowSize + x];
						windowVector.push_back(valueToInsert);
					}
				}
			}
			sum = 0;
			for (int j = 0; j < (int)windowVector.size(); j++)
			{
				float value = windowVector[j];
				sum += value;
			}
			reducedCube[i] = sum / (float)windowVector.size();
			windowVector.clear();
		}	
		*newDataPtr = reducedCube;
		return 0;
	}
	*/
	DllExport int FreeMemory(void* ptrToDelete)
	{
		delete[] ptrToDelete;
		return EXIT_SUCCESS;
	}
}
