
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

int FitsGetHduType(fitsfile *fptr, int *hdutype, int *status) {
    return fits_get_hdu_type(fptr, hdutype, status);
}

int FitsGetNumHdus(fitsfile *fptr, int *numhdus, int *status) {
    return fits_get_num_hdus(fptr, numhdus, status);
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

int FitsReadKeyString(fitsfile *fptr, const char *keyname, char *value,
                char *comm, int *status)
{
    return fits_read_key_str(fptr, keyname, value, comm, status);
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
    int success = fits_write_pix(fptr, TSHORT, startPix, nelements, array, status);
    delete[] startPix;
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
    int success = fits_read_pixll(fptr, TFLOAT, startPix, nelem, &nulval, dataarray, &anynul, status);
    delete[] startPix;
    *array = dataarray;
    return success;
}

int FitsReadSubImageFloat(fitsfile *fptr, int dims, long *startPix, long *finalPix, int64_t nelem, float **array, int *status)
{
    int anynul;
    float nulval = 0;
    float* dataarray = new float[nelem];
    long* increment = new long[dims];
    for (int i = 0; i < dims; i++)
        increment[i] = 1;
    int success = fits_read_subset(fptr, TFLOAT, startPix, finalPix, increment, &nulval, dataarray, &anynul, status);
    delete[] increment;
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

int WriteLogFile(const char * fileName, const char * content, int type) {
    std::ofstream file;
    std::string header;
    switch (type) {
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
    try {
        file.open(fileName, std::ios_base::app);
        file << header << content << std::endl;
        return 0;
    }
    catch (std::exception &e) {
        std::cerr << "Error with writing from library to debug log." << std::endl;
        return 1;
    }
}

/**
 * @brief 
 * Function to write header values for the fits file, called when writing moment maps.
 * Consider adding RA and DEC values, and x/y axis units as well.
 * @param newFitsFile The fitsfile to be written to.
 * @return int Returns the status. 0 if successful, see the usual table if not 0.
 */
int writeMomMapFitsHeader(fitsfile* mainFitsFile, fitsfile *newFitsFile, int mapNumber)
{
    int status = 0;

    std::string comment = "The software that processed this data";
    std::string val = "i-DaVIE-v";
    auto refVal = val.data();
    fits_write_key(newFitsFile, TSTRING, "SOFTNAME", refVal, comment.c_str(), &status);
    if (status)
    {
        std::stringstream debug;
        debug << "Error " << std::to_string(status) << " when writing SOFTNAME to FITS header in writeFITSHeader()!";
        WriteLogFile(defaultDebugFile.data(), debug.str().c_str(), 2);
    }

    //Consider changing the version value to instead be pulled from a config file (not known to user), instead of being hard coded like this.
    comment = "Version of the software";
    val = "1.0";
    refVal = val.data();
    fits_write_key(newFitsFile, TSTRING, "SOFTVERS", refVal, comment.c_str(), &status);
    if (status)
    {
        std::stringstream debug;
        debug << "Error " << std::to_string(status) << " when writing SOFTVERS to FITS header in writeFITSHeader()!";
        WriteLogFile(defaultDebugFile.data(), debug.str().c_str(), 2);
    }

    //Consider changing the release date value to instead be pulled from a config file (not known to user), instead of being hard coded like this.
    comment = "Release date of the software";
    val = "2024-08-06";
    refVal = val.data();
    fits_write_key(newFitsFile, TSTRING, "SOFTDATE", refVal, comment.c_str(), &status);
    if (status)
    {
        std::stringstream debug;
        debug << "Error " << std::to_string(status) << " when writing SOFTDATE to FITS header in writeFITSHeader()!";
        WriteLogFile(defaultDebugFile.data(), debug.str().c_str(), 2);
    }

    comment = "Software maintainer";
    val = "IDIA Visualisation Lab <vislab@idia.ac.za>";
    refVal = val.data();
    fits_write_key(newFitsFile, TSTRING, "SOFTAUTH", refVal, comment.c_str(), &status);
    if (status)
    {
        std::stringstream debug;
        debug << "Error " << std::to_string(status) << " when writing SOFTAUTH to FITS header in writeFITSHeader()!";
        WriteLogFile(defaultDebugFile.data(), debug.str().c_str(), 2);
    }

    comment = "Institute responsible for software";
    val = "IDIA https://www.idia.ac.za/";
    refVal = val.data();
    fits_write_key(newFitsFile, TSTRING, "SOFTINST", refVal, comment.c_str(), &status);
    if (status)
    {
        std::stringstream debug;
        debug << "Error " << std::to_string(status) << " when writing SOFTINST to FITS header in writeFITSHeader()!";
        WriteLogFile(defaultDebugFile.data(), debug.str().c_str(), 2);
    }

    char* valueString = new char[FLEN_VALUE];

    // Write the required string keys to the new fits file
    for (int i = 0; i < REQUIRED_MOMENT_MAP_STR_KEYS.size(); i++)
    {
        char* key = const_cast<char*>(REQUIRED_MOMENT_MAP_STR_KEYS[i].c_str());
        //check if the key exists in the main fits file
        if (!fits_read_key(mainFitsFile, TSTRING, key, valueString, nullptr, &status))
        {
            fits_write_key(newFitsFile, TSTRING, key, valueString, "", &status);
            if (status)
            {
                std::stringstream debug;
                debug << "Error " << std::to_string(status) << " when writing " << key << " to FITS header in writeFITSHeader()!";
                WriteLogFile(defaultDebugFile.data(), debug.str().c_str(), 2);
            }
        }
        else
        {
            std::stringstream debug;
            debug << "Cannot find  " << std::string(key) << " from FITS header in writeFITSHeader(). Status is " << std::to_string(status) << "!\n";
            WriteLogFile(defaultDebugFile.data(), debug.str().c_str(), 2);
            status = 0;
        }
    }

    // Write the derived BUNIT depending on the map number
    // If the map is a moment 0 map, the units are CUNIT3 * BUNIT of the original cube
    char* valueString2 = new char[FLEN_VALUE];
    char* cunit3Key = const_cast<char*>("CUNIT3");
    char* bunitKey = const_cast<char*>("BUNIT");
    if (mapNumber == 0)
    {
        if (!fits_read_key(mainFitsFile, TSTRING, cunit3Key, valueString, nullptr, &status))
        {
            if (!fits_read_key(mainFitsFile, TSTRING, bunitKey, valueString2, nullptr, &status))
            {
                std::string bunit = std::string(valueString2) + " " + std::string(valueString);
                fits_write_key(newFitsFile, TSTRING, "BUNIT", const_cast<char*>(bunit.c_str()), "", &status);
                if (status)
                {
                    std::stringstream debug;
                    debug << "Error " << std::to_string(status) << " when writing BUNIT + CUNIT3 to FITS header in writeFITSHeader()!";
                    WriteLogFile(defaultDebugFile.data(), debug.str().c_str(), 2);
                }
            }
            else
            {
                std::stringstream debug;
                debug << "Cannot find  " << std::string(bunitKey) << " from FITS header in writeFITSHeader(). Status is " << std::to_string(status) << "!\n";
                WriteLogFile(defaultDebugFile.data(), debug.str().c_str(), 2);
                status = 0;
            }
        }
        else
        {
            std::stringstream debug;
            debug << "Cannot find  " << std::string(cunit3Key) << " from FITS header in writeFITSHeader(). Status is " << std::to_string(status) << "!\n";
            WriteLogFile(defaultDebugFile.data(), debug.str().c_str(), 2);
            status = 0;
        }
    }
    // Moment 1 map BUNIT is just CUNIT3
    else if (mapNumber == 1)
    {
        if (!fits_read_key(mainFitsFile, TSTRING, cunit3Key, valueString, nullptr, &status))
        {
            fits_write_key(newFitsFile, TSTRING, "BUNIT", valueString, "", &status);
            if (status)
            {
                std::stringstream debug;
                debug << "Error " << std::to_string(status) << " when writing CUNIT3 to BUNIT FITS header in writeFITSHeader()!";
                WriteLogFile(defaultDebugFile.data(), debug.str().c_str(), 2);
            }
        }
        else
        {
            std::stringstream debug;
            debug << "Cannot find  " << std::string(cunit3Key) << " from FITS header in writeFITSHeader(). Status is " << std::to_string(status) << "!\n";
            WriteLogFile(defaultDebugFile.data(), debug.str().c_str(), 2);
            status = 0;
        }
    }


    delete[] valueString;
    delete[] valueString2;
    double* valueDbl = new double;
    // do the same for the double keys
    for (int i = 0; i < REQUIRED_MOMENT_MAP_DBL_KEYS.size(); i++)
    {
        char* key = const_cast<char*>(REQUIRED_MOMENT_MAP_DBL_KEYS[i].c_str());
        //check if the key exists in the main fits file
        if (!fits_read_key(mainFitsFile, TDOUBLE, key, valueDbl, nullptr, &status))
        {
            fits_write_key(newFitsFile, TDOUBLE, key, valueDbl, "", &status);
            if (status)
            {
                std::stringstream debug;
                debug << "Error " << std::to_string(status) << " when writing " << key << " to FITS header in writeFITSHeader()!";
                WriteLogFile(defaultDebugFile.data(), debug.str().c_str(), 2);
            }
        }
        else
        {
            std::stringstream debug;
            debug << "Cannot find  " << std::string(key) << " from FITS header in writeFITSHeader(). Status is " << std::to_string(status) << "!\n";
            WriteLogFile(defaultDebugFile.data(), debug.str().c_str(), 2);
            status = 0;
        }
    }
    delete valueDbl;

    return status;
}

/**
 * @brief 
 * Writes out an image as supplied by the pixels in imgPixs to filename in FITS format.
 * @param filename The destination file name.
 * @param imgPixs The data to be written out to the file. This data is expected to be in row-major form, same as FITS.
 * @param xDims The dimensions of the final file in the x axis (NAXIS1).
 * @param yDims The dimensions of the final file in the y axis (NAXIS2).
 * @return int Returns the status. 0 if successful, see the usual table if not 0.
 */
int WriteMomentMap(fitsfile* mainFitsFile, char* filename, float* imagePixelArray, long xDims, long yDims, int mapNumber)
{
    fitsfile* newFitsFile;
    int status = 0;
    fits_create_file(&newFitsFile, filename, &status);
    if (status)
    {
        std::stringstream debug;
        debug << "Error " + std::to_string(status) + " opening FITS file for FitsWriter::Write()!";
        WriteLogFile(defaultDebugFile.data(), debug.str().c_str(), 2);
        return status;
    }

    long naxes[2];
    naxes[0] = xDims;
    naxes[1] = yDims;

    fits_create_img(newFitsFile, FLOAT_IMG, 2, naxes, &status);
    if (status)
    {
        std::stringstream debug;
        debug << "Error " + std::to_string(status) + " creating FITS file for FitsWriter::Write()!";
        WriteLogFile(defaultDebugFile.data(), debug.str().c_str(), 2);
        return status;
    }

    writeMomMapFitsHeader(mainFitsFile, newFitsFile, mapNumber);

    // write image data
    long* fpixel = new long[2];
    for (int i = 0; i < 2; ++i)
        fpixel[i] = 1;
    long nelements = naxes[0] * naxes[1];

    fits_write_pix(newFitsFile, TFLOAT, fpixel, nelements, imagePixelArray, &status);
    if (status)
    {
        std::cerr << ("Error " + std::to_string(status) + " when writing FITS file via CFITSIO, datatype = TFLOAT!");
        std::stringstream debug;
        debug << "Error " + std::to_string(status) + " opening FITS file for FitsWriter::Write()!";
        WriteLogFile(defaultDebugFile.data(), debug.str().c_str(), 2);
    }

    fits_close_file(newFitsFile, &status);
    if (status)
    {
        std::cerr << ("Error " + std::to_string(status) + " when closing new FITS file for FitsWriter::Write()!");
    }
    delete[] fpixel;

    return status;
}
