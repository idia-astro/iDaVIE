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
        astClearStatus;
        return -1;
    }
    if (!header)
    {
        return -1;
    }
    astPutCards(fitschan, header);
    wcsinfo = static_cast<AstFrameSet*>(astRead(fitschan));
    if (!astOK)
    {
        astClearStatus;
        return -1;
    }
    else if (wcsinfo == AST__NULL)
    {
        return -1;
    }
    else if (strcmp(astGetC(wcsinfo, "Class"), "FrameSet"))
    {
        return -1;
    }
    *astFrameSetPtr = wcsinfo;
    return 0;
}

int Format(AstFrameSet* wcsinfo, int axis, double value, char* formattedVal, int formattedValLength)
{
    if (!wcsinfo)
    {
        return -1;
    }
    strcpy_s(formattedVal, formattedValLength, astFormat(wcsinfo, axis, value));
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
        return 1;
    }

    astSet(obj, attrib);
    if (!astOK)
    {
        astClearStatus;
        return 1;
    }
    return 0;
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
        return 1;
    }
    astNorm(wcsinfo, inout);
    return 0;
}

int Transform(AstFrameSet* wcsinfo, int npoint, const double xin[], const double yin[], int forward, double xout[], double yout[])
{
    if (!wcsinfo)
    {
        return 1;
    }

    astTran2(wcsinfo, npoint, xin, yin, forward, xout, yout);
    if (!astOK)
    {
        astClearStatus;
        return 1;
    }
    return 0;
}

int Transform3D(AstSpecFrame* wcsinfo, double x, double y, double z, const int forward, double** output)
{
    if (!wcsinfo)
    {
        return 1;
    }

    double in[] ={x, y, z};
    astTranN(wcsinfo, 1, 3, 1, in, forward, 3, 1, *output);
    if (!astOK)
    {
        astClearStatus;
        return 1;
    }
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
