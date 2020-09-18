#include "ast_tool.h"

int InitAstFrameSet(AstFrameSet** astFrameSetPtr, const char* header)
{
    std::cout << "Initializing AstFrameSet..." << std::endl;
    AstFitsChan* fitschan = nullptr;
    AstFrameSet* wcsinfo = nullptr;
    if (wcsinfo)
    {
        astEnd;
    }
	astClearStatus;
    astBegin;
    fitschan = astFitsChan(nullptr, nullptr, "");
    if (!fitschan)
    {
        std::cout << "astFitsChan returned null :(" << std::endl;
        astClearStatus;
        return -1;
    }
    if (!header)
    {
        std::cout << "Missing header argument." << std::endl;
        return -1;
    }
    astPutCards(fitschan, header);
    wcsinfo = static_cast<AstFrameSet*>(astRead(fitschan));
    if (!astOK)
    {
        std::cout << "Some AST LIB error, check logs." << std::endl;
        astClearStatus;
        return -1;
    }
    else if (wcsinfo == AST__NULL)
    {
        std::cout << "No WCS found" << std::endl;
        return -1;
    }
    else if (strcmp(astGetC(wcsinfo, "Class"), "FrameSet"))
    {
        std::cout << "check FITS header (astlib)" << std::endl;
        return -1;
    }
    astShow(wcsinfo);
    *astFrameSetPtr = wcsinfo;
    return 0;
}

int GetAstFrame(AstFrameSet* frameSetPtr, AstFrame** astFramePtr, int index)
{
    if (!frameSetPtr)
    {
        return -1;
    }
    *astFramePtr = static_cast<AstFrame*>(astGetFrame(frameSetPtr, index));
    if (!astOK)
    {
        astClearStatus;
        return -1;
    }
    return 0;
}

int Format(AstFrameSet* frameSetPtr, int axis, double value, char* formattedVal, int formattedValLength)
{
    if (!frameSetPtr)
    {
        return -1;
    }
    strcpy_s(formattedVal, formattedValLength, astFormat(frameSetPtr, axis, value));
    if (!astOK)
    {
        astClearStatus;
        return -1;
    }
    return 0;
}

int Set(AstFrameSet* frameSetPtr, const char* attrib)
{
    if (!frameSetPtr)
    {
        return -1;
    }
    astSet(frameSetPtr, attrib);
    if (!astOK)
    {
        astClearStatus;
        return -1;
    }
    return 0;
}

int Clear(AstObject* obj, const char* attrib)
{
    if (!obj)
    {
        return -1;
    }

    astSet(obj, attrib);
    if (!astOK)
    {
        astClearStatus;
        return -1;
    }
    return 0;
}

void Dump(AstFrameSet* frameSetPtr, char* stringToReturn)
{
    if (frameSetPtr)
    {
        stringToReturn = astToString(frameSetPtr);
    }
}

int GetString(AstFrameSet* frameSetPtr, const char* attribute, char* stringToReturn, int stringToReturnLen)
{
    if (!frameSetPtr || !astHasAttribute(frameSetPtr, attribute))
    {
        return -1;
    }
    strcpy_s(stringToReturn, stringToReturnLen, astGetC(frameSetPtr, attribute));
    return 0;
}

int Norm(AstFrameSet* frameSetPtr, double xIn, double yIn, double zIn, double* xOut, double* yOut, double* zOut)
{
    if (!frameSetPtr)
    {
        return -1;
    }
    int nDims = astGetI(frameSetPtr, "Naxes");
    double *coords = new double[nDims];
    coords[0] = xIn;
    coords[1] = yIn;
    coords[2] = zIn;
    for (int i = 3; i < nDims ; i++)
    {
        coords[i] = 1;
    }
    astNorm(frameSetPtr, coords);
    if (!astOK)
    {
        astClearStatus;
        delete[] coords;
        return -1;
    }
    *xOut = coords[0];
    *yOut = coords[1];
    *zOut = coords[2];
    delete[] coords;
    return 0;
}

int Distance1D(AstFrame* astFramePtr, double start, double end, int axis, double* distance)
{
    if (!astFramePtr)
    {
        return -1;
    }
    *distance = astAxDistance(astFramePtr, axis, start, end);
    if (!astOK)
    {
        astClearStatus;
        return -1;
    }
    return 0;
}

int Transform(AstFrameSet* frameSetPtr, int npoint, const double xin[], const double yin[], int forward, double xout[], double yout[])
{
    if (!frameSetPtr)
    {
        return -1;
    }

    astTran2(frameSetPtr, npoint, xin, yin, forward, xout, yout);
    if (!astOK)
    {
        astClearStatus;
        return -1;
    }
    return 0;
}

int Transform3D(AstFrameSet* frameSetPtr, double xIn, double yIn, double zIn, const int forward, double* xOut, double* yOut, double* zOut)
{
    if (!frameSetPtr)
    {
        return -1;
    }
    int nDims = astGetI(frameSetPtr, "Naxes");
    double *input = new double[nDims];
    double* output = new double[nDims];
    input[0] = xIn;
    input[1] = yIn;
    input[2] = zIn;
    for (int i = 3; i < nDims ; i++)
    {
        input[i] = 1;
    }
    astTranN(frameSetPtr, 1, nDims, 1, input, forward, nDims, 1, output);
    if (!astOK)
    {
        astClearStatus;
        delete[] input;
        delete[] output;
        return -1;
    }
    *xOut = output[0];
    *yOut = output[1];
    *zOut = output[2];
    delete[] input;
    delete[] output;
    return 0;
}

int SpectralTransform(AstFrameSet* frameSetPtr, const char* specSysTo, const char* specUnitTo, const char* specRestTo, double zIn, const int forward, double* zOut, char* formatZOut, int formatLength)
{
    if (!frameSetPtr)
    {
        return 1;
    }

    AstFrameSet* frameSetTo = nullptr;
    frameSetTo = static_cast<AstFrameSet*> astCopy(frameSetPtr);
    if (!frameSetTo)
    {
        return 1;
    }

    char buffer[128];
    if (specSysTo) {
        snprintf(buffer, sizeof(buffer), "System(3)=%s", specSysTo);
        astSet(frameSetTo, buffer);
    }
    if (specUnitTo) {
        snprintf(buffer, sizeof(buffer), "Unit(3)=%s", specUnitTo);
        astSet(frameSetTo, buffer);
    }
    if (specRestTo) {
        snprintf(buffer, sizeof(buffer), "StdOfRest(3)=%s", specRestTo);
        astSet(frameSetTo, buffer);
    }

    AstFrameSet *cvt;
    cvt = static_cast<AstFrameSet*>astConvert(frameSetPtr, frameSetTo, "");
    int nDims = astGetI(frameSetPtr, "Naxes");
    double *input = new double[nDims];
    double* output = new double[nDims];
    for (int i = 0; i < nDims ; i++)
    {
        input[i] = 1;
    }
    input[2] = zIn;
    astTranN(cvt, 1, nDims, 1, input, forward, nDims, 1, output);
    if (!astOK)
    {
        astClearStatus;
        delete[] input;
        delete[] output;
        return -1;
    }
    *zOut = output[2];
    delete[] input;
    delete[] output;
    strcpy_s(formatZOut, formatLength, astFormat(cvt, 3, output[2]));
    if (!astOK)
    {
        astClearStatus;
        return -1;
    }
    return 0;
}



void DeleteObject(AstFrameSet* frameSetPtr)
{
    astDelete(frameSetPtr);
}

int Copy(AstFrameSet* src, AstFrameSet** copy)
{
    *copy = static_cast<AstFrameSet*> astCopy(src);
    if (!astOK)
    {
        astClearStatus;
        return -1;
    }
    return 0;
}

void Invert(AstFrameSet* src)
{
    astInvert(src);
}

void FreeMemory(void* ptrToDelete)
{
    delete[] ptrToDelete;
}
