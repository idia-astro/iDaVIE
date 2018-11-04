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

	DllExport int FitsGetImageDims(fitsfile *fptr, int  *dims, int *status)
	{
		return fits_get_img_dim(fptr, dims, status);
	}

	DllExport int FitsGetImageSize(fitsfile *fptr, long  **naxes, int *status)
	{
		//need to allocate
		long* dude = new long[3];
		fits_get_img_size(fptr, 3, dude , status);
		*naxes = dude;
		return 0;

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

	DllExport int FitsReadColFloat(fitsfile *fptr, int colnum, long firstrow,
		long firstelem, long nelem, float **array, int  *status)
	{
		int anynul;
		float nulval = 0;
		float *dataArray = new float[nelem];
		int success = fits_read_col(fptr, TFLOAT, colnum, firstrow, firstelem, nelem, &nulval, dataArray, &anynul, status);
		*array = dataArray;
		return success;
	}

	DllExport int FitsReadColString(fitsfile *fptr, int colnum, long firstrow,
		long firstelem, long nelem, char ***ptrarray, char **chararray, int  *status)
	{
		int anynul;
		float nulval = 0;
		char **dataArray = (char**)malloc(sizeof(char*)*nelem);
		char *dataArrayElements = (char*)malloc(sizeof(char)*nelem*FLEN_VALUE);
		for (int i = 0; i < nelem; i++)
			*(dataArray + i) = (dataArrayElements + i* FLEN_VALUE);
		int success = fits_read_col(fptr, TSTRING, colnum, firstrow, firstelem, nelem, &nulval, dataArray, &anynul, status);
		*ptrarray = dataArray;
		*chararray = dataArrayElements;
		return success;
	}

	DllExport int FreeMemory(void* ptrToDelete)
	{
		delete[] ptrToDelete;
		return 0;
	}

	DllExport int FreeMemoryTwo(void* ptrToDelete1, void* ptrToDelete2)
	{
		delete[] ptrToDelete1;
		delete[] ptrToDelete2;
		return 0;
	}

	DllExport int FitsRead3DFloat(fitsfile *fptr, long naxis1, long naxis2, long naxis3, float **array, int *status)
	{
		int anynul;
		float nulval = 0;
		long totalPix = naxis1 * naxis2 * naxis3;
		float* dataarray = new float[totalPix];
		long startPix[3] = { 1,1 ,1};
		
			//int success = fits_read_3d_flt(fptr, 1, nulval, naxis1, naxis2, naxis1, naxis2, naxis3, dataarray, &anynul, status);
		int success = fits_read_pix(fptr, TFLOAT, startPix, totalPix, &nulval, dataarray, &anynul, status);
		*array = dataarray;
		return success;
	}
	

}