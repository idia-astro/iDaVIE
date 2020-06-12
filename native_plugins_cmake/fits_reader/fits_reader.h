#ifndef FITS_READER_FITS_READER_H
#define FITS_READER_FITS_READER_H

#define DllExport __declspec (dllexport)

#include <cfitsio/fitsio.h>
#include <iostream>
#include <cstring>

///Insert these three lines to debug directly out to a file:
//char* str = new char[70];
//freopen("debug.txt", "a", stdout);
//printf("%s\n", str);


extern "C"
{
DllExport int FitsOpenFileReadOnly(fitsfile**, char*,  int*);

DllExport int FitsOpenFileReadWrite(fitsfile** , char* , int* );

DllExport int FitsCreateFile(fitsfile** , char* , int* );

DllExport int FitsCloseFile(fitsfile *, int *);

DllExport int FitsFlushFile(fitsfile* , int* );

DllExport int FitsMovabsHdu(fitsfile *, int , int *, int *);

DllExport int FitsGetNumHeaderKeys(fitsfile *, int *, int *, int *);

DllExport int FitsGetNumRows(fitsfile *, long *, int *);

DllExport int FitsGetNumCols(fitsfile *, int  *, int *);

DllExport int FitsMakeKeyN(const char *, int , char *, int *);

DllExport int FitsReadKey(fitsfile *, int , const char *, void *,
                          char *, int *);

DllExport int FitsReadKeyN(fitsfile *, int , char *, char *,
                           char *, int *);

DllExport int FitsGetImageDims(fitsfile *, int  *, int *);

DllExport int FitsCreateImg(fitsfile * ,int , int , long *, int *);

DllExport int FitsCopyHeader(fitsfile *, fitsfile *, int *);

DllExport int FitsWriteImageInt16(fitsfile* , int , int64_t , int16_t* , int* );

DllExport int FitsWriteKey(fitsfile* , int , char *, void *, char *, int *);

DllExport int FitsUpdateKey(fitsfile* , int , char* , void* , char* , int* );

DllExport int FitsGetImageSize(fitsfile *, int , int64_t **, int *);

DllExport int FitsReadColFloat(fitsfile *, int , long ,
                               long , int64_t , float **, int  *);

DllExport int FitsReadColString(fitsfile *, int , long ,
                                long , int64_t , char ***, char **, int  *);

DllExport int FitsReadImageFloat(fitsfile *, int , int64_t , float **, int *);

DllExport int FitsReadSubImageFloat(fitsfile *, int, long *, long *, int64_t, float **, int *);

DllExport int FitsReadImageInt16(fitsfile *, int , int64_t , int16_t **, int *);

DllExport int CreateEmptyImageInt16(int64_t , int64_t , int64_t , int16_t** );

DllExport int FreeMemory(void* );

DllExport int InsertSubArrayInt16(int16_t* , int64_t , int16_t* , int64_t , int64_t );

}

class fits_unity
{
};



#endif //FITS_UNITY_LIBRARY_H
