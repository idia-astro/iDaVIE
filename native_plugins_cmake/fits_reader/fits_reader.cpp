#include "fits_reader.h"

int FitsOpenFileReadOnly(fitsfile **fptr, char* filename,  int *status)
{
    return fits_open_file(fptr, filename, READONLY, status);
}

int FitsOpenFileReadWrite(fitsfile** fptr, char* filename, int* status)
{
    return fits_open_file(fptr, filename, READWRITE, status);
}

int FitsCreateFile(fitsfile** fptr, char* filename, int* status)
{
    return fits_create_file(fptr, filename, status);
}

int FitsCloseFile(fitsfile *fptr, int *status)
{
    return fits_close_file(fptr, status);
}

int FitsFlushFile(fitsfile* fptr, int* status)
{
    return fits_flush_file(fptr, status);
}

int FitsMovabsHdu(fitsfile *fptr, int hdunum, int *hdutype, int *status)
{
    return fits_movabs_hdu(fptr, hdunum, hdutype, status);
}

int FitsGetNumHeaderKeys(fitsfile *fptr, int *keysexist, int *morekeys, int *status)
{
    return fits_get_hdrspace(fptr, keysexist, morekeys, status);
}

int FitsGetNumRows(fitsfile *fptr, long *nrows, int *status)
{
    return fits_get_num_rows(fptr, nrows, status);
}

int FitsGetNumCols(fitsfile *fptr, int  *ncols, int *status)
{
    return fits_get_num_cols(fptr, ncols, status);
}

int FitsMakeKeyN(const char *keyroot, int value, char *keyname, int *status)
{
    return fits_make_keyn(keyroot, value, keyname, status);
}

int FitsReadKey(fitsfile *fptr, int datatype, const char *keyname, void *value,
                char *comm, int *status)
{
    return fits_read_key(fptr, datatype, keyname, value, comm, status);
}

int FitsReadKeyN(fitsfile *fptr, int keynum, char *keyname, char *value,
                 char *comment, int *status)
{
    return fits_read_keyn(fptr, keynum, keyname, value, comment, status);
}

int FitsGetImageDims(fitsfile *fptr, int  *dims, int *status)
{
    return fits_get_img_dim(fptr, dims, status);
}

int FitsCreateImg(fitsfile *fptr, int bitpix, int naxis, long *naxes, int *status)
{
    int success = fits_create_img(fptr, bitpix, naxis, naxes, status);
    return success;
}

int FitsCopyHeader(fitsfile *infptr, fitsfile *outfptr, int *status)
{
    int success = fits_copy_header(infptr, outfptr, status);
    return success;
}

int FitsWriteImageInt16(fitsfile* fptr, int dims, int64_t nelements, int16_t* array, int* status)
{
    long* startPix = new long[dims];
    for (int i = 0; i < dims; i++)
        startPix[i] = 1;
    int success = fits_write_pix(fptr, TSHORT, startPix, nelements, array, status);
    delete[] startPix;
    return success;
}

int FitsWriteKey(fitsfile* fptr, int datatype, char *keyname, void *value, char *comment, int *status)
{
    int success = fits_write_key(fptr, datatype, keyname, value, comment, status);
    return success;
}

int FitsUpdateKey(fitsfile* fptr, int datatype, char* keyname, void* value, char* comment, int* status)
{
    int success = fits_update_key(fptr, datatype, keyname, value, comment, status);
    return success;
}

int FitsGetImageSize(fitsfile *fptr, int dims, int64_t **naxes, int *status)
{
    int64_t* imageSize = new int64_t[dims];
    int success = fits_get_img_sizell(fptr, dims, imageSize, status);
    *naxes = imageSize;
    return success;
}

int FitsReadColFloat(fitsfile *fptr, int colnum, long firstrow,
                     long firstelem, int64_t nelem, float **array, int  *status)
{
    int anynul;
    float nulval = 0;
    float *dataArray = new float[nelem];
    int success = fits_read_col(fptr, TFLOAT, colnum, firstrow, firstelem, nelem, &nulval, dataArray, &anynul, status);
    *array = dataArray;
    return success;
}

int FitsReadColString(fitsfile *fptr, int colnum, long firstrow,
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

int FitsReadImageFloat(fitsfile *fptr, int dims, int64_t nelem, float **array, int *status)
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

int FitsReadImageInt16(fitsfile *fptr, int dims, int64_t nelem, int16_t **array, int *status)
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

int CreateEmptyImageInt16(int64_t sizeX, int64_t sizeY, int64_t sizeZ, int16_t** array)
{
    int64_t nelem = sizeX * sizeY * sizeZ;
    int16_t* dataarray = new int16_t[nelem];
    std::memset(dataarray, 0, nelem * sizeof(int16_t));
    *array = dataarray;
    return 0;
}

int FreeMemory(void* ptrToDelete)
{
    free(ptrToDelete);
    return 0;
}

int InsertSubArrayInt16(int16_t* mainArray, int64_t mainArraySize, int16_t* subArray, int64_t subArraySize, int64_t startIndex)
{
    if (subArraySize > mainArraySize|| startIndex > mainArraySize || startIndex + subArraySize > mainArraySize)
        return EXIT_FAILURE;
    memcpy(mainArray + startIndex, subArray, subArraySize*sizeof(int16_t));
    return EXIT_SUCCESS;
}