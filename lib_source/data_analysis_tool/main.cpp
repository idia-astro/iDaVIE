#include <iostream>
#include <vector>
#include <math.h>
#include <omp.h>

#define DllExport __declspec (dllexport)

extern "C"
{
	DllExport void FindMaxMin(float *dataPtr, long numberElements, float *maxResult, float *minResult)
	{
		float val;
		float maxVal = -std::numeric_limits<float>::max();
		float minVal = std::numeric_limits<float>::max();
#pragma omp parallel 
		{
			float currentMax = -std::numeric_limits<float>::max();
			float currentMin = -std::numeric_limits<float>::min();

#pragma omp for
			for (int i = 0; i < numberElements; i++)
			{
				val = dataPtr[i];
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
	}

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

	DllExport int FreeMemory(void* ptrToDelete)
	{
		delete[] ptrToDelete;
		return 0;
	}
}
