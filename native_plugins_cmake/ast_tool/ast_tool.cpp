#include "ast_tool.h"

int InitFrame(AstFrameSet** astFrameSetPtr, const char* header)
{
    AstFitsChan* fitschan = nullptr;
    AstFrameSet* wcsinfo = nullptr;
    int status = 0;
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
    std::cout << "Successfully initialized AstFrame!" << std::endl;
    *astFrameSetPtr = wcsinfo;
    return 0;
}

int Format(AstFrameSet* wcsinfo, int axis, double value, char* formattedVal, int formattedValLength)
{
    if (!wcsinfo)
    {
        return -1;
    }
    strcpy_s(formattedVal, formattedValLength, astFormat(wcsinfo, axis, value)); //might not work... need to free
    if (!astOK)
    {
        astClearStatus;
        return -1;
    }
    return 0;
}

int Set(AstFrameSet* wcsinfo, const char* attrib)
{
    if (!wcsinfo)
    {
        return -1;
    }
    astSet(wcsinfo, attrib);
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

void Dump(AstFrameSet* wcsinfo, char* stringToReturn)
{
    if (wcsinfo)
    {
        stringToReturn = astToString(wcsinfo);
    }
}

int GetString(AstFrameSet* wcsinfo, const char* attribute, char* stringToReturn, int stringToReturnLen)
{
    if (!wcsinfo || !astHasAttribute(wcsinfo, attribute))
    {
        return -1;
    }
    strcpy_s(stringToReturn, stringToReturnLen, astGetC(wcsinfo, attribute));
    return 0;
}

int Norm(AstFrameSet* wcsinfo, double inout[])
{
    if (!wcsinfo)
    {
        return -1;
    }
    astNorm(wcsinfo, inout);
    return 0;
}

int Transform(AstFrameSet* wcsinfo, int npoint, const double xin[], const double yin[], int forward, double xout[], double yout[])
{
    if (!wcsinfo)
    {
        return -1;
    }

    astTran2(wcsinfo, npoint, xin, yin, forward, xout, yout);
    if (!astOK)
    {
        astClearStatus;
        return -1;
    }
    return 0;
}

int Transform3D(AstSpecFrame* wcsinfo, double xin, double yin, double zin, const int forward, double* xout, double* yout, double* zout)
{
    if (!wcsinfo)
    {
        return -1;
    }
    int nDims = astGetI(wcsinfo, "Naxes");
    double *input = new double[nDims];
    double* output = new double[nDims];
    input[0] = xin;
    input[1] = yin;
    input[2] = zin;
    for (int i = 3; i < nDims ; i++)
    {
        input[i] = 1;
    }
    astTranN(wcsinfo, 1, nDims, 1, input, forward, nDims, 1, output);
    if (!astOK)
    {
        astClearStatus;
        delete[] input;
        delete[] output;
        return -1;
    }
    *xout = output[0];
    *yout = output[1];
    *zout = output[2];
    delete[] input;
    delete[] output;
    return 0;
}

void DeleteObject(AstFrameSet* src)
{
    astDelete(src);
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
