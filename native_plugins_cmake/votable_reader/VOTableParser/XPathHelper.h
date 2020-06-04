/*
 * Persistent Systems Private Limited, Pune, India (website - http://www.pspl.co.in)
 *
 * Permission is hereby granted, without written agreement and without
 * license or royalty fees, to use, copy, modify, and distribute this
 * software and its documentation for any purpose, provided that this
 * copyright notice and the following paragraph appears in all
 * copies of this software.
 *
 * DISCLAIMER OF WARRANTY.
 * -----------------------
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND.
 * IN NO EVENT SHALL THE RIGHTHOLDER BE LIABLE FOR ANY CLAIM, DAMAGES OR
 * OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
 * ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 */
/*
* Class XPathHelper
* This class talks directly to the VOTable XML document.
* This class is used for getting all required information
* such as VTable, MetaData, and Data from the XML document.
*
* Used internally.
*/
#ifndef XPATH_HELPER_H
#define XPATH_HELPER_H

// Base header file.  Must be first.
#include "xalanc/Include/PlatformDefinitions.hpp"
#include <cassert>

#if defined(XALAN_CLASSIC_IOSTREAMS)
#include <iostream.h>
#else
#include <iostream>
#endif

#include "xercesc/util/PlatformUtils.hpp"
#include "xercesc/util/Base64.hpp"
#include "xercesc/util/XMLString.hpp"
#include "xercesc/framework/LocalFileInputSource.hpp"
#include "xercesc/framework/MemBufInputSource.hpp"

#include "xalanc/XPath/XObject.hpp"
#include "xalanc/XPath/XPathEvaluator.hpp"
#include "xalanc/XPath/NodeRefListBase.hpp"

#include "xalanc/XalanDOM/XalanDocument.hpp"
#include "xalanc/XalanDOM/XalanDOMString.hpp"
#include "xalanc/XalanDOM/XalanElement.hpp"
#include "xalanc/XalanDOM/XalanNamedNodeMap.hpp"
#include "xalanc/XalanDOM/XalanNode.hpp"
#include "xalanc/XalanDOM/XalanNodeList.hpp"

#include "xalanc/DOMSupport/DOMServices.hpp"

#include "xalanc/XalanSourceTree/XalanSourceTreeDOMSupport.hpp"
#include "xalanc/XalanSourceTree/XalanSourceTreeInit.hpp"
#include "xalanc/XalanSourceTree/XalanSourceTreeParserLiaison.hpp"


#include <string.h>
#include "global.h"
#include "VTable.h"
#include "Resource.h"
#include "Group.h"

const int MAX_XPATH_EXPR_LEN = 256;

// Added by Sonali on 31 May 2004 while
// compiling on Xalan 1.8.0 and Xerces 2.5.0
using xercesc::LocalFileInputSource;
using xercesc::XMLPlatformUtils;
using xercesc::XMLException;

using xalanc::XalanDocument;
using xalanc::XalanDOMString;
using xalanc::XalanNode;
using xalanc::XalanSourceTreeInit;
using xalanc::XalanSourceTreeDOMSupport;
using xalanc::XalanSourceTreeParserLiaison;
using xalanc::XObjectPtr;
using xalanc::XObject;
using xalanc::XalanNamedNodeMap;
using xalanc::XalanDOMChar;
using xalanc::NodeRefListBase;


class XPathHelper {

public:

	XPathHelper(void);
	int getVTableTree(VTable &v, const char * filename,
		const char * xpath, int *status);
	int getResourceTree(Resource &r, const char * filename,
		const char * xpath, int *status);

	int getVTableTree(VTable &v, const char * buffer, int bufferLength,
				const char * systemID, const char * xpath, int *status);
	int getResourceTree(Resource &r, const char * buffer, int bufferLength,
				const char * systemID, const char * xpath, int *status);


private:

	int extractResourceInfo(XObjectPtr  xObj, Resource &r, int *status);
	int extractTableInfo(XObjectPtr node, VTable &v, int *status);
	int getTable(const XalanNode * node, VTable &v, int *status);
	int getFieldParam(const XalanNode *siblingNode, FieldParam *f, bool isField, int *status);
	int getFieldParamRef(const XalanNode *node, FieldParamRef *fp, int *status);
	int getFieldParamRefAttributes(const XalanNode *node, FieldParamRef *fp, int *status);
	int getLink(const XalanNode *siblingNode, Link &link, int *status);
	char* getDescription(const XalanNode *siblingNode, int *status);
	int getTableAttributes( const XalanNode *node, VTable &v, int *status);
	int getFieldParamAttributes(const XalanNode *node, FieldParam *f, bool isField, int * status);
	int getValuesAttributes(const XalanNode *node, Values &v, int * status);
	char * getCharString(XalanDOMString str);
	XMLByte * getXMLByteString(XalanDOMString str);
	unsigned short * getShortString(XalanDOMString str, int &size);
	int getValues(const XalanNode *node, Values &v, int *status);
	int getOptionAttributes(const XalanNode *node, Option &o, int *status);
	Range * getRange(const XalanNode *node, int *status);
	int getRows(const XalanNode *node, vector<Row> &r, const vector<Field> &fields, int *status);
	int getColumnAttributes(const XalanNode *node, Column &c, int *status);
	Range getRangeAttributes(const XalanNode *node, int *status);
	int getData(const XalanNode *node, VTable &v, vector<Field> &fields, int *status);
	int getOption(const XalanNode *node, Option &o, int *status);
	int getRow(const XalanNode *node, Row &r, const vector<Field> &fields, int *status);
	int getColumn(const XalanNode *node, Column &c, bool isUnicode, const vector<Field> &fields, int columnNum, int *status);
	int getBinaryData(const XalanNode *node, vector<Row> &r, BinaryData &bd, const vector<Field> &fields, int *status);
	int getBinaryStream(const XalanNode *node, vector<Row> &r, Stream &st, const vector<Field> &fields, int *status);
	int getFitsData(const XalanNode *node, FitsData &bd, const vector<Field> &fields, int *status);
	int getFitsStream(const XalanNode *node, Stream &st, const vector<Field> &fields, int *status);
	int getStreamAttributes(const XalanNode *node, Stream &s, int *status);
	int getFitsAttributes(const XalanNode *node, FitsData &fd, int *status);
	int getResourceAttributes(const XalanNode *node, Resource &r, int *status);
	int getGroupAttributes(const XalanNode *node, Group &g, int *status);
	int getInfoAttributes(const XalanNode *node, Info &i, int *status);
	int getCoosysAttributes(const XalanNode *node, Coosys &c, int *status);
	int getResource(const XalanNode* node, Resource &r, int * status);
	int getGroup(const XalanNode* node, Group &g, int * status);
	int getInfo(const XalanNode *node, Info &i, int *status);
	int getCoosys(const XalanNode *node, Coosys &c, int *status);
	int copyString(char * strDest, const char * strSrc, unsigned int numBytes);
	

};


#endif
