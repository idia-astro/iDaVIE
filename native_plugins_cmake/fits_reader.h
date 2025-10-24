/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 Inter-University Institute for Data Intensive Astronomy
 *
 * This file is part of the iDaVIE project.
 *
 * iDaVIE is free software: you can redistribute it and/or modify it under the terms 
 * of the GNU Lesser General Public License (LGPL) as published by the Free Software 
 * Foundation, either version 3 of the License, or (at your option) any later version.
 *
 * iDaVIE is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR 
 * PURPOSE. See the GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License along with 
 * iDaVIE in the LICENSE file. If not, see <https://www.gnu.org/licenses/>.
 *
 * Additional information and disclaimers regarding liability and third-party 
 * components can be found in the DISCLAIMER and NOTICE files included with this project.
 *
 */
#ifndef FITS_READER_FITS_READER_H
#define FITS_READER_FITS_READER_H

#define DllExport __declspec (dllexport)

#include <cfitsio/fitsio.h>
#include <cstring>
#include <iostream>
#include <cstring>
#include <string_view>
#include <cmath>
#include <fstream>
#include <limits>
#include <regex>
#include <sstream>
#include <string>

// These are the keys that will be copied over from the main fits cube to the moment maps if they are exported as fits files
const std::vector<std::string> REQUIRED_MOMENT_MAP_DBL_KEYS = {"CRVAL1", "CDELT1", "CRPIX1", "CRVAL2", "CDELT2", "CRPIX2", "BMAJ", "BMIN", "BPA"};
const std::vector<std::string> REQUIRED_MOMENT_MAP_STR_KEYS = {"CTYPE1", "CTYPE2"};

//Use the WriteLogFile function to output directly to a text file for debugging.
static constexpr std::string_view defaultDebugFile = "Outputs/Logs/iDaVIE_Plugin_Log_0.log";

extern "C"
{
DllExport int FitsOpenFileReadOnly(fitsfile **, char *,  int *);

DllExport int FitsOpenFileReadWrite(fitsfile ** , char *, int *);

DllExport int FitsCreateFile(fitsfile ** , char * , int *);

DllExport int FitsCloseFile(fitsfile *, int *);

DllExport int FitsFlushFile(fitsfile * , int *);

DllExport int FitsGetHduCount(fitsfile *, int *, int *);

DllExport int FitsGetCurrentHdu(fitsfile *, int *);

DllExport int FitsMovabsHdu(fitsfile *, int , int *, int *);

DllExport int FitsGetHduType(fitsfile *, int *, int *);

DllExport int FitsGetNumHdus(fitsfile *, int *, int *);

DllExport int FitsGetNumHeaderKeys(fitsfile *, int *, int *, int *);

DllExport int FitsGetNumRows(fitsfile *, long *, int *);

DllExport int FitsGetNumCols(fitsfile *, int  *, int *);

DllExport int FitsMakeKeyN(const char *, int , char *, int *);

DllExport int FitsReadKeyString(fitsfile * , const char *, char *, char *, int *);

DllExport int FitsReadKey(fitsfile *, int , const char *, void *,
                          char *, int *);

DllExport int FitsReadKeyN(fitsfile *, int , char *, char *,
                           char *, int *);

DllExport int FitsGetImageDims(fitsfile *, int  *, int *);

DllExport int FitsCreateImg(fitsfile * ,int , int , long *, int *);

DllExport int FitsCopyHeader(fitsfile *, fitsfile *, int *);

DllExport int FitsCopyFile(fitsfile *, fitsfile *, int *);

DllExport int FitsCopyCubeSection(fitsfile *, fitsfile *, char *, int *); 

[[deprecated("Replaced by FitsWriteSubImageInt16, which is more flexible.")]]
DllExport int FitsWriteImageInt16(fitsfile * , int , int64_t , int16_t* , int* );

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
DllExport int FitsWriteSubImageInt16(fitsfile * , long* , long* , int16_t* , int* );

/*
 * @brief Function that writes a new copy of a mask that was loaded as a rectangular subset.
 *
 * @param oldFileName The filepath of the new/destination mask.
 * @param fptr The fitsfile being worked on.
 * @param fPix An array containing the indices of the first pixel (xyz, left bottom front) to be written.
 * @param lPix An array containing the indices of the last pixe (xyz, right top back) to be written.
 * @param array The array containing the data to be written. This is assumed to be at least the size of lPix - fPix.
 * @param status Value containing outcome of CFITSIO operation.
 * @return int 
 */
DllExport int FitsWriteNewCopySubImageInt16(char* , fitsfile* , long* , long* , int16_t* , char* , int* );

DllExport int FitsWriteHistory(fitsfile *, char *,  int *);

DllExport int FitsWriteKey(fitsfile * , int , char *, void *, char *, int *);

DllExport int FitsUpdateKey(fitsfile * , int , char* , void* , char* , int* );

DllExport int FitsDeleteKey(fitsfile *, char*, int*);

DllExport int FitsGetImageSize(fitsfile *, int , int64_t **, int *);

DllExport int FitsReadColFloat(fitsfile *, int , long ,
                               long , int64_t , float **, int  *);

DllExport int FitsReadColString(fitsfile *, int , long ,
                                long , int64_t , char ***, char **, int  *);

[[deprecated("Replaced by FitsReadSubImageFloat, which is more flexible.")]]
DllExport int FitsReadImageFloat(fitsfile *, int , int64_t , float **, int *);

/**
 * @brief Function to read a rectangular subset of the FITS image, which can be any size up to the full size of the image.
 *        This version is for floating point images.
 *        It reads the file in chunks, limited to no more than 2^32 - 1 voxels at a time, to avoid CFITSIO issues with regards to sizeof(long).
 * 
 * @param fptr The fitsfile being worked on.
 * @param dims The number of axes in the FITS image.
 * @param zAxis The index of the z Axis in the FITS image.
 * @param startPix An array containing the indices of the first pixel (xyz, left bottom front) to be written.
 * @param finalPix An array containing the indices of the last pixe (xyz, right top back) to be written.
 * @param nelem The size of the final image loaded, the data point count.
 * @param array The target array to which the data will be loaded.
 * @param status Value containing outcome of CFITSIO operation.
 * @return int The result code, 0 for success, a CFITSIO error code if not.
 */
DllExport int FitsReadSubImageFloat(fitsfile *, int, int, long *, long *, int64_t, float **, int *);

[[deprecated("Replaced by FitsReadSubImageInt16, which is more flexible.")]]
DllExport int FitsReadImageInt16(fitsfile *, int , int64_t , int16_t **, int *);

/**
 * @brief Function to read a rectangular subset of the FITS image, which can be any size up to the full size of the image.
 *        This version is for Int16 images.
 * 
 * @param fptr The fitsfile being worked on.
 * @param dims The number of axes in the FITS image.
 * @param zAxis The index of the z Axis in the FITS image.
 * @param startPix An array containing the indices of the first pixel (xyz, left bottom front) to be written.
 * @param finalPix An array containing the indices of the last pixe (xyz, right top back) to be written.
 * @param nelem The size of the final image loaded, the data point count.
 * @param array The target array to which the data will be loaded.
 * @param status Value containing outcome of CFITSIO operation.
 * @return int The result code, 0 for success, a CFITSIO error coded if not.
 */
DllExport int FitsReadSubImageInt16(fitsfile *, int, int, long *, long *, int64_t, float **, int *);

DllExport int FitsCreateHdrPtrForAst(fitsfile *, char **, int *, int *);

DllExport int CreateEmptyImageInt16(int64_t , int64_t , int64_t , int16_t** );

DllExport int FreeFitsPtrMemory(void* );

DllExport void FreeFitsMemory(char* header, int* status);

/**
 * @brief 
 * Function to write debug logging to file.
 * @param fileName The log file to be written to.
 * @param content The content to be written to the file.
 * @param type The type of message to write to the log file.
 * @return int Returns the status. 0 if successful, 1 if an exception is thrown and caught.
 */
DllExport int WriteLogFile(const char * fileName, const char * content, int type);

/**
 * @brief 
 * Writes out an image as supplied by the pixels in imgPixs to filename in FITS format.
 * @param filename The destination file name.
 * @param imgPixs The data to be written out to the file. This data is expected to be in row-major form, same as FITS.
 * @param xDims The dimensions of the final file in the x axis (NAXIS1).
 * @param yDims The dimensions of the final file in the y axis (NAXIS2).
 * @return int Returns the status. 0 if successful, see the usual table if not 0.
 */
DllExport int WriteMomentMap(fitsfile *, char*, float*, long, long, int);

/**
 * @brief 
 * Function to write header values for the fits file, called when writing moment maps.
 * Consider adding RA and DEC values, and x/y axis units as well.
 * @param newFitsFile The fitsfile to be written to.
 * @return int Returns the status. 0 if successful, see the usual table if not 0.
 */
int writeFitsHeader(fitsfile *, fitsfile *, int);

}

#endif //FITS_READER_FITS_READER_H
