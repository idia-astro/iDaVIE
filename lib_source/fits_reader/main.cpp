#include <cfitsio/fitsio.h>


#define DllExport __declspec (dllexport)

extern "C"
{
	DllExport int FitsOpenFile(fitsfile **fptr, char* filename, int mode)
	{
		int status;
		//fitsfile *fptr2;
		fits_open_file(fptr, filename, READONLY, &status);
		//fptr = &fptr2;
		return status;
	}

	DllExport int FitsCloseFile(fitsfile *fptr, int *status)
	{
		return fits_close_file(fptr, status);
	}

	/*
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
	DllExport int FitsReadCol(fitsfile *fptr, int datatype, int colnum, LONGLONG firstrow,
		LONGLONG firstelem, LONGLONG nelem, void *nulval, void *array, int *anynul,
		int  *status)
	{
		return fits_read_col(fptr, datatype, colnum, firstrow, firstelem, nelem, nulval, array, anynul, status);

	}
	*/


}