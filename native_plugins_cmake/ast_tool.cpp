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
#include "ast_tool.h"

#include <string>
#include <regex>

const double M_PI = 3.141592653589793238463;

std::string GetStringFromFitsChan(const AstFitsChan *chan, const char *name) {
  char *val_ptr;
  if (astGetFitsS(chan, name, &val_ptr)) {
    astClear(chan, "Card");
    return val_ptr;
  }
  astClear(chan, "Card");
  return "";
}

bool FixNcpHeaders(AstFitsChan* fitschan)
{
  std::string ra_type = GetStringFromFitsChan(fitschan, "CTYPE1");
  std::string dec_type = GetStringFromFitsChan(fitschan, "CTYPE2");
  double crval2 = 0.0;
  if (ra_type.find("-NCP") != std::string::npos && dec_type.find("-NCP") != std::string::npos && astGetFitsF(fitschan, "CRVAL2", &crval2)) {
    std::cout << "Need to translate NCP->SIN and define PV matrix" << std::endl;
    ra_type = std::regex_replace(ra_type, std::regex("-NCP"), "-SIN");
    dec_type = std::regex_replace(dec_type, std::regex("-NCP"), "-SIN");
    double pv22 = 1.0 / std::tan(crval2 * M_PI / 180.0);

    astClear(fitschan, "Card");
    astFindFits(fitschan, "CTYPE1", nullptr, 0);
    astSetFitsS(fitschan, "CTYPE1", ra_type.c_str(), nullptr, 1);
    astFindFits(fitschan, "CTYPE2", nullptr, 0);
    astSetFitsS(fitschan, "CTYPE2", dec_type.c_str(), nullptr, 1);
    astSetFitsF(fitschan, "PV2_1", 0.0, "Edited by iDaVIE", 0);
    astSetFitsF(fitschan, "PV2_2", pv22, "Edited by iDaVIE", 0);
    astClear(fitschan, "Card");
    return true;
  }
  return false;
}

int InitAstFrameSet(AstFrameSet** astFrameSetPtr, const char* header, double restFreqGHz = 0)
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

    // Replace all NCP projections with SIN as a workaround
    FixNcpHeaders(fitschan);

    if (restFreqGHz != 0)
    {
        if (astTestFits(fitschan, "RESTFREQ", nullptr) != 0)
        {
            astFindFits(fitschan, "RESTFREQ", nullptr, 0);
            astSetFitsF(fitschan, "RESTFREQ", restFreqGHz * 1.0e9, nullptr, 1);
            astClear(fitschan, "Card");
        }
        else
        {
            astFindFits(fitschan, "RESTFRQ", nullptr, 0);
            astSetFitsF(fitschan, "RESTFRQ", restFreqGHz * 1.0e9, nullptr, 1);
            astClear(fitschan, "Card");
        }
    }
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

int GetAltSpecSet(AstFrameSet* frameSetPtr, AstFrameSet** specFrameSet, const char* specSysTo, const char* specUnitTo, const char* specRestTo)
{
    if (!frameSetPtr)
    {
        std::cout << "No primary frame set provided!" << std::endl;
        return 1;
    }
    AstFrameSet* newFrameSet = static_cast<AstFrameSet*> astCopy(frameSetPtr);
    char buffer[128];
    if (specSysTo) {
        snprintf(buffer, sizeof(buffer), "System(3)=%s", specSysTo);        // TODO: Consider depth in 4th dimension
        astSet(newFrameSet, buffer);
    }
    if (specUnitTo) {
        snprintf(buffer, sizeof(buffer), "Unit(3)=%s", specUnitTo);
        astSet(newFrameSet, buffer);
    }
    if (specRestTo) {
        snprintf(buffer, sizeof(buffer), "StdOfRest(3)=%s", specRestTo);
        astSet(newFrameSet, buffer);
    }
    astShow(newFrameSet);
    *specFrameSet = newFrameSet;
    if (!astOK)
    {
        astClearStatus;
        return -1;
    }
    return 0;
}

int Show(AstObject* astObject)
{
    astShow(astObject);
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

int SetString(AstFrameSet* frameSetPtr, const char* attribute, const char* stringValue)
{
    if (!frameSetPtr || !astHasAttribute(frameSetPtr, attribute))
    {
        return -1;
    }
    astSetC(frameSetPtr, attribute, stringValue);
    return 0;
}

bool HasAttribute(AstFrameSet* frameSetPtr, const char* attribute)
{
    return astHasAttribute(frameSetPtr, attribute);
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

int Distance2D(AstFrame* astFramePtr, double xStart, double yStart, double xEnd, double yEnd, double* distance)
{
    if (!astFramePtr)
    {
        return -1;
    }
    int nDims = astGetI(astFramePtr, "Naxes");
    double *startPt = new double[nDims];
    double *endPt = new double[nDims];
    for (int i = 2; i < nDims ; i++)
    {
        startPt[i] = 1;
        endPt[i] = 1;
    }
        startPt[0] = xStart;
        startPt[1] = yStart;
        endPt[0] = xEnd;
        endPt[1] = yEnd;
    *distance = astDistance(astFramePtr, startPt, endPt);
    if (!astOK)
    {
        astClearStatus;
        delete[] startPt;
        delete[] endPt;
        return -1;
    }
    delete[] startPt;
    delete[] endPt;
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
    AstCmpFrame* cmpFrameFrom = static_cast<AstCmpFrame*>(astGetFrame(frameSetPtr, 2));
    AstCmpFrame* cmpFrameTo = nullptr;
    cmpFrameTo = static_cast<AstCmpFrame*> astCopy(frameSetPtr);
    if (!cmpFrameTo)
    {
        return 1;
    }

    char buffer[128];
    if (specSysTo) {
        snprintf(buffer, sizeof(buffer), "System(3)=%s", specSysTo);
        astSet(cmpFrameTo, buffer);
    }
    if (specUnitTo) {
        snprintf(buffer, sizeof(buffer), "Unit(3)=%s", specUnitTo);
        astSet(cmpFrameTo, buffer);
    }
    if (specRestTo) {
        snprintf(buffer, sizeof(buffer), "StdOfRest(3)=%s", specRestTo);
        astSet(cmpFrameTo, buffer);
    }

    AstFrameSet *cvt;
    cvt = static_cast<AstFrameSet*>astConvert(cmpFrameFrom, cmpFrameTo, "");
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

int Invert(AstFrameSet* src)
{
    astInvert(src);
    if (!astOK)
    {
        astClearStatus;
        return -1;
    }
    return 0;
}

void AstEnd()
{
    astEnd;
}

void FreeAstMemory(void* ptrToDelete)
{
    delete[] ptrToDelete;
}
