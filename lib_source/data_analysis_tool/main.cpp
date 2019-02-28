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

    
	DllExport int DataDownsampleByFactor(float *dataPtr, float **newDataPtr, int dimX, int dimY, int dimZ, int factorX, int factorY, int factorZ)
	{
		int oldSize = dimX * dimY * dimZ;
		int newDimX = dimX / factorX;
		int newDimY = dimY / factorY;
		int newDimZ = dimZ / factorZ;
		if (dimX % factorX != 0)
			newDimX = newDimX + 1;
		if (dimY % factorY != 0)
			newDimY = newDimY + 1;
		if (dimZ % factorZ != 0)
			newDimZ = newDimZ + 1;
		int newSize = newDimX * newDimY * newDimZ;
		float* reducedCube = new float[newSize];
		int* pixelCount = new int[newSize];
		for (int i = 0; i < newSize; i++)
		{
			pixelCount[i] = 0;
			reducedCube[i] = 0;
		}
		
		int newX, newY, newZ, oldIndex, newIndex;
		for (int oldZ = 0; oldZ < dimZ; oldZ++)
		{
			newZ = oldZ / factorZ;
			for (int oldY = 0; oldY < dimY; oldY++)
			{
				newY = oldY / factorY;
				for (int oldX = 0; oldX < dimX; oldX++)
				{
					newX = oldX / factorX;
					oldIndex = oldZ * dimX*dimY + oldY * dimX + oldX;
					newIndex = newZ * newDimX*newDimY + newY * newDimX + newX;
					reducedCube[newIndex] = reducedCube[newIndex] + dataPtr[oldIndex];
					pixelCount[newIndex]++;
				}
			}
		//	reducedCube[newIndex] = reducedCube[newIndex] + dataPtr[oldIndex];
		//	pixelCount[newIndex]++;
		}
		
		
		for (int i = 0; i < newSize; i++)
		{
			if (pixelCount[i] != 0)
				reducedCube[i] = reducedCube[i] / (float)pixelCount[i];
			else
				reducedCube[i] = NAN;
		}
		//delete[] pixelCount;
		*newDataPtr = reducedCube;
		return 0;
	}
	
	DllExport int FreeMemory(void* ptrToDelete)
	{
		delete[] ptrToDelete;
		return EXIT_SUCCESS;
	}
}
