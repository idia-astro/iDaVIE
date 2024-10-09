#include "fits_reader.h"

// #include <chrono>
#include <cmath>
#include <fstream>
#include <limits>
#include <regex>
#include <sstream>
#include <string>

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
    if (fptr == nullptr)
    {
        std::stringstream debug;
        debug << "Fitsfile is already closed! Aborting.";
        WriteLogFile(defaultDebugFile.data(), debug.str().c_str(), 0);    
        return -1;
    }
    std::stringstream debug;
    debug << "Closing fitsfile " << fptr->Fptr->filename << ".";
    WriteLogFile(defaultDebugFile.data(), debug.str().c_str(), 0);
    auto val = fits_close_file(fptr, status);
    fptr = nullptr;
    return val;
}

int FitsFlushFile(fitsfile* fptr, int* status)
{
    return fits_flush_file(fptr, status);
}

int FitsGetHduCount(fitsfile *fptr, int *hdunum, int *status)
{
    return fits_get_num_hdus(fptr, hdunum, status);
}

int FitsGetCurrentHdu(fitsfile *fptr, int *hdunum)
{
    return fits_get_hdu_num(fptr,  hdunum);
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

int FitsDeleteKey(fitsfile *fptr, char *keyname, int *status)
{
    return fits_delete_key(fptr, keyname, status);
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

int FitsCopyFile(fitsfile *infptr, fitsfile *outfptr, int *status)
{
    int success = fits_copy_file(infptr, outfptr, 1, 1, 1, status);
    return success;
}

int FitsCopyCubeSection(fitsfile *infptr, fitsfile *outfptr, char *section, int *status)
{
    int success = fits_copy_image_section(infptr, outfptr, section, status);
    return success; 
}

int FitsWriteImageInt16(fitsfile* fptr, int dims, int64_t nelements, int16_t* array, int* status)
{
    long* startPix = new long[dims];
    for (int i = 0; i < dims; i++)
        startPix[i] = 1;
    
    std::stringstream debug;
    debug << "Writing mask image with " << dims << " dimensions and " << nelements << " elements, starting from [1, 1, 1].";
    WriteLogFile(defaultDebugFile.data(), debug.str().c_str(), 0);
    
    int success = fits_write_pix(fptr, TSHORT, startPix, nelements, array, status);
    delete[] startPix;
    return success;
}

/**
 * @brief Function writes a rectangular subset of the FITS image, which can be any size up to the full size of the image.
 * 
 * @param fptr The fitsfile being worked on.
 * @param fPix An array containing the indices of the first pixel (xyz, left bottom front) to be written.
 * @param lPix An array containing the indices of the last pixe (xyz, right top back) to be written.
 * @param array The array containing the data to be written. This is assumed to be at least the size of lPix - fPix.
 * @param status Value containing outcome of CFITSIO operation.
 * @return int 
 */
int FitsWriteSubImageInt16(fitsfile* fptr, long* fPix, long* lPix, int16_t* array, int* status)
{
    long* firstPix = new long[3];
    for (int i = 0; i < 3; i++)
        firstPix[i] = fPix[i];
    
    std::stringstream debug;
    debug << "Writing mask sub image from [" << firstPix[0] << ", " << firstPix[1] << ", " << firstPix[2] << "] to [" << lPix[0] << ", " << lPix[1] << ", " << lPix[2] << "].";
    WriteLogFile(defaultDebugFile.data(), debug.str().c_str(), 0);
    
    int success = fits_write_subset(fptr, TSHORT, firstPix, lPix, array, status);
    return success;
}

int FitsWriteHistory(fitsfile *fptr, char *history,  int *status)
{
    int success = fits_write_history(fptr, history, status);
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
    
    std::stringstream debug;
    debug << "Reading cube image with " << dims << " dimensions and " << nelem << " elements.";
    WriteLogFile(defaultDebugFile.data(), debug.str().c_str(), 0);
    
    int success = fits_read_pixll(fptr, TFLOAT, startPix, nelem, &nulval, dataarray, &anynul, status);
    delete[] startPix;
    *array = dataarray;
    return success;
}

/**
 * @brief Function to read a rectangular subset of the FITS image, which can be any size up to the full size of the image.
 *        This version is for floating point images.
 * 
 * @param fptr The fitsfile being worked on.
 * @param dims The number of axes in the FITS image.
 * @param zAxis The index of the z Axis in the FITS image.
 * @param startPix An array containing the indices of the first pixel (xyz, left bottom front) to be written.
 * @param finalPix An array containing the indices of the last pixe (xyz, right top back) to be written.
 * @param nelem The size of the final image loaded.
 * @param array The target array to which the data will be loaded.
 * @param status Value containing outcome of CFITSIO operation.
 * @return int 
 */
int FitsReadSubImageFloat(fitsfile *fptr, int dims, int zAxis, long *startPix, long *finalPix, int64_t nelem, float **array, int *status)
{
    int anynul;
    float nulval = 0;
    long* increment = new long[dims];
    for (int i = 0; i < dims; i++)
        increment[i] = 1;
    
    // Calculate the size of a 2D slice
    int64_t sliceSize = (finalPix[0] - startPix[0] + 1) * (finalPix[1] - startPix[1] + 1);
    float* dataarray = new float[nelem];
    /**
     * @brief slicesInChunk specifies the number of slices to read at a time.
     */
    long slicesInChunk = std::max((long) 1, (long) std::floor(std::numeric_limits<long>::max() / sliceSize));
    long finalZ = finalPix[zAxis];

    // auto start = std::chrono::high_resolution_clock::now();
    // Loop over the third dimension
    int64_t offset = 0;
    for (long z = startPix[zAxis]; z <= finalPix[zAxis]; z+=slicesInChunk)
    {
        // Set the start and end pixels for the current slice
        long* sliceStartPix = new long[dims];
        long* sliceFinalPix = new long[dims];
        for (int i = 0; i < dims; i++)
        {
            sliceStartPix[i] = startPix[i];
            sliceFinalPix[i] = finalPix[i];
        }
        sliceStartPix[zAxis] = z;
        sliceFinalPix[zAxis] = std::min(z + slicesInChunk, finalZ);

        // Read the current slice directly into the final dataarray
        std::stringstream debug;
        debug << "Reading cube sub image from [" << sliceStartPix[0] << ", " << sliceStartPix[1] << ", " << sliceStartPix[2] << "] to [" << sliceFinalPix[0] << ", " << sliceFinalPix[1] << ", " << sliceFinalPix[2] << "].";
        WriteLogFile(defaultDebugFile.data(), debug.str().c_str(), 0);
        int success = fits_read_subset(fptr, TFLOAT, sliceStartPix, sliceFinalPix, increment, &nulval, dataarray + offset, &anynul, status);
        
        if (success != 0)
        {
            delete[] increment;
            delete[] sliceStartPix;
            delete[] sliceFinalPix;
            return success;
        }

        // Calculate the offset in the dataarray
        // int64_t offset = sliceSize * (z - startPix[2]);
        int64_t nelem = (sliceFinalPix[0] - sliceStartPix[0] + 1) * (sliceFinalPix[1] - sliceStartPix[1] + 1) * (sliceFinalPix[2] - sliceStartPix[2] + 1);
        offset += nelem;
        delete[] sliceStartPix;
        delete[] sliceFinalPix;
    }

    // auto end = std::chrono::high_resolution_clock::now();
    // auto microsecs = std::chrono::duration_cast<std::chrono::microseconds>(end - start);
    // std::stringstream time;
    // time << "Time taken for loading entire file through fits_read_subset(): " << microsecs.count() << " µs.";
    // WriteLogFile(defaultDebugFile.data(), time.str().c_str(), 0);

    delete[] increment;
    *array = dataarray;
    return 0;
}

int FitsReadImageInt16(fitsfile *fptr, int dims, int64_t nelem, int16_t **array, int *status)
{
    int anynul;
    float nulval = 0;
    int16_t* dataarray = new int16_t[nelem];
    int64_t* startPix = new int64_t[dims];
    for (int i = 0; i < dims; i++)
        startPix[i] = 1;
    std::stringstream debug;
    debug << "Reading mask image with " << dims << " dimensions and " << nelem << " elements.";
    WriteLogFile(defaultDebugFile.data(), debug.str().c_str(), 0);
    int success = fits_read_pixll(fptr, TSHORT, startPix, nelem, &nulval, dataarray, &anynul, status);
    delete[] startPix;
    *array = dataarray;
    return success;
}

/**
 * @brief Function to read a rectangular subset of the FITS image, which can be any size up to the full size of the image.
 *        This version is for Int16 images.
 * 
 * @param fptr The fitsfile being worked on.
 * @param dims The number of axes in the FITS image.
 * @param zAxis The index of the z Axis in the FITS image.
 * @param startPix An array containing the indices of the first pixel (xyz, left bottom front) to be written.
 * @param finalPix An array containing the indices of the last pixe (xyz, right top back) to be written.
 * @param nelem The size of the final image loaded.
 * @param array The target array to which the data will be loaded.
 * @param status Value containing outcome of CFITSIO operation.
 * @return int 
 */
int FitsReadSubImageInt16(fitsfile *fptr, int dims, int zAxis, long *startPix, long *finalPix, int64_t nelem, float **array, int *status)
{
    int anynul;
    float nulval = 0;
    long* increment = new long[dims];
    for (int i = 0; i < dims; i++)
        increment[i] = 1;
    
    // Calculate the size of a 2D slice
    int64_t sliceSize = (finalPix[0] - startPix[0] + 1) * (finalPix[1] - startPix[1] + 1);
    float* dataarray = new float[nelem];
    
    /**
     * @brief slicesInChunk specifies the number of slices to read at a time.
     */
    long slicesInChunk = std::max((long) 1, (long) std::floor(std::numeric_limits<long>::max() / sliceSize));
    long finalZ = finalPix[2];

    // auto start = std::chrono::high_resolution_clock::now();
    // Loop over the third dimension
    int64_t offset = 0;
    for (long z = startPix[2]; z <= finalPix[2]; z+=slicesInChunk)
    {
        // Set the start and end pixels for the current slice
        long* sliceStartPix = new long[dims];
        long* sliceFinalPix = new long[dims];
        for (int i = 0; i < dims; i++)
        {
            sliceStartPix[i] = startPix[i];
            sliceFinalPix[i] = finalPix[i];
        }
        sliceStartPix[zAxis] = z;
        sliceFinalPix[zAxis] = std::min(z + slicesInChunk, finalZ);

        // Read the current slice directly into the final dataarray
        std::stringstream debug;
        debug << "Reading mask sub image from [" << sliceStartPix[0] << ", " << sliceStartPix[1] << ", " << sliceStartPix[2] << "] to [" << sliceFinalPix[0] << ", " << sliceFinalPix[1] << ", " << sliceFinalPix[2] << "].";
        WriteLogFile(defaultDebugFile.data(), debug.str().c_str(), 0);
        int success = fits_read_subset(fptr, TSHORT, sliceStartPix, sliceFinalPix, increment, &nulval, dataarray + offset, &anynul, status);
        
        if (success != 0)
        {
            delete[] increment;
            delete[] sliceStartPix;
            delete[] sliceFinalPix;
            return success;
        }

        // Calculate the offset in the dataarray
        // int64_t offset = sliceSize * (z - startPix[2]);
        int64_t nelem = (sliceFinalPix[0] - sliceStartPix[0] + 1) * (sliceFinalPix[1] - sliceStartPix[1] + 1) * (sliceFinalPix[2] - sliceStartPix[2] + 1);
        offset += nelem;
        delete[] sliceStartPix;
        delete[] sliceFinalPix;
    }

    // auto end = std::chrono::high_resolution_clock::now();
    // auto microsecs = std::chrono::duration_cast<std::chrono::microseconds>(end - start);
    // std::stringstream time;
    // time << "Time taken for loading entire file through fits_read_subset(): " << microsecs.count() << " µs.";
    // WriteLogFile(defaultDebugFile.data(), time.str().c_str(), 0);

    delete[] increment;
    *array = dataarray;
    return 0;
}

int FitsCreateHdrPtrForAst(fitsfile *fptr, char **header, int *nkeys, int *status)    //need to free header string with FreeFitsMemory() after use
{
    bool needToSwap = false;
    int numberExcl = 12;
    char ctype4[68], subtype4[5];
    char **excludeList = new char*[sizeof(char*) * numberExcl];
    for (int i = 0; i < numberExcl; i++)
    {
        excludeList[i] = new char[7];
    }
    for (int i = 0; i < 5; i++)
    {
        std::string cString = "C????" + std::to_string(i + 5);
        std::string nString = "NAXIS" + std::to_string(i + 5);
        strcpy_s(excludeList[i], 7, cString.c_str() );
        strcpy_s(excludeList[i + 5], 7, nString.c_str());
    }
    if (fits_read_keyword(fptr, "CTYPE4", ctype4, nullptr, status) == 202) {
        *status = 0;
        strcpy_s(excludeList[10], 7, "C????4");
        strcpy_s(excludeList[11], 7, "NAXIS4");
        //if an axis 4, check if it is spectral, in which case swap with axis 3 later
    } else {
        strncpy_s(subtype4, 5,  ctype4 + 1, 4);
        subtype4[4] = '\0';
        if (strcmp(subtype4, "FREQ") == 0 || strcmp(subtype4, "VRAD") == 0 || strcmp(subtype4, "VOPT") == 0 ||
        strcmp(subtype4, "VELO") == 0 || strcmp(subtype4, "ZOPT") == 0 || strcmp(subtype4, "WAVE") == 0 ||
        strcmp(subtype4, "AWAV") == 0 || strcmp(subtype4, "AIRW") == 0 || strcmp(subtype4, "VOPT") == 0 ||
        strcmp(subtype4, "VREL") == 0 || strcmp(subtype4, "ENER") == 0 || strcmp(subtype4, "ENER") == 0 ||
        strcmp(subtype4, "WAVN") == 0)
        {
            needToSwap = true;
            strcpy_s(excludeList[10], 7, "C????3");
            strcpy_s(excludeList[11], 7, "NAXIS3");
        }
        else
        {
            strcpy_s(excludeList[10], 7, "C????4");
            strcpy_s(excludeList[11], 7, "NAXIS4");
        }
    }
    fits_hdr2str(fptr, 1, excludeList, numberExcl, header, nkeys, status);
    if (needToSwap)
    {
        std::string headerString(*header);
        std::regex regPattern ("(CTYPE|CDELT|CRPIX|CRVAL|CUNIT|NAXIS|CROTA)(4)");
        std::string result = std::regex_replace(headerString, regPattern, "$013");
        strcpy_s(*header, *nkeys * 80 + 1, result.c_str());
    }
    for (int i = 0; i < numberExcl; i++)
    {
        delete[](excludeList[i]);
    }
    delete[](excludeList);
    return EXIT_SUCCESS;
}

int CreateEmptyImageInt16(int64_t sizeX, int64_t sizeY, int64_t sizeZ, int16_t** array)
{
    int64_t nelem = sizeX * sizeY * sizeZ;
    std::stringstream debug;
    debug << "Creating empty mask file with dimensions [" << sizeX << ", " << sizeY << ", " << sizeZ << "].";
    WriteLogFile(defaultDebugFile.data(), debug.str().c_str(), 0);
    int16_t* dataarray = new int16_t[nelem];
    std::memset(dataarray, 0, nelem * sizeof(int16_t));
    *array = dataarray;
    return 0;
}

int FreeFitsPtrMemory(void* ptrToDelete)
{
    delete[] ptrToDelete;
    return 0;
}

void FreeFitsMemory(char* header, int* status)
{
    fits_free_memory(header, status);
}

int WriteLogFile(const char * fileName, const char * content, int type)
{
    std::ofstream file;
    std::string header;
    switch(type){
        case 0:
            header = "[Debug] ";
            break;
        case 1:
            header = "[Warning] ";
            break;
        case 2:
            header = "[Error] ";
            break;
        default:
            header = "[Message] ";
            break;
    }
    try
    {
        file.open(fileName, std::ios_base::app);
        file << header << content << std::endl;
        return 0;
    }
    catch (std::exception& e)
    {
        std::cerr << "Error with writing from library to debug log." << std::endl;
        return 1;
    }
}