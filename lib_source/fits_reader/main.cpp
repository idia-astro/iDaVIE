#include <cfitsio/fitsio.h>
#include <iostream>


#define DllExport __declspec (dllexport)

extern "C"
{
	DllExport int FitsOpenFile(fitsfile **fptr, char* filename,  int *status)
	{
		return fits_open_file(fptr, filename, READONLY, status);
	}

	DllExport int FitsCloseFile(fitsfile *fptr, int *status)
	{
		return fits_close_file(fptr, status);
	}
	
	DllExport int FitsMovabsHdu(fitsfile *fptr, int hdunum, int *hdutype, int *status)
	{
		return fits_movabs_hdu(fptr, hdunum, hdutype, status);
	}

	DllExport int FitsGetNumRows(fitsfile *fptr, long *nrows, int *status)
	{
		return fits_get_num_rows(fptr, nrows, status);
	}

	DllExport int FitsGetNumCols(fitsfile *fptr, int  *ncols, int *status)
	{
		return fits_get_num_cols(fptr, ncols, status);
	}

	DllExport int FitsMakeKeyN(const char *keyroot, int value, char *keyname, int *status)
	{
		return fits_make_keyn(keyroot, value, keyname, status);
	}

	DllExport int FitsReadKey(fitsfile *fptr, int datatype, const char *keyname, void *value,
		char *comm, int *status)
	{
		return fits_read_key(fptr, datatype, keyname, value, comm, status);
	}

	DllExport int FitsGetImageDims(fitsfile *fptr, int  *dims, int *status)
	{
		return fits_get_img_dim(fptr, dims, status);
	}

	
	DllExport int FitsGetImageSize(fitsfile *fptr, int dims, int64_t **naxes, int *status)
	{
		int64_t* imageSize = new int64_t[dims];
		fits_get_img_sizell(fptr, dims, imageSize, status);
		*naxes = imageSize;
		return 0;
	}


	DllExport int FitsReadColFloat(fitsfile *fptr, int colnum, long firstrow,
		long firstelem, int64_t nelem, float **array, int  *status)
	{
		int anynul;
		float nulval = 0;
		float *dataArray = new float[nelem];
		int success = fits_read_col(fptr, TFLOAT, colnum, firstrow, firstelem, nelem, &nulval, dataArray, &anynul, status);
		*array = dataArray;
		return success;
	}

	DllExport int FitsReadColString(fitsfile *fptr, int colnum, long firstrow,
		long firstelem, int64_t nelem, char ***ptrarray, char **chararray, int  *status)
	{
		int anynul;
		float nulval = 0;
		char **dataArray = new char*[sizeof(char*)*nelem];
		char *dataArrayElements = new char[sizeof(char)*nelem*FLEN_VALUE];
		for (int i = 0; i < nelem; i++)
			*(dataArray + i) = (dataArrayElements + i* FLEN_VALUE);
		int success = fits_read_col(fptr, TSTRING, colnum, firstrow, firstelem, nelem, &nulval, dataArray, &anynul, status);
		*ptrarray = dataArray;
		*chararray = dataArrayElements;
		return success;
	}
	
	DllExport int FitsReadImageFloat(fitsfile *fptr, int dims, int64_t nelem, float **array, int *status)
	{
		int anynul;
		float nulval = 0;
		float* dataarray = new float[nelem];
		int64_t* startPix = new int64_t[dims];
		for (int i = 0; i < dims; i++)
			startPix[i] = 1;
		int success = fits_read_pixll(fptr, TFLOAT, startPix, nelem, &nulval, dataarray, &anynul, status);
		delete[] startPix;
		*array = dataarray;
		return success;
	}

	DllExport int FitsReadImageInt16(fitsfile *fptr, int dims, int64_t nelem, int16_t **array, int *status)
	{
		int anynul;
		float nulval = 0;
		int16_t* dataarray = new int16_t[nelem];
		int64_t* startPix = new int64_t[dims];
		for (int i = 0; i < dims; i++)
			startPix[i] = 1;
		int success = fits_read_pixll(fptr, TSHORT, startPix, nelem, &nulval, dataarray, &anynul, status);
		delete[] startPix;
		*array = dataarray;
		return success;
	}

	DllExport int FreeMemory(void* ptrToDelete)
	{
		delete[] ptrToDelete;
		return 0;
	}
}