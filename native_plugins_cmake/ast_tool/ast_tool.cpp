#include "ast_tool.h"

int initFrame(AstFrameSet** astFrameSetPtr, const char* header)
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

int format(AstFrameSet* wcsinfo, int axis, double value, const char* formattedVal)
{
    if (!wcsinfo)
    {
        return -1;
    }
    formattedVal = astFormat(wcsinfo, axis, value);
    if (!astOK)
    {
        astClearStatus;
        return -1;
    }
    return 0;
}

int set(AstFrameSet* wcsinfo, const char* attrib)
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

int clear(AstObject* obj, const char* attrib)
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

int getString(AstFrameSet* wcsinfo, const char* attribute, const char* stringToReturn)
{
    if (!wcsinfo || !astHasAttribute(wcsinfo, attribute))
    {
        return -1;
    }
    stringToReturn = astGetC(wcsinfo, attribute);
    return 0;
}

int norm(AstFrameSet* wcsinfo, double inout[])
{
    if (!wcsinfo)
    {
        return 1;
    }
    astNorm(wcsinfo, inout);
    return 0;
}

int transform(AstFrameSet* wcsinfo, int npoint, const double xin[], const double yin[], int forward, double xout[], double yout[])
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

int transform3D(AstSpecFrame* wcsinfo, double x, double y, double z, const int forward, double* out)
{
    if (!wcsinfo)
    {
        return 1;
    }

    double in[] ={x, y, z};
    astTranN(wcsinfo, 1, 3, 1, in, forward, 3, 1, out);
    if (!astOK)
    {
        astClearStatus;
        return 1;
    }
    return 0;
}

void deleteObject(AstFrameSet* src)
{
    astDelete(src);
}

AstFrameSet* copy(AstFrameSet* src)
{
    return static_cast<AstFrameSet*> astCopy(src);
}

void invert(AstFrameSet* src)
{
    astInvert(src);
}

