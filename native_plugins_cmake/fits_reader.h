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
static constexpr std::string_view defaultDebugFile = "Outputs/Logs/i-DaVIE_Plugin_Debug.log";

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

DllExport int FitsWriteImageInt16(fitsfile * , int , int64_t , int16_t* , int* );

DllExport int FitsWriteSubImageInt16(fitsfile * , long* , long* , int16_t* , int* );

DllExport int FitsWriteHistory(fitsfile *, char *,  int *);

DllExport int FitsWriteKey(fitsfile * , int , char *, void *, char *, int *);

DllExport int FitsUpdateKey(fitsfile * , int , char* , void* , char* , int* );

DllExport int FitsDeleteKey(fitsfile *, char*, int*);

DllExport int FitsGetImageSize(fitsfile *, int , int64_t **, int *);

DllExport int FitsReadColFloat(fitsfile *, int , long ,
                               long , int64_t , float **, int  *);

DllExport int FitsReadColString(fitsfile *, int , long ,
                                long , int64_t , char ***, char **, int  *);

DllExport int FitsReadImageFloat(fitsfile *, int , int64_t , float **, int *);

DllExport int FitsReadSubImageFloat(fitsfile *, int, int, long *, long *, int64_t, float **, int *);

DllExport int FitsReadImageInt16(fitsfile *, int , int64_t , int16_t **, int *);

DllExport int FitsReadSubImageInt16(fitsfile *, int, int, long *, long *, int64_t, float **, int *);

DllExport int FitsCreateHdrPtrForAst(fitsfile *, char **, int *, int *);

DllExport int CreateEmptyImageInt16(int64_t , int64_t , int64_t , int16_t** );

DllExport int FreeFitsPtrMemory(void* );

DllExport void FreeFitsMemory(char* header, int* status);

DllExport int WriteLogFile(const char * fileName, const char * content, int type);

DllExport int WriteMomentMap(fitsfile *, char*, float*, long, long, int);

int writeFitsHeader(fitsfile *, fitsfile *, int);

}

#endif //FITS_READER_FITS_READER_H
