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
#include "XPathHelper.h"
#include "InternalConstants.h"
#include <string.h>
#include <float.h>

#include <fstream>
#include <iostream>
#include <vector>
using namespace std;

//XALAN_USING_STD(cerr)
//XALAN_USING_STD(endl)
//XALAN_USING_STD(ostream)

//#define DEBUG_MODE 1

const char VOTABLE_ROOT[] = "/VOTABLE";


/*int
main(
			int				argc,
			const char*		argv[])
{
	int status;
	
	if (argc != 3)
	{
		cout << "Usage TestVOTableAPI xmlfilepath xpath" << endl;
		return 0;
	}

	Resource res1;
	
	res1.openFile(argv[1], argv[2], 0,  &status);
	//cout << endl << "Output of Test Program starts here *****************" << endl;
	cout << "Status of Resource openFile is  " << status << endl;

	res1.closeFile (&status);
	
	cout << endl << "Completed operation - exiting now." << endl;

	return 0;
}

*/                                

XPathHelper::XPathHelper(void)
{
	// do nothing
}

/*
* Get subtree from the whole VOTable document.
*/
int XPathHelper::getResourceTree(Resource &r, const char * filename, const char * xpath, int *status)
{
	
#ifdef  DEBUG_MODE
				cout << "File is " << filename << endl;
#endif
	
#if !defined(XALAN_NO_NAMESPACES)
	using std::cerr;
	using std::cout;
	using std::endl;
#endif

	
	try
		{

			// Added by Sonali on 31 May 2004 while
			// compiling on Xalan 1.8.0 and Xerces 2.5.0
		using xercesc::XMLPlatformUtils;
		using xalanc::XPathEvaluator;


			XMLPlatformUtils::Initialize();

			XPathEvaluator::initialize();

			{	
				// Just hoist everything
				using namespace xalanc;

				// Added by Sonali on 31 May 2004 while
				// compiling on Xalan 1.8.0 and Xerces 2.5.0
				using xercesc::MemBufInputSource;

				int result = VOERROR;			

				int		theResultInt = 0;
				
				char xpathExpr[MAX_XPATH_EXPR_LEN];

				strcpy(xpathExpr, VOTABLE_ROOT);
				strcat(xpathExpr, xpath);
				const XObjectPtr xPtr(NULL);


				// Initialize the XalanSourceTree subsystem...
				XalanSourceTreeInit		theSourceTreeInit;

				// We'll use these to parse the XML file.
				XalanSourceTreeDOMSupport		theDOMSupport;
				XalanSourceTreeParserLiaison	theLiaison(theDOMSupport);

				// Hook the two together...
				theDOMSupport.setParserLiaison(&theLiaison);

				const XalanDOMString	theFileName(filename);

				// Create an input source that represents a local file...				
				const LocalFileInputSource	theInputSource(theFileName.c_str());

				
				// Parse the document...
				XalanDocument* const	theDocument =
						theLiaison.parseXMLStream(theInputSource);
				assert(theDocument != 0);

				XPathEvaluator	theEvaluator;

				// OK, let's find the context node...
				XalanNode* const	theContextNode =
						theEvaluator.selectSingleNode(
							theDOMSupport,
							theDocument,
							XalanDOMString("/").c_str(),
							theDocument->getDocumentElement());

				if (theContextNode == 0)
				{
					cerr << "Warning -- No nodes matched the location path \""
						 << "/"
						 << "\"."
						 << endl
						 << "Execution cannot continue..."
						 << endl
						 << endl;

					//XPathEvaluator::terminate();

					//XMLPlatformUtils::Terminate();
				}
				else
				{
					// OK, let's evaluate the expression...
					XObjectPtr *xPtr =  new XObjectPtr(
						theEvaluator.evaluate(
								theDOMSupport,
								theContextNode,
								XalanDOMString(xpathExpr).c_str(),
								theDocument->getDocumentElement()));

					assert((*xPtr).null() == false);
					
					extractResourceInfo(*xPtr, r, status);


#ifdef  DEBUG_MODE
					cout << "Got RESOURCE from the file." << endl;
#endif

					delete xPtr;
			

				
				}
			}
		

			XPathEvaluator::terminate();

			XMLPlatformUtils::Terminate();
		}
		
		catch(...)
		{
			*status = PARSING_ERROR;
			cerr << "Exception caught !!" << endl;
		}
	
	
	cout.flush();
	//return *(new XObjectPtr());
	return *status;
	
}



/*
* Get subtree from the whole VOTable document.
*/
int XPathHelper::getResourceTree(Resource &r, const char * buffer, int bufferLength,
				const char * systemID, const char * xpath, int *status)
{
	
	
#if !defined(XALAN_NO_NAMESPACES)
	using std::cerr;
	using std::cout;
	using std::endl;
#endif

	
	try
		{

			// Added by Sonali on 31 May 2004 while
			// compiling on Xalan 1.8.0 and Xerces 2.5.0
		using xercesc::XMLPlatformUtils;
		using xalanc::XPathEvaluator;


			XMLPlatformUtils::Initialize();

			XPathEvaluator::initialize();

			{	
				// Just hoist everything
				using namespace xalanc;

				// Added by Sonali on 31 May 2004 while
				// compiling on Xalan 1.8.0 and Xerces 2.5.0
				using xercesc::MemBufInputSource;

				int result = VOERROR;			

				int		theResultInt = 0;
				
				char xpathExpr[MAX_XPATH_EXPR_LEN];

				strcpy(xpathExpr, VOTABLE_ROOT);
				strcat(xpathExpr, xpath);
				const XObjectPtr xPtr(NULL);


				// Initialize the XalanSourceTree subsystem...
				XalanSourceTreeInit		theSourceTreeInit;

				// We'll use these to parse the XML file.
				XalanSourceTreeDOMSupport		theDOMSupport;
				XalanSourceTreeParserLiaison	theLiaison(theDOMSupport);

				// Hook the two together...
				theDOMSupport.setParserLiaison(&theLiaison);

				/*		
				FILE    *fileF;
				size_t  t;

				fileF = fopen( filename, "rb" );
				if (fileF == 0) {
					fprintf(stderr, "Can not open file \"%s\".\n", filename);
					exit(-1);
				}
				fseek(fileF, 0, SEEK_END);
				int len = ftell(fileF);
				fseek(fileF, 0, SEEK_SET);
				char * fileContent = new char[len + 1];
				t = fread(fileContent, 1, len, fileF);
				if (t != len) {
					fprintf(stderr, "Error reading file \"%s\".\n", filename);
					exit(-1);
				}
				fclose(fileF);
				fileContent[len] = 0;
        
				cout << len << endl;
				//Should output 'this'
				cout<< fileContent <<"\n";

				const char systemId[] = {'R', 'E', 'S', 'P', 'O', 'N', 'S', 'E', '\0'};

				*/
				const MemBufInputSource theInputSource((const XMLByte *)buffer, bufferLength, systemID);  


				// Parse the document...
				XalanDocument* const	theDocument =
						theLiaison.parseXMLStream(theInputSource);
				assert(theDocument != 0);

				XPathEvaluator	theEvaluator;

				// OK, let's find the context node...
				XalanNode* const	theContextNode =
						theEvaluator.selectSingleNode(
							theDOMSupport,
							theDocument,
							XalanDOMString("/").c_str(),
							theDocument->getDocumentElement());

				if (theContextNode == 0)
				{
					cerr << "Warning -- No nodes matched the location path \""
						 << "/"
						 << "\"."
						 << endl
						 << "Execution cannot continue..."
						 << endl
						 << endl;

					//XPathEvaluator::terminate();

					//XMLPlatformUtils::Terminate();
				}
				else
				{
					// OK, let's evaluate the expression...
					XObjectPtr *xPtr =  new XObjectPtr(
						theEvaluator.evaluate(
								theDOMSupport,
								theContextNode,
								XalanDOMString(xpathExpr).c_str(),
								theDocument->getDocumentElement()));

					assert((*xPtr).null() == false);
					
					extractResourceInfo(*xPtr, r, status);


#ifdef  DEBUG_MODE
					cout << "Got RESOURCE from the file." << endl;
#endif

					delete xPtr;
			

				
				}
			}
		

			XPathEvaluator::terminate();

			XMLPlatformUtils::Terminate();
		}
		
		catch(...)
		{
			*status = PARSING_ERROR;
			cerr << "Exception caught !!" << endl;
		}
	
	
	cout.flush();
	//return *(new XObjectPtr());
	return *status;
	
}



/*
* Get subtree from the whole VOTable document.
*/
int XPathHelper::getVTableTree(VTable &v, const char * filename, const char * xpath, int *status)
{

#ifdef  DEBUG_MODE
	cout << "File is " << filename << endl;
#endif
	
#if !defined(XALAN_NO_NAMESPACES)
	using std::cerr;
	using std::cout;
	using std::endl;
#endif

	
	try
		{
			// Added by Sonali on 31 May 2004 while
			// compiling on Xalan 1.8.0 and Xerces 2.5.0

		using xercesc::XMLPlatformUtils;
		using xalanc::XPathEvaluator;

			XMLPlatformUtils::Initialize();

			XPathEvaluator::initialize();

			{				
			
				// Just hoist everything
				using namespace xalanc;

					// Added by Sonali on 31 May 2004 while
					// compiling on Xalan 1.8.0 and Xerces 2.5.0
					using xercesc::MemBufInputSource;

				int result = VOERROR;			

				int		theResultInt = 0;
				
				char xpathExpr[MAX_XPATH_EXPR_LEN];

				strcpy(xpathExpr, VOTABLE_ROOT);
				strcat(xpathExpr, xpath);
				const XObjectPtr xPtr(NULL);
				

				// Initialize the XalanSourceTree subsystem...
				XalanSourceTreeInit		theSourceTreeInit;

				// We'll use these to parse the XML file.
				XalanSourceTreeDOMSupport		theDOMSupport;
				XalanSourceTreeParserLiaison	theLiaison(theDOMSupport);

				// Hook the two together...
				theDOMSupport.setParserLiaison(&theLiaison);
				const XalanDOMString	theFileName(filename);

				// Create an input source that represents a local file...				
				const LocalFileInputSource	theInputSource(theFileName.c_str());

				// Parse the document...
				XalanDocument* const	theDocument =
						theLiaison.parseXMLStream(theInputSource);
				assert(theDocument != 0);

				XPathEvaluator	theEvaluator;

				// OK, let's find the context node...
				XalanNode* const	theContextNode =
						theEvaluator.selectSingleNode(
							theDOMSupport,
							theDocument,
							XalanDOMString("/").c_str(),
							theDocument->getDocumentElement());

				if (theContextNode == 0)
				{
					cerr << "Warning -- No nodes matched the location path \""
						 << "/"
						 << "\"."
						 << endl
						 << "Execution cannot continue..."
						 << endl
						 << endl;

					//XPathEvaluator::terminate();

					//XMLPlatformUtils::Terminate();
				}
				else
				{
					// OK, let's evaluate the expression...
					XObjectPtr *xPtr =  new XObjectPtr(
						theEvaluator.evaluate(
								theDOMSupport,
								theContextNode,
								XalanDOMString(xpathExpr).c_str(),
								theDocument->getDocumentElement()));

					assert((*xPtr).null() == false);
				
					extractTableInfo(*xPtr, v, status);
				
					delete xPtr;				
				}
			}
		

			XPathEvaluator::terminate();

			XMLPlatformUtils::Terminate();
		}
		
		catch(...)
		{
			*status = PARSING_ERROR;
			cerr << "Exception caught !!" << endl;
		}
	
	
	cout.flush();
	//return *(new XObjectPtr());
	return *status;
	
}


/*
* Get subtree from the whole VOTable document.
*/
int XPathHelper::getVTableTree(VTable &v, const char * buffer, int bufferLength,
				const char * systemID, const char * xpath, int *status)
{

#if !defined(XALAN_NO_NAMESPACES)
	using std::cerr;
	using std::cout;
	using std::endl;
#endif

	
	try
		{
			// Added by Sonali on 31 May 2004 while
			// compiling on Xalan 1.8.0 and Xerces 2.5.0

		using xercesc::XMLPlatformUtils;
		using xalanc::XPathEvaluator;

			XMLPlatformUtils::Initialize();

			XPathEvaluator::initialize();

			{				
			
				// Just hoist everything
				using namespace xalanc;

				// Added by Sonali on 31 May 2004 while
				// compiling on Xalan 1.8.0 and Xerces 2.5.0
				using xercesc::MemBufInputSource;

				int result = VOERROR;			

				int		theResultInt = 0;
				
				char xpathExpr[MAX_XPATH_EXPR_LEN];

				strcpy(xpathExpr, VOTABLE_ROOT);
				strcat(xpathExpr, xpath);
				const XObjectPtr xPtr(NULL);
				

				// Initialize the XalanSourceTree subsystem...
				XalanSourceTreeInit		theSourceTreeInit;

				// We'll use these to parse the XML file.
				XalanSourceTreeDOMSupport		theDOMSupport;
				XalanSourceTreeParserLiaison	theLiaison(theDOMSupport);

				// Hook the two together...
				theDOMSupport.setParserLiaison(&theLiaison);
				
				const MemBufInputSource theInputSource((const XMLByte *)buffer, bufferLength, systemID);  

				// Parse the document...
				XalanDocument* const	theDocument =
						theLiaison.parseXMLStream(theInputSource);
				assert(theDocument != 0);

				XPathEvaluator	theEvaluator;

				// OK, let's find the context node...
				XalanNode* const	theContextNode =
						theEvaluator.selectSingleNode(
							theDOMSupport,
							theDocument,
							XalanDOMString("/").c_str(),
							theDocument->getDocumentElement());

				if (theContextNode == 0)
				{
					cerr << "Warning -- No nodes matched the location path \""
						 << "/"
						 << "\"."
						 << endl
						 << "Execution cannot continue..."
						 << endl
						 << endl;

					//XPathEvaluator::terminate();

					//XMLPlatformUtils::Terminate();
				}
				else
				{
					// OK, let's evaluate the expression...
					XObjectPtr *xPtr =  new XObjectPtr(
						theEvaluator.evaluate(
								theDOMSupport,
								theContextNode,
								XalanDOMString(xpathExpr).c_str(),
								theDocument->getDocumentElement()));

					assert((*xPtr).null() == false);
				
					extractTableInfo(*xPtr, v, status);
				
					delete xPtr;				
				}
			}
		

			XPathEvaluator::terminate();

			XMLPlatformUtils::Terminate();
		}
		
		catch(...)
		{
			*status = PARSING_ERROR;
			cerr << "Exception caught !!" << endl;
		}
	
	
	cout.flush();
	//return *(new XObjectPtr());
	return *status;
	
}



/*
* Extract information into VTable from the DOM tree.
*/
int XPathHelper::extractResourceInfo(XObjectPtr  xObj, Resource &r, int *status)
{
	int result = SUCCESS;

	
	// check if the node is an element.
	int nodeType = xObj->getType () ;
	
	//std::cout << "Node Type is " << nodeType << endl;

	if (nodeType == XObject::eTypeNodeSet)
	{
		const NodeRefListBase& nodeset = xObj->nodeset();		
		size_t len = nodeset.getLength();

#ifdef  DEBUG_MODE
		cout << "Number of Resource nodes are " << len << endl;
#endif

		if (0 == len)
		{
			result = XPATH_RESOURCE_ERROR;
		}

		for (size_t i=0; i<len; i++)
		{
			XalanNode* const	node = nodeset.item(i);
			XalanDOMString		str;

			const XalanDOMString tbc = ((XalanDOMString)ELE_RESOURCE);


			const int theType = node->getNodeType();
			//std::cout << "Node Type is " << theType << endl;

			
			if (theType == XalanNode::ELEMENT_NODE) 
			{
				
				str = node->getNodeName();
#ifdef  DEBUG_MODE
				cout << "Node name is " << str << endl;
#endif
				// check if element is table
				if (str.compare(tbc) == 0)
				{	
					try 
					{
						getResource(node, r, &result);								
					} 
					catch (bad_alloc ex)
					{
						result = INSUFFICIENT_MEMORY_ERROR;
					}
				
					break;
				} 
				else
				{

#ifdef  DEBUG_MODE
					cout << "Node is not RESOURCE, returning ERROR ."<< endl;
#endif

					result = XPATH_RESOURCE_ERROR;
				}
			}
			else
			{
				result = XPATH_RESOURCE_ERROR;
			}			
		}

	} 
	else 
	{
		result = XPATH_RESOURCE_ERROR;
#ifdef  DEBUG_MODE
		cout << "There is no node type in this Object.";
#endif
	}
	
	*status = result;
	return *status;

}

/*
* Extract information into VTable from the DOM tree.
*/
int XPathHelper::extractTableInfo(XObjectPtr xObj, VTable &vTable, int *status)
{
	int result = SUCCESS;

	
	
	// check if the node is an element.
	int nodeType = xObj->getType () ;
	
	//std::cout << "Node Type is " << nodeType << endl;

	if (nodeType == XObject::eTypeNodeSet)
	{
		const NodeRefListBase& nodeset = xObj->nodeset();
		size_t len = nodeset.getLength();

#ifdef  DEBUG_MODE
		cout << "Number of TABLE nodes are " << len << endl;
#endif
		if (0 == len)
		{
			result = XPATH_VTABLE_ERROR;
		}

		for (size_t i=0; i<len; i++)
		{
			XalanNode* const	node = nodeset.item(i);
			XalanDOMString		str;

			const int theType = node->getNodeType();
			//std::cout << "Node Type is " << theType << endl;

			
			if (theType == XalanNode::ELEMENT_NODE) 
			{
				
				str = node->getNodeName();
				//cout << "Node name is " << str << endl;
				// check if element is table
				if (str.compare(((const XalanDOMString)ELE_TABLE)) == 0)
				{	
					try 
					{
						getTable(node, vTable,  &result);								
					}
					catch (bad_alloc ex)
					{
						result = INSUFFICIENT_MEMORY_ERROR;
					}
					break;
				} 
				else
				{
#ifdef  DEBUG_MODE
					cout << "Node is not TABLE, returning ERROR ."<< endl;
#endif

					result = XPATH_VTABLE_ERROR;
				}
			}
			else
			{
				result = XPATH_VTABLE_ERROR;
			}			
		}

	} 
	else 
	{
		result = XPATH_VTABLE_ERROR;
#ifdef  DEBUG_MODE
		cout << "There is no node type in this Object.";
#endif
	}
	
	*status = result;
	return *status;

}

/*
* Process the child nodes of Resource such as Param, Link, Description etc.
* and fill them up.
*/
int XPathHelper::getResource(const XalanNode * node, Resource &r, int * status)
{

	// get attributes
	getResourceAttributes(node, r, status);

	XalanNode * const childNode = node->getFirstChild();

	if (childNode == NULL)
	{
#ifdef  DEBUG_MODE
		cout << "Resource has no child nodes." << endl;
#endif
		*status = SUCCESS;
		return *status;
	}

	//cout << "First node name is " << childNode->getNodeName() << endl;
	//cout << "First node type is " << childNode->getNodeType() << endl;

	//XalanNode * const newSiblingNode = childNode;
	const XalanNode * siblingNode;
	int nodes = 0, eleNodes = 0;
	XalanDOMString	name;
	int result = 0;
	char *desc = NULL;

	siblingNode = childNode;
	vector<Info> infos;
	vector<Coosys> coosysList;
	vector<Param> params;
	vector<Link> links;
	vector<VTable> vtables;
	vector<Resource> resources;

	//get all siblings (not using getChildNodes since it is not working) :-(
	while (siblingNode != NULL) 
	{
		// process only element nodes, not text nodes.
		// To do - find some way of avoiding text nodes.

		//cout << "Next node name is " << siblingNode->getNodeName() << endl;
		//cout << "Next node type is " << siblingNode->getNodeType() << endl;
		if (siblingNode->getNodeType() == XalanNode::ELEMENT_NODE)
		{
			name = siblingNode->getNodeName();
#ifdef  DEBUG_MODE
			cout << "Node name is " << siblingNode->getNodeName() << endl;
#endif

			// check if node is info node
			if (name.compare(((const XalanDOMString)ELE_INFO)) == 0) {
				Info i;

				getInfo(siblingNode, i, &result);
				infos.push_back(i);
			} else if (name.compare(((const XalanDOMString)ELE_COOSYS)) == 0) { // check if node is coosys node
				Coosys c;
				getCoosys(siblingNode, c, &result);

				coosysList.push_back(c);
			} else if (name.compare(((const XalanDOMString)ELE_PARAM)) == 0) { // check if node is param node
				Param p;
				getFieldParam(siblingNode, &p, false, &result);
				params.push_back(p);
			} else if (name.compare(((const XalanDOMString)ELE_DESC)) == 0) { // check if node is description
				desc = getDescription(siblingNode, &result);
			} else if (name.compare(((const XalanDOMString)ELE_LINK)) == 0) { // check if node is link node
				Link l;
				getLink(siblingNode,l, &result);

				links.push_back(l);
			} else if (name.compare(((const XalanDOMString)ELE_TABLE)) == 0) { // check if node is table node
				VTable v;
				getTable(siblingNode, v, &result);
				vtables.push_back(v);
			} else if (name.compare(((const XalanDOMString)ELE_RESOURCE)) == 0)	{
				Resource r;
				getResource(siblingNode,r, &result);
				resources.push_back(r);
			}
		}
		//XalanNode * const newSiblingNode = siblingNode->getNextSibling();
		siblingNode = siblingNode->getNextSibling();

	}

	r.setCoosystems(coosysList, &result);
	r.setInfos (infos, &result);
	r.setParams (params, &result);
	r.setLinks (links, &result);
	r.setTables (vtables, &result);
	r.setResources (resources, &result);
	r.setDesc (desc, &result);


#ifdef  DEBUG_MODE
	cout << "Resource is parsed." << endl;
#endif

	*status = SUCCESS;
	return SUCCESS;

}


/*
* Process the child nodes of Table such as Field, Link, Description etc.
* and fill them up.
*/
int XPathHelper::getTable(const XalanNode* node, VTable &v, int * status)
{

	getTableAttributes(node, v, status);

	const XalanNode * childNode = node->getFirstChild();

	if (childNode == NULL)
	{
#ifdef  DEBUG_MODE
		cout << "Table has no child nodes." << endl;
#endif
		*status = VOERROR;
		return *status;
	}

	//cout << "First node name is " << childNode->getNodeName() << endl;
	//cout << "First node type is " << childNode->getNodeType() << endl;

	const XalanNode * siblingNode;
	//int nodes = 0, eleNodes = 0;
	XalanDOMString	name;
	int result = 0;
	char *desc = NULL;

	siblingNode = childNode;
	vector<Field> fields;
	vector<Param> params;
	vector<Link> links;
	vector<Group> groups;
	TableMetaData tmd;
	
	bool isField = true;


	//get all siblings (not using getChildNodes since it is not working) :-(
	while (siblingNode != NULL) 
	{
		// process only element nodes, not text nodes.
		// To do - find some way of avoiding text nodes.

		//cout << "Next node name is " << siblingNode->getNodeName() << endl;
		//cout << "Next node type is " << siblingNode->getNodeType() << endl;
		if (siblingNode->getNodeType() == XalanNode::ELEMENT_NODE)
		{

			name = siblingNode->getNodeName();
#ifdef  DEBUG_MODE
			cout << "Node name is " << siblingNode->getNodeName() << endl;
#endif


			// check if node is field node
			if (name.compare(((const XalanDOMString)ELE_FIELD)) == 0) {
				Field f;
				//cout << "Going to get field" << endl;
				getFieldParam(siblingNode,&f,isField,&result);
				//cout << "returned from field" << endl;
				fields.push_back(f);			
				
			} 
			// check if node is param node
			else if (name.compare(((const XalanDOMString)ELE_PARAM)) == 0) {
				Param p;
				getFieldParam(siblingNode,&p,false,&result);
				params.push_back(p);
			}
			else if (name.compare(((const XalanDOMString)ELE_GROUP)) == 0) { // check if node is Group
				Group g;
				getGroup(siblingNode,g,&result);
				groups.push_back(g);
			}
			else if (name.compare(((const XalanDOMString)ELE_LINK)) == 0) { // check if node is link node
				Link l;

				getLink(siblingNode, l, &result);

				links.push_back(l);
			} else if (name.compare(((const XalanDOMString)ELE_DESC)) == 0) { // check if node is description
				desc = getDescription(siblingNode, &result);
			} else if (name.compare(((const XalanDOMString)ELE_DATA)) == 0)	{
				// get data
				getData(siblingNode, v, fields, &result);

			}
		
		//	eleNodes++;
		}
		
		siblingNode = siblingNode->getNextSibling();
	//	nodes++;

	}



	//cout << "Total no. is nodes is " << nodes << endl;
	tmd.setDesciption(desc, &result);
	tmd.setFields(fields, &result);
	tmd.setParams(params, &result);
	tmd.setLinks(links, &result);
	tmd.setGroups(groups, &result);
	v.setMetaData(tmd, &result);
	

	

#ifdef  DEBUG_MODE
	cout << "Filled the Table node." << endl;
#endif

	*status = SUCCESS;
	return *status;

}



/*
* Process the child nodes of Group such as Param, FieldRef, Description etc.
* and fill them up.
*/
int XPathHelper::getGroup(const XalanNode * node, Group &g, int * status)
{

	// get attributes
	getGroupAttributes(node, g, status);

	XalanNode * const childNode = node->getFirstChild();

	if (childNode == NULL)
	{
#ifdef  DEBUG_MODE
		cout << "Group has no child nodes." << endl;
#endif
		*status = SUCCESS;
		return *status;
	}

	//cout << "First node name is " << childNode->getNodeName() << endl;
	//cout << "First node type is " << childNode->getNodeType() << endl;

	//XalanNode * const newSiblingNode = childNode;
	const XalanNode * siblingNode;
	int nodes = 0, eleNodes = 0;
	XalanDOMString	name;
	int result = 0;
	char *desc = NULL;

	siblingNode = childNode;
	vector<Param> params;
	vector<ParamRef> paramRefs;
	vector<FieldRef> fieldRefs;
	vector<Group> groups;

	//get all siblings (not using getChildNodes since it is not working) :-(
	while (siblingNode != NULL) 
	{
		// process only element nodes, not text nodes.
		// To do - find some way of avoiding text nodes.

		//cout << "Next node name is " << siblingNode->getNodeName() << endl;
		//cout << "Next node type is " << siblingNode->getNodeType() << endl;
		if (siblingNode->getNodeType() == XalanNode::ELEMENT_NODE)
		{
			name = siblingNode->getNodeName();
#ifdef  DEBUG_MODE
			cout << "Node name is " << siblingNode->getNodeName() << endl;
#endif

			if (name.compare(((const XalanDOMString)ELE_PARAM)) == 0) { // check if node is param node
				Param p;
				getFieldParam(siblingNode, &p, false, &result);
				params.push_back(p);
			} else if (name.compare(((const XalanDOMString)ELE_DESC)) == 0) { // check if node is description
				desc = getDescription(siblingNode, &result);
			} else if (name.compare(((const XalanDOMString)ELE_PARAM_REF)) == 0) { // check if node is param ref node
				ParamRef p;
				getFieldParamRef(siblingNode, &p, &result);
				paramRefs.push_back(p);
			} else if (name.compare(((const XalanDOMString)ELE_FIELD_REF)) == 0) { // check if node is field ref node
				FieldRef f;
				getFieldParamRef(siblingNode, &f, &result);
				fieldRefs.push_back(f);
			} else if (name.compare(((const XalanDOMString)ELE_GROUP)) == 0)	{
				Group grp;
				getGroup(siblingNode,grp, &result);
				groups.push_back(grp);
			}
		}
		//XalanNode * const newSiblingNode = siblingNode->getNextSibling();
		siblingNode = siblingNode->getNextSibling();

	}

	g.setParams (params, &result);
	g.setParamRefs (paramRefs, &result);
	g.setFieldRefs (fieldRefs, &result);
	g.setGroups (groups, &result);
	g.setDescription (desc, &result);

	*status = SUCCESS;
	return SUCCESS;

}




/*
* Convert from XalanDOMString to char *.
* All the classes of VTable internally make use of char * 
* and not XalanDOMChar * to store strings. Need an explicit 
* conversion from 'short' to 'char'.
*/
char * XPathHelper::getCharString(XalanDOMString str)
{
	if (str.size() == 0) 
	{ 
		return NULL;
	}

	int length = str.size();
	char * charStr;
	//cout << "Length of " << str << " is = " << length << endl;

	//try {
		charStr = new char[length + 1];
	//} 
	//catch (bad_alloc ex)
	//{
//#ifdef  DEBUG_MODE
//	cout << "Cannot allocate required memory." << endl;
//#endif
		// Cannot allocate memory, exit ?
	//	return NULL;
	//}
	charStr[length] = 0;

	const XalanDOMChar * xdc = str.c_str();
	int i = 0;
	while (i < length)
	{
		// explicit conversion from 'short' to 'char'.
		charStr[i] = (char) xdc[i];
		i++;
	}
	charStr[length] = 0;

	//cout << "The returned string is = " << charStr << endl;

	return charStr;

}


/*
* Convert from XalanDOMString to XMLByte *.
* All the classes of VTable internally make use of char * 
* and not XalanDOMChar * to store strings. Need an explicit 
* conversion from 'short' to 'char'.
*/
XMLByte * XPathHelper::getXMLByteString(XalanDOMString str)
{
	if (str.size() == 0) 
	{ 
		return NULL;
	}

	int length = str.size();
	XMLByte * byteStr;
	//cout << "Length of " << str << " is = " << length << endl;

	//try {
		byteStr = new XMLByte[length + 1];
	//} 
	//catch (bad_alloc ex)
	//{
//#ifdef  DEBUG_MODE
//	cout << "Cannot allocate required memory." << endl;
//#endif
		// Cannot allocate memory, exit ?
	//	return NULL;
	//}
	byteStr[length] = 0;

	const XalanDOMChar * xdc = str.c_str();
	int i = 0;
	while (i < length)
	{
		// explicit conversion from 'short' to 'char'.
		byteStr[i] = (XMLByte) xdc[i];
		i++;
	}
	byteStr[length] = 0;

	//cout << "The returned string is = " << charStr << endl;

	return byteStr;

}

/*
* Convert from XalanDOMString to short *.
* Used for storing Unicode.
*/
unsigned short * XPathHelper::getShortString(XalanDOMString str, int &size)
{
	size = 0; 
	if (str.size() == 0) 
	{ 
		
		return NULL;
	}

	int length = str.size();
	unsigned short * charStr;
#ifdef  DEBUG_MODE
	cout << "Length is = " << length << endl;
#endif

	//try 
	{
		charStr = new unsigned short[length + 1];
	}
	//catch (bad_alloc ex)
	//{
//#ifdef  DEBUG_MODE
//	cout << "Cannot allocate required memory." << endl;
//#endif
//		size = 0;
//		return NULL;
//	}
	charStr[length] = 0;

	const XalanDOMChar * xdc = str.c_str();
	/*int i = 0;
	int j = 0;
	while (i < length)
	{
		charStr[j] = xdc[i];
		charStr[j] = charStr[j] << 8;
		if (i + 1 < length)
		{
			charStr[j] += xdc[i + 1];
		}
		//cout << "charStr[" << j << "] = " << charStr[j];
		i += 2;
		j++;
	}
	charStr[j] = 0;
	cout << " j = " << j << "  and i = " << i << endl;
	cout << "size = " << (length/2 + 2) << endl;
	*/
	

	int i = 0;
	while (i < length)
	{
#ifdef  DEBUG_MODE
		cout << ((char) xdc[i]) << " ";
#endif
		
		charStr[i] = xdc[i];
		i++;
	}

	//cout << "The returned string is = " << charStr << endl;

	size = length + 1;
	//cout << endl << "New Length of " << str << " is = " << j+1 << endl;

	return charStr;

}


/*
* Get dataype of the field.
*/
int getFieldDatatype(char * type)
{
	
	for (int i = 1; i <= NUM_OF_DATATYPES; i++)
	{
		if (strcmp(type, FIELD_DATATYPE[i]) == 0)
		{
	
			return i;
		}
	}
	return 0;

}


/*
* Get system of the coosys.
*/
int getCoosysSystem(char * type)
{
	
	for (int i = 1; i <= NUM_OF_COOSYS_SYSTEMS; i++)
	{
		if (strcmp(type, COOSYS_SYSTEM[i]) == 0)
		{
			return i;
		}
	}
	return 0;

}

/*
* Get type of the field.
*/
int getFieldType(char * type)
{
	for (int i = 1; i <= NUM_OF_FIELD_TYPES; i++)
	{
		if (strcmp(type, FIELD_TYPE[i]) == 0)
		{
			return i;
		}
	}
	return 0;

}

/*
* Get content-role of the link.
*/
int getContentRole(char * role)
{
	for (int i = 1; i <= NUM_OF_CONTENT_ROLES; i++)
	{
		if (strcmp(role, CONTENT_ROLE[i]) == 0)
		{
			return i;
		}
	}
	return 0;

}

/*
* Get attributes of 'Resource'.
*/
int XPathHelper::getResourceAttributes(const XalanNode *node, Resource &r, int *status)
{
	const XalanNamedNodeMap * xnnp = node->getAttributes();
	int numOfAttribs = xnnp->getLength();
	XalanDOMString name, value;
	char *charValue = NULL;
	int result = 0;
	
	//cout << "Number of attributes are = " << numOfAttribs << endl;

	// need to get attributes one by one
	for (int i = 0; i <  numOfAttribs; i++)
	{
		XalanNode * attribNode = xnnp->item(i);

		name = attribNode->getNodeName();
		value = attribNode->getNodeValue();

#ifdef  DEBUG_MODE
		cout << "Resource attribute name is = " << name << endl;
		cout << "Attribute value is = " << value << endl;
#endif
		
		
		if (name.compare(((const XalanDOMString)ATTR_ID)) == 0) 
		{
			// set ID
			charValue = getCharString(value);
			r.setID(charValue, &result);
			
		}
		
		if (name.compare(((const XalanDOMString)ATTR_UTYPE)) == 0) 
		{
			// set utype
			charValue = getCharString(value);
			r.setUtype(charValue, &result);
			
		}
		else if (name.compare(((const XalanDOMString)ATTR_NAME)) == 0)
		{
			// set ID
			charValue = getCharString(value);
			r.setName(charValue, &result);
			
		}
		else if (name.compare(((const XalanDOMString)ATTR_TYPE)) == 0)
		{
			// set ID
			charValue = getCharString(value);
			resource_type t = results;
			if (strcmp(charValue, RESOURCE_META_TYPE) == 0)
			{
				t = meta;
			}
			r.setType(t, &result);
			delete[] charValue;
			
		}
	}

#ifdef  DEBUG_MODE
		cout << "Filled Resource node." << endl;
#endif
	*status = SUCCESS;
	
	return *status;
}



/*
* Get attributes of 'Group'.
*/
int XPathHelper::getGroupAttributes(const XalanNode *node, Group &g, int *status)
{
	const XalanNamedNodeMap * xnnp = node->getAttributes();
	int numOfAttribs = xnnp->getLength();
	XalanDOMString name, value;
	char *charValue = NULL;
	int result = 0;
	
	//cout << "Number of attributes are = " << numOfAttribs << endl;

	// need to get attributes one by one
	for (int i = 0; i <  numOfAttribs; i++)
	{
		XalanNode * attribNode = xnnp->item(i);

		name = attribNode->getNodeName();
		value = attribNode->getNodeValue();

#ifdef  DEBUG_MODE
		cout << "Group attribute name is = " << name << endl;
		cout << "Attribute value is = " << value << endl;
#endif
		
		
		if (name.compare(((const XalanDOMString)ATTR_ID)) == 0) 
		{
			// set ID
			charValue = getCharString(value);
			g.setID(charValue, &result);
			
		}
		
		else if (name.compare(((const XalanDOMString)ATTR_UTYPE)) == 0) 
		{
			// set utype
			charValue = getCharString(value);
			g.setUtype(charValue, &result);
			
		}
		else if (name.compare(((const XalanDOMString)ATTR_NAME)) == 0)
		{
			// set name
			charValue = getCharString(value);
			g.setName(charValue, &result);
			
		}
		else if (name.compare(((const XalanDOMString)ATTR_UCD)) == 0) 
		{
			// set ucd
			charValue = getCharString(value);
			g.setUcd(charValue, &result);
			
		}
		else if (name.compare(((const XalanDOMString)ATTR_REF)) == 0)
		{
			// set ref
			charValue = getCharString(value);
			g.setRef(charValue, &result);
			
		}
	}

#ifdef  DEBUG_MODE
		cout << "Filled Group node." << endl;
#endif
	*status = SUCCESS;
	
	return *status;
}


/*
* Get attributes of Stream.
*/
int XPathHelper::getStreamAttributes(const XalanNode *node, Stream &s, int *status)
{
	const XalanNamedNodeMap * xnnp = node->getAttributes();
	int numOfAttribs = xnnp->getLength();
	XalanDOMString name, value;
	char *charValue = NULL;
	int result = 0;


	//cout << "Number of attributes are = " << numOfAttribs << endl;

	// need to get attributes one by one
	for (int i = 0; i <  numOfAttribs; i++)
	{
		XalanNode * attribNode = xnnp->item(i);

		name = attribNode->getNodeName();
		value = attribNode->getNodeValue();

#ifdef  DEBUG_MODE
		cout << "Coosys attribute name is = " << name << endl;
		cout << "Attribute value is = " << value << endl;
#endif
		
		
		if (name.compare(((const XalanDOMString)ATTR_TYPE_STREAM)) == 0) 
		{
			// set type
			charValue = getCharString(value);
			s.setType(charValue, &result);
			
		} 
		else if (name.compare(((const XalanDOMString)ATTR_HREF_STREAM)) == 0)
		{
			// set href
			charValue = getCharString(value);
			s.setHref(charValue, &result);
			
		}
		else if (name.compare(((const XalanDOMString)ATTR_ENCODING)) == 0)
		{
			// set encoding
			charValue = getCharString(value);
			s.setEncoding(charValue, &result);
			
		}
		else if (name.compare(((const XalanDOMString)ATTR_ACTUATE)) == 0)
		{
			// set system
			charValue = getCharString(value);	
		    s.setActuate(charValue, &result);		
		}

		else if (name.compare(((const XalanDOMString)ATTR_EXPIRES)) == 0)
		{
			// set system
			charValue = getCharString(value);	
		    s.setExpires(charValue, &result);		
		}

		else if (name.compare(((const XalanDOMString)ATTR_RIGHTS)) == 0)
		{
			// set system
			charValue = getCharString(value);	
		    s.setRights(charValue, &result);		
		}
		
	}
	*status = SUCCESS;
	return SUCCESS;
}


/*
* Get attributes of Fits.
*/
int XPathHelper::getFitsAttributes(const XalanNode *node, FitsData &fd, int *status)
{
	const XalanNamedNodeMap * xnnp = node->getAttributes();
	int numOfAttribs = xnnp->getLength();
	XalanDOMString name, value;
	char *charValue = NULL;
	int result = 0;


	//cout << "Number of attributes are = " << numOfAttribs << endl;

	// need to get attributes one by one
	for (int i = 0; i <  numOfAttribs; i++)
	{
		XalanNode * attribNode = xnnp->item(i);

		name = attribNode->getNodeName();
		value = attribNode->getNodeValue();

#ifdef  DEBUG_MODE
		cout << "Coosys attribute name is = " << name << endl;
		cout << "Attribute value is = " << value << endl;
#endif
		
		
		if (name.compare(((const XalanDOMString)ATTR_EXTNUM)) == 0) 
		{
			// set type
			charValue = getCharString(value);
			fd.setExtnum(charValue, &result);
			
		} 			
	}
	*status = SUCCESS;
	return SUCCESS;
}


/*
* Get attributes of table.
*/
int XPathHelper::getTableAttributes(const XalanNode *node, VTable &v, int *status)
{
	const XalanNamedNodeMap * xnnp = node->getAttributes();
	int numOfAttribs = xnnp->getLength();
	XalanDOMString name, value;
	char *charValue = NULL;
	int result = 0;
	
	//cout << "Number of attributes are = " << numOfAttribs << endl;

	// need to get attributes one by one
	for (int i = 0; i <  numOfAttribs; i++)
	{
		XalanNode * attribNode = xnnp->item(i);

		name = attribNode->getNodeName();
		value = attribNode->getNodeValue();

#ifdef  DEBUG_MODE
		cout << "Table attribute name is = " << name << endl;
		cout << "Attribute value is = " << value << endl;
#endif
		
		
		if (name.compare(((const XalanDOMString)ATTR_ID)) == 0) 
		{
			// set ID
			charValue = getCharString(value);
			v.setID(charValue, &result);
			
		}
		else if (name.compare(((const XalanDOMString)ATTR_UTYPE)) == 0)
		{
			// set utype
			charValue = getCharString(value);
			v.setUtype(charValue, &result);
			
		}
		else if (name.compare(((const XalanDOMString)ATTR_NAME)) == 0)
		{
			// set ID
			charValue = getCharString(value);
			v.setName(charValue, &result);
			
		}
		else if (name.compare(((const XalanDOMString)ATTR_REF)) == 0)
		{
			// set ID
			charValue = getCharString(value);
			v.setRef(charValue, &result);
			
		}
	}
	*status = SUCCESS;
	return *status;
}

/*
* Get attributes of Column (Cell).
*/
int XPathHelper::getColumnAttributes(const XalanNode *node, Column &c, int *status)
{
	const XalanNamedNodeMap * xnnp = node->getAttributes();
	int numOfAttribs = xnnp->getLength();
	XalanDOMString name, value;
	char *charValue = NULL;
	int result = 0;
	
	//cout << "Number of attributes are = " << numOfAttribs << endl;

	// need to get attributes one by one
	for (int i = 0; i <  numOfAttribs; i++)
	{
		XalanNode * attribNode = xnnp->item(i);

		name = attribNode->getNodeName();
		value = attribNode->getNodeValue();

#ifdef  DEBUG_MODE
		cout << "Column attribute name is = " << name << endl;
		cout << "Attribute value is = " << value << endl;
#endif
		
		
		if (name.compare(((const XalanDOMString)ATTR_REF)) == 0) 
		{
			// set ID
			charValue = getCharString(value);
			c.setRef(charValue, &result);
			
		} 
		
	}
	return SUCCESS;
}


/*
* Get attributes of option.
*/
int XPathHelper::getOptionAttributes(const XalanNode *node, Option &o, int *status)
{
	const XalanNamedNodeMap * xnnp = node->getAttributes();
	int numOfAttribs = xnnp->getLength();
	XalanDOMString name, value;
	char *charValue = NULL;
	int result = 0;
	
	//cout << "Number of attributes are = " << numOfAttribs << endl;

	// need to get attributes one by one
	for (int i = 0; i <  numOfAttribs; i++)
	{
		XalanNode * attribNode = xnnp->item(i);

		name = attribNode->getNodeName();
		value = attribNode->getNodeValue();

#ifdef  DEBUG_MODE
		cout << "Option attribute name is = " << name << endl;
		cout << "Attribute value is = " << value << endl;
#endif
		
		
		if (name.compare(((const XalanDOMString)ATTR_NAME)) == 0) 
		{
			// set ID
			charValue = getCharString(value);
			o.setName(charValue, &result);
			
		} 
		else if (name.compare(((const XalanDOMString)ATTR_VALUE)) == 0)
		{
			// set ID
			charValue = getCharString(value);
			o.setValue(charValue, &result);
			
		}
		
	}

	*status = SUCCESS;
	return SUCCESS;
}

/*
* Get attributes of Info.
*/
int XPathHelper::getInfoAttributes(const XalanNode *node, Info &info, int *status)
{
	const XalanNamedNodeMap * xnnp = node->getAttributes();
	int numOfAttribs = xnnp->getLength();
	XalanDOMString name, value;
	char *charValue = NULL;
	int result = 0;

	//cout << "Number of attributes are = " << numOfAttribs << endl;

	// need to get attributes one by one
	for (int i = 0; i <  numOfAttribs; i++)
	{
		XalanNode * attribNode = xnnp->item(i);

		name = attribNode->getNodeName();
		value = attribNode->getNodeValue();

#ifdef  DEBUG_MODE
		cout << "Info attribute name is = " << name << endl;
		cout << "Attribute value is = " << value << endl;
#endif
		
		
		if (name.compare(((const XalanDOMString)ATTR_NAME)) == 0) 
		{
			// set ID
			charValue = getCharString(value);
			info.setName(charValue, &result);
			
		} 
		else if (name.compare(((const XalanDOMString)ATTR_VALUE)) == 0)
		{
			// set ID
			charValue = getCharString(value);
			info.setValue(charValue, &result);
			
		}
		else if (name.compare(((const XalanDOMString)ATTR_ID)) == 0)
		{
			// set ID
			charValue = getCharString(value);
			info.setID(charValue, &result);
			
		}
		
	}
	*status = SUCCESS;
	return SUCCESS;
}

/*
* Get attributes of Coosys.
*/
int XPathHelper::getCoosysAttributes(const XalanNode *node, Coosys &c, int *status)
{
	const XalanNamedNodeMap * xnnp = node->getAttributes();
	int numOfAttribs = xnnp->getLength();
	XalanDOMString name, value;
	char *charValue = NULL;
	int result = 0;


	//cout << "Number of attributes are = " << numOfAttribs << endl;

	// need to get attributes one by one
	for (int i = 0; i <  numOfAttribs; i++)
	{
		XalanNode * attribNode = xnnp->item(i);

		name = attribNode->getNodeName();
		value = attribNode->getNodeValue();

#ifdef  DEBUG_MODE
		cout << "Coosys attribute name is = " << name << endl;
		cout << "Attribute value is = " << value << endl;
#endif
		
		
		if (name.compare(((const XalanDOMString)ATTR_ID)) == 0) 
		{
			// set ID
			charValue = getCharString(value);
			c.setID(charValue, &result);
			
		} 
		else if (name.compare(((const XalanDOMString)ATTR_EQUINOX)) == 0)
		{
			// set equinox
			charValue = getCharString(value);
			c.setEquinox(charValue, &result);
			
		}
		else if (name.compare(((const XalanDOMString)ATTR_EPOCH)) == 0)
		{
			// set epoch
			charValue = getCharString(value);
			c.setEpoch(charValue, &result);
			
		}
		else if (name.compare(((const XalanDOMString)ATTR_SYSTEM)) == 0)
		{
			// set system
			charValue = getCharString(value);
			
			int sys = getCoosysSystem(charValue);

			delete[] charValue;

			coosys_system system;
			if (sys == 0)
			{
				system = eq_FK5;
			} 
			else 
			{
				switch (sys)
				{
				case eq_FK4 :
				{
					system = eq_FK4;
					break;
				}
				case eq_FK5 :
				{
					system = eq_FK5;
					break;
				}
				case ICRS :
				{
					system = ICRS;
					break;
				}
				case ecl_FK4 :
				{
					system = ecl_FK4;
					break;
				}
				case ecl_FK5 :
				{
					system = ecl_FK5;
					break;
				}
				case galactic :
				{
					system = galactic;
					break;
				}
				case supergalactic :
				{
					system = supergalactic;
					break;
				}
				case xy :
				{
					system = xy;
					break;
				}
				case barycentric :
				{
					system = barycentric;
					break;
				}
				case geo_app :
				{
					system = geo_app;
					break;
				}
				default :
				{
					system = eq_FK5;
					break;
				}
				} // end of switch

			}
		    c.setSystem(system, &result);		
		}
		
	}
	*status = SUCCESS;
	return SUCCESS;
}


/*
* Get attributes of range.
*/
Range XPathHelper::getRangeAttributes(const XalanNode *node, int *status)
{
	const XalanNamedNodeMap * xnnp = node->getAttributes();
	int numOfAttribs = xnnp->getLength();
	XalanDOMString name, value;
	char *charValue = NULL;
	int result = 0;
	Range r;

	//cout << "Number of attributes are = " << numOfAttribs << endl;

	// need to get attributes one by one
	for (int i = 0; i <  numOfAttribs; i++)
	{
		XalanNode * attribNode = xnnp->item(i);

		name = attribNode->getNodeName();
		value = attribNode->getNodeValue();

#ifdef  DEBUG_MODE
		cout << "Range attribute name is = " << name << endl;
		cout << "Attribute value is = " << value << endl;	
#endif
		
		
		if (name.compare(((const XalanDOMString)ATTR_VALUE)) == 0) 
		{
			// set ID
			charValue = getCharString(value);
			r.setValue(charValue, &result);
			
		} 
		else if (name.compare(((const XalanDOMString)ATTR_INCLUSIVE)) == 0)
		{
			// set ID
			charValue = getCharString(value);
			bool inclusive = true;
			if (strcmp(charValue, NO) == 0)
			{
				inclusive = false;				
			} 
			delete[] charValue;
		
			r.setInclusiveFlag(inclusive, &result);
			
		}
		
	}
	*status = SUCCESS;
	return r;
}

/*
* Get attributes of Values.
*/
int XPathHelper::getValuesAttributes(const XalanNode *node, Values &v, int *status)
{
	const XalanNamedNodeMap * xnnp = node->getAttributes();
	int numOfAttribs = xnnp->getLength();
	XalanDOMString name, value;
	char *charValue = NULL;
	int result = 0;
	

	//cout << "getting values attributes" <<  endl;
	//cout << "Number of attributes are = " << numOfAttribs << endl;

	// need to get attributes one by one
	for (int i = 0; i <  numOfAttribs; i++)
	{
		XalanNode * attribNode = xnnp->item(i);

		name = attribNode->getNodeName();
		value = attribNode->getNodeValue();

#ifdef  DEBUG_MODE
		cout << "Values attribute name is = " << name << endl;
		cout << "Attribute value is = " << value << endl;
#endif
		
		
		if (name.compare(((const XalanDOMString)ATTR_ID)) == 0) 
		{
			// set ID
			charValue = getCharString(value);
			v.setID(charValue, &result);
			delete[] charValue;
			
			
		} 
		else if (name.compare(((const XalanDOMString)ATTR_TYPE)) == 0)
		{
			// set ID
			charValue = getCharString(value);
			values_type type;
			if (strcmp(charValue, VALUES_ACTUAL_TYPE) == 0) 
			{
				type = actual;
			} 
			else 
			{
				type = legal;
			}
			v.setType(type, &result);
			delete[] charValue;
			
		}
		else if (name.compare(((const XalanDOMString)ATTR_NULL)) == 0)
		{
			// set null
			charValue = getCharString(value);
			v.setNull(charValue, &result);
			delete[] charValue;
			
		}
		else if (name.compare(((const XalanDOMString)ATTR_REF)) == 0)
		{
			// set ref
			charValue = getCharString(value);
			v.setRef(charValue, &result);
			delete[] charValue;
			
		}
		else if (name.compare(((const XalanDOMString)ATTR_INVALID)) == 0)
		{
			// set ID
			charValue = getCharString(value);
			bool invalid;
			if (strcmp(charValue, YES) == 0) 
			{
				invalid = true;
			} 
			else 
			{
				invalid = false;
			}
			v.setInvalidFlag(invalid, &result);
			delete[] charValue;
			
		}
	}

	//cout << "finished parsing the value attributes" << endl;


	return SUCCESS;
}


/*
* Get attributes of field.
*/
int XPathHelper::getFieldParamAttributes(const XalanNode *node, FieldParam *fp, bool isField, int *status)
{
	const XalanNamedNodeMap * xnnp = node->getAttributes();
	int numOfAttribs = xnnp->getLength();
	XalanDOMString name, value;
	char *charValue = NULL;
	int result = 0;
	

	//cout << "Number of attributes are = " << numOfAttribs << endl;

	// need to get attributes one by one
	for (int i = 0; i <  numOfAttribs; i++)
	{
		XalanNode * attribNode = xnnp->item(i);

		name = attribNode->getNodeName();
		value = attribNode->getNodeValue();

#ifdef  DEBUG_MODE
		cout << "Field attribute name is = " << name << endl;
		cout << "Attribute value is = " << value << endl;
#endif
		
		
		if (name.compare(((const XalanDOMString)ATTR_ID)) == 0) 
		{
			// set ID
			charValue = getCharString(value);
			(*fp).setID(charValue, &result);
			
		} 
		else if (name.compare(((const XalanDOMString)ATTR_UTYPE)) == 0)
		{
			// set utype
			charValue = getCharString(value);
			(*fp).setUtype(charValue, &result);
		}
		else if (name.compare(((const XalanDOMString)ATTR_UNIT)) == 0)
		{
			// set unit
			charValue = getCharString(value);
			(*fp).setUnit(charValue, &result);
		}
		else if (name.compare(((const XalanDOMString)ATTR_DATATYPE)) == 0)
		{
			// set datatype
			charValue = getCharString(value);
			int dt = getFieldDatatype(charValue);

			// delete here itself.
			delete[] charValue;
			field_datatype fdt;

			if (dt == 0)
			{
				//cout << "Field does not have dataype." << endl;
				//f.error = VOERROR;
				//result = VOERROR;
				fdt = datatype_not_specified;
			} else {
				
				switch (dt) {
					case BooleanType:  fdt = BooleanType; break;
					case BitType :  fdt = BitType; break; 
					case UnsignedByteType :  fdt = UnsignedByteType; break; 
					case ShortType :  fdt = ShortType; break; 
					case IntType :  fdt = IntType; break; 
					case LongType :  fdt = LongType; break; 
					case CharType :  fdt = CharType; break; 
					case UnicodeCharType :  fdt = UnicodeCharType; break; 
					case FloatType :  fdt = FloatType; break; 
					case DoubleType :  fdt = DoubleType; break; 
					case FloatComplexType :  fdt = FloatComplexType; break; 
					case DoubleComplexType :  fdt = DoubleComplexType; break; 
				}
				
			}
			(*fp).setDatatype(fdt, &result);
			
		}
		else if (name.compare(((const XalanDOMString)ATTR_PRECISION)) == 0)
		{
			// set precision
			charValue = getCharString(value);
			(*fp).setPrecision(charValue, &result);
		}
		else if (name.compare(((const XalanDOMString)ATTR_WIDTH)) == 0)
		{
			// set width
			charValue = getCharString(value);
			// convert string to 'int'.
			unsigned int width = atoi(charValue);
			(*fp).setWidth(width, &result);
			delete[] charValue;
		}
		else if (name.compare(((const XalanDOMString)ATTR_REF)) == 0)
		{
			// set ref
			charValue = getCharString(value);
			(*fp).setRef(charValue, &result);
		}
		else if (name.compare(((const XalanDOMString)ATTR_NAME)) == 0)
		{
			// set name
			charValue = getCharString(value);
			(*fp).setName(charValue, &result);
		}
		else if (name.compare(((const XalanDOMString)ATTR_UCD)) == 0)
		{
			// set UCD
			charValue = getCharString(value);
			(*fp).setUCD(charValue, &result);
		}
		else if (name.compare(((const XalanDOMString)ATTR_ARRAYSIZE)) == 0)
		{
			// set arraysize
			charValue = getCharString(value);
			(*fp).setArraySize(charValue, &result);
		} 
		else if (isField && name.compare(((const XalanDOMString)ATTR_TYPE)) == 0)
		{
			// set type
			charValue = getCharString(value);
			int type = getFieldType(charValue);
			field_type ft;
			switch (type) {
				case type_not_specified:  ft = type_not_specified; break;
				case hidden:  ft = hidden; break;
				case no_query:  ft = no_query; break;
				case trigger:  ft = trigger; break;
			}	 
			delete[] charValue;

			(*fp).setType(ft, &result);
		}
		else if (!isField && name.compare(((const XalanDOMString)ATTR_VALUE)) == 0)
		{
			// set arraysize
			charValue = getCharString(value);
			(*fp).setValue(charValue, &result);
		}

	}

	//Field f1 = new Field(f);

	return SUCCESS;
}

/*
* Fill in the 'Field'.s
*/
int XPathHelper::getFieldParam(const XalanNode *node, FieldParam *fp, bool isField, int *status)
{
	int result = 0;

	// get field attributes
	getFieldParamAttributes(node, fp, isField, &result);
	
	XalanDOMString	name;
	
	const XalanNode * childNode = node->getFirstChild();

	if (childNode == NULL)
	{
#ifdef  DEBUG_MODE
		cout << "Field has no child nodes." << endl;
#endif
		return SUCCESS;
	}

	const XalanNode * siblingNode;
	
	vector <Link> links;
	vector <Values> values;
	Link l;
	Values v[2];
	siblingNode = childNode;
	int valueIndex = 0;
		

	//get all siblings (not using getChildNodes since it is not working) :-(
	while (siblingNode != NULL) 
	{
		// process only element nodes, not text nodes.
		// To do - find some way of avoiding text nodes.

		//cout << "Next node name is " << siblingNode->getNodeName() << endl;
		//cout << "Next node type is " << siblingNode->getNodeType() << endl;
		if (siblingNode->getNodeType() == XalanNode::ELEMENT_NODE)
		{
			name = siblingNode->getNodeName();
#ifdef  DEBUG_MODE
			cout << "Node name is " << siblingNode->getNodeName() << endl;
#endif

			// check if node is description node
			if (name.compare(((const XalanDOMString)ELE_DESC)) == 0) {
				(*fp).setDescription(getDescription(siblingNode, &result), &result) ;
			} else if (name.compare(((const XalanDOMString)ELE_LINK)) == 0) { // check if node is link node
				Link l;
				getLink(siblingNode,l, &result);
				links.push_back(l);				
			} else if (name.compare(((const XalanDOMString)ELE_VALUES)) == 0) { // check if node is Values
				// there should be only 2 'values' nodes.
				if (valueIndex < 2) 
				{
					//cout << "going to get Values" << endl;
					getValues(siblingNode, v[valueIndex], &result);
					valueIndex++;
				}
			} 
		}
		siblingNode = siblingNode->getNextSibling();
	}
	
	(*fp).setValues(v, valueIndex, &result);
	(*fp).setLinks(links, &result);

//	Field f1 = new Field(f);
//	delete &f;
#ifdef  DEBUG_MODE
		cout << "Parsed field" << endl;
#endif

	return SUCCESS;
}




/*
* Fill in the FieldRef.s
*/
int XPathHelper::getFieldParamRef(const XalanNode *node, FieldParamRef *fp, int *status)
{
	int result = 0;

	// get field attributes
	getFieldParamRefAttributes(node, fp, &result);
	
	return SUCCESS;
}



/*
* Get attributes of field.
*/
int XPathHelper::getFieldParamRefAttributes(const XalanNode *node, FieldParamRef *fp, int *status)
{
	const XalanNamedNodeMap * xnnp = node->getAttributes();
	int numOfAttribs = xnnp->getLength();
	XalanDOMString name, value;
	char *charValue = NULL;
	int result = 0;
	

	//cout << "Number of attributes are = " << numOfAttribs << endl;

	// need to get attributes one by one
	for (int i = 0; i < numOfAttribs; i++)
	{
		XalanNode * attribNode = xnnp->item(i);

		name = attribNode->getNodeName();
		value = attribNode->getNodeValue();

#ifdef  DEBUG_MODE
		cout << "Field Ref attribute name is = " << name << endl;
		cout << "Attribute value is = " << value << endl;
#endif
		
		
		if (name.compare(((const XalanDOMString)ATTR_REF)) == 0) 
		{
			// set ref
			charValue = getCharString(value);
			(*fp).setRef(charValue, &result);	
		} 	
	}
	return SUCCESS;
}


int XPathHelper::getLink(const XalanNode *node, Link &l, int *status)
{
	
	const XalanNamedNodeMap * xnnp = node->getAttributes();
	int numOfAttribs = xnnp->getLength();
	XalanDOMString name, value;
	char *charValue = NULL;
	int result = 0;

	//cout << "Number of attributes are = " << numOfAttribs << endl;

	// need to get attributes one by one
	for (int i = 0; i <  numOfAttribs; i++)
	{
		XalanNode * attribNode = xnnp->item(i);

		name = attribNode->getNodeName();
		value = attribNode->getNodeValue();

#ifdef  DEBUG_MODE
		cout << "Link attribute name is = " << name << endl;
		cout << "Attribute value is = " << value << endl;
#endif
		
		
		if (name.compare(((const XalanDOMString)ATTR_ID)) == 0) 
		{
			// set ID
			charValue = getCharString(value);
			l.setID(charValue, &result); 			
		} 
		else if (name.compare(((const XalanDOMString)ATTR_CONTENT_ROLE)) == 0)
		{
			charValue = getCharString(value);
			int role = getContentRole(charValue);
			content_role cr;
			switch (role) {
				case role_not_specified:  cr = role_not_specified; break;
				case query:  cr = query; break;
				case hints:  cr = hints; break;
				case doc:  cr = doc; break;
			}	
					
			l.setContentRole(cr, &result);
			delete[] charValue;
		}
		else if (name.compare(((const XalanDOMString)ATTR_CONTENT_TYPE)) == 0)
		{
			charValue = getCharString(value);
			l.setContentType(charValue, &result);
		}
		else if (name.compare(((const XalanDOMString)ATTR_TITLE)) == 0)
		{
			charValue = getCharString(value);
			l.setTitle(charValue, &result);
		}
		else if (name.compare(((const XalanDOMString)ATTR_VALUE)) == 0)
		{
			charValue = getCharString(value);
			l.setValue(charValue, &result);
		}
		else if (name.compare(((const XalanDOMString)ATTR_HREF)) == 0)
		{
			charValue = getCharString(value);
			l.setHRef(charValue, &result);
		}
		else if (name.compare(((const XalanDOMString)ATTR_GREF)) == 0)
		{
			charValue = getCharString(value);
			l.setGRef(charValue, &result);
		}
		else if (name.compare(((const XalanDOMString)ATTR_ACTION)) == 0)
		{
			charValue = getCharString(value);
			l.setAction(charValue, &result);
		}
	}

	// get the PC Data
	char *pcdata = NULL;
	const XalanNode * childNode = node->getFirstChild();
	if (childNode == NULL)
	{
#ifdef  DEBUG_MODE
		cout << "LINK has no child nodes." << endl;		
#endif
	} 
	else 
	{
		pcdata = getCharString(childNode->getNodeValue());
#ifdef  DEBUG_MODE
		cout << "PCDATA in Link is " << pcdata << endl;
#endif
	}

	l.setPCData(pcdata, &result);
	

	*status = SUCCESS;
	return SUCCESS;
}

/*
* Get the description.
*/
char * XPathHelper::getDescription(const XalanNode *node, int * status)
{
	char * desc = NULL;

	const XalanNode * childNode = node->getFirstChild();

	if (childNode == NULL)
	{
#ifdef  DEBUG_MODE
		cout << "DESC has no child nodes." << endl;
#endif
		return desc;
	} 
	else 
	{
		desc = getCharString(childNode->getNodeValue());
#ifdef  DEBUG_MODE
		cout << "Description is " << desc << endl;
#endif
	}

	return desc;
}

/*
* Extract information from Values node.
*/
int XPathHelper::getValues(const XalanNode *node, Values &v, int *status)
{
	int result = 0;
	// get attributes of Values node
	getValuesAttributes(node, v, &result);

	// fill child nodes of Values
	XalanDOMString	name;
	
	const XalanNode * childNode = node->getFirstChild();
	const XalanNode * siblingNode;
	
	Range *minimum = NULL;
	Range *maximum = NULL;
	vector<Option> optionList;
	siblingNode = childNode;
		

	//get all siblings (not using getChildNodes since it is not working) :-(
	while (siblingNode != NULL) 
	{
		// process only element nodes, not text nodes.
		// To do - find some way of avoiding text nodes.

		//cout << "Next node name is " << siblingNode->getNodeName() << endl;
		//cout << "Next node type is " << siblingNode->getNodeType() << endl;
		if (siblingNode->getNodeType() == XalanNode::ELEMENT_NODE)
		{
			name = siblingNode->getNodeName();
			//cout << "Node name is " << siblingNode->getNodeName() << endl;

			// check if node is 'Option' node
			if (name.compare(((const XalanDOMString)ELE_OPTION)) == 0) {
				Option o;
				getOption(siblingNode, o, &result);
				optionList.push_back(o) ;
			} else if (name.compare(((const XalanDOMString)ELE_MIN)) == 0) { // check if node is MIN 
				minimum = (getRange(siblingNode,&result));				
			} else if (name.compare(((const XalanDOMString)ELE_MAX)) == 0) { // check if node is MAX
				maximum = (getRange(siblingNode,&result));
			} 
		}
		siblingNode = siblingNode->getNextSibling();
	}
	v.setMinimun(minimum, &result);
	v.setMaximum(maximum, &result);
	v.setOptions(optionList, &result);
#ifdef  DEBUG_MODE
		cout << "Parsed values." << endl;
#endif

	return SUCCESS;


}

/*
* Extract information from 'Info' node.
*/
int XPathHelper::getInfo(const XalanNode *node, Info &i, int *status)
{
	int result = 0;
	// get attributes of Values node
	getInfoAttributes(node, i, &result);

	// get the PC Data
	char *pcdata = NULL;
	const XalanNode * childNode = node->getFirstChild();
	if (childNode == NULL)
	{
#ifdef  DEBUG_MODE
		cout << "LINK has no child nodes." << endl;		
#endif
	} 
	else 
	{
		pcdata = getCharString(childNode->getNodeValue());
#ifdef  DEBUG_MODE
		cout << "PCDATA in Link is " << pcdata << endl;
#endif
	}

	i.setPCData(pcdata, &result);

	*status = SUCCESS;
	return SUCCESS;
}

/*
* Extract information from 'Coosys' node.
*/
int XPathHelper::getCoosys(const XalanNode *node, Coosys &c, int *status)
{
	int result = 0;
	// get attributes of Values node
	getCoosysAttributes(node, c, &result);

	// get the PC Data
	char *pcdata = NULL;
	const XalanNode * childNode = node->getFirstChild();
	if (childNode == NULL)
	{
#ifdef  DEBUG_MODE
		cout << "Coosys has no child nodes." << endl;		
#endif
	} 
	else 
	{
		pcdata = getCharString(childNode->getNodeValue());
#ifdef  DEBUG_MODE
		cout << "PCDATA in Link is " << pcdata << endl;
#endif
	}

	c.setPCData(pcdata, &result);

	*status = SUCCESS;
	return SUCCESS;
}


/*
* Extract information from Option node.
*/
int XPathHelper::getOption(const XalanNode *node, Option &o, int *status)
{
	int result = 0;
	// get attributes of Option node
	getOptionAttributes(node, o, &result);
		
	XalanDOMString	name;
	
	const XalanNode * childNode = node->getFirstChild();
	const XalanNode * siblingNode;
	
	vector<Option> optionList;
	siblingNode = childNode;
		

	//get all siblings (not using getChildNodes since it is not working) :-(
	while (siblingNode != NULL) 
	{
		// process only element nodes, not text nodes.
		// To do - find some way of avoiding text nodes.

		//cout << "Next node name is " << siblingNode->getNodeName() << endl;
		//cout << "Next node type is " << siblingNode->getNodeType() << endl;
		if (siblingNode->getNodeType() == XalanNode::ELEMENT_NODE)
		{
			name = siblingNode->getNodeName();
			//cout << "Node name is " << siblingNode->getNodeName() << endl;

			// check if node is 'Option' node
			if (name.compare(((const XalanDOMString)ELE_OPTION)) == 0) {
				Option option;
				getOption(siblingNode, option, &result);
				optionList.push_back(option) ;
			} 
		}
		siblingNode = siblingNode->getNextSibling();
	}
	
	o.setOptions(optionList, &result);

	*status = SUCCESS;
	return SUCCESS;

}


/*
* Extract information from Range (Min or Max) node.
*/
Range * XPathHelper::getRange(const XalanNode *node, int *status)
{
	int result = 0;
	// get attributes of Option node
	Range *r = new Range(getRangeAttributes(node, &result));

	// get the PC Data
	char *pcdata = NULL;
	const XalanNode * childNode = node->getFirstChild();
	if (childNode == NULL)
	{
#ifdef  DEBUG_MODE
		cout << "Range has no child nodes." << endl;		
#endif
	} 
	else 
	{
		pcdata = getCharString(childNode->getNodeValue());
#ifdef  DEBUG_MODE
		cout << "PCDATA in Link is " << pcdata << endl;
#endif
	}

	(*r).setPCData(pcdata, &result);
	
	return r;

}

/*
* Extract information from Option node.

Option XPathHelper::getOption(const XalanNode *node, int *status)
{
	int result = 0;
	// get attributes of Option node
	Option o = getOptionAttributes(node, &result);
		
	XalanDOMString	name;
	
	const XalanNode * childNode = node->getFirstChild();
	const XalanNode * siblingNode;
	
	vector<Option> optionList;
	siblingNode = childNode;
		

	//get all siblings (not using getChildNodes since it is not working) :-(
	while (siblingNode != NULL) 
	{
		// process only element nodes, not text nodes.
		// To do - find some way of avoiding text nodes.

		//cout << "Next node name is " << siblingNode->getNodeName() << endl;
		//cout << "Next node type is " << siblingNode->getNodeType() << endl;
		if (siblingNode->getNodeType() == XalanNode::ELEMENT_NODE)
		{
			name = siblingNode->getNodeName();
			cout << "Node name is " << siblingNode->getNodeName() << endl;

			// check if node is 'Option' node
			if (name.compare(ELE_OPTION) == 0) {
				optionList.push_back(getOption(siblingNode, &result)) ;
			} 
		}
		siblingNode = siblingNode->getNextSibling();
	}
	
	o.setOptions(optionList, &result);

	return o;

}
*/


/*
* Extract information from 'Data' node.
*/
int XPathHelper::getData(const XalanNode *node, VTable &v, 
						 vector<Field> &fields, int *status)
{
	int result = 0;
			
	XalanDOMString	name;

	const XalanNode * childNode = node->getFirstChild();
	const XalanNode * siblingNode;
		
	siblingNode = childNode;

	//get all siblings (not using getChildNodes since it is not working) :-(
	while (siblingNode != NULL) 
	{
		// process only element nodes, not text nodes.
		// To do - find some way of avoiding text nodes.

		//cout << "Next node name is " << siblingNode->getNodeName() << endl;
		//cout << "Next node type is " << siblingNode->getNodeType() << endl;
		if (siblingNode->getNodeType() == XalanNode::ELEMENT_NODE)
		{
			name = siblingNode->getNodeName();
			//cout << "Node name is " << siblingNode->getNodeName() << endl;

			// check if node is 'Option' node
			if (name.compare(((const XalanDOMString)ELE_TABLEDATA)) == 0) {
				vector<Row> rowList;
				TableData td;
				getRows(siblingNode, rowList, fields, &result);
				td.setRows(rowList, &result);
				v.setData(td, &result);
				v.setBinaryFlag(false, &result);
			}
			// Added to support binary format
			else if (name.compare(((const XalanDOMString)ELE_BINARY)) == 0) {
				vector<Row> rowList;
				TableData td;
				BinaryData bd;
				getBinaryData(siblingNode, rowList, bd, fields, &result);
				td.setRows(rowList, &result);
				v.setData(td, &result);
				v.setBinaryData(bd, &result);
				VTable::setBinaryFlag(true, &result);
			}
			else if(name.compare(((const XalanDOMString)ELE_FITS)) == 0)  {
				FitsData fd;
				getFitsData(siblingNode, fd, fields, &result);
				v.setFitsData(fd, &result);
			}
			else 
			{
				*status = VOERROR;
				// there is no table data in this table
				// Current implementation supports only TableData
				//cout << "Current implemeation supports only TableData." << endl;
				return VOERROR;

			}
		}
		siblingNode = siblingNode->getNextSibling();
	}

#ifdef  DEBUG_MODE
	cout << "Parsed the data." << endl;		
#endif

	*status = SUCCESS;
	return SUCCESS;

}

/*
* Extract information from 'TableData' node.
*/
int XPathHelper::getRows(const XalanNode *node, vector<Row> &rowList, 
						 const vector<Field> &fields, int *status)
{
	int result = 0;
			
	XalanDOMString	name;
	
	const XalanNode * childNode = node->getFirstChild();
	const XalanNode * siblingNode;
		
	siblingNode = childNode;
		

	//get all siblings (not using getChildNodes since it is not working) :-(
	while (siblingNode != NULL) 
	{
		Row r;
		// process only element nodes, not text nodes.
		// To do - find some way of avoiding text nodes.

		//cout << "Next node name is " << siblingNode->getNodeName() << endl;
		//cout << "Next node type is " << siblingNode->getNodeType() << endl;
		if (siblingNode->getNodeType() == XalanNode::ELEMENT_NODE)
		{
			name = siblingNode->getNodeName();
			//cout << "Node name is " << siblingNode->getNodeName() << endl;

			// check if node is 'Option' node
			if (name.compare(((const XalanDOMString)ELE_TR)) == 0) {
				getRow(siblingNode, r, fields, &result);
				rowList.push_back(r);
			} 
			
		}
		siblingNode = siblingNode->getNextSibling();
	}
	
#ifdef  DEBUG_MODE
	cout << "Parsed the rows." << endl;		
#endif
	

	*status = SUCCESS;
	return SUCCESS;

}

/*
* Extract information from 'Row' node.
*/
int XPathHelper::getRow(const XalanNode *node, Row &row, 
						const vector<Field> &fields, int *status)
{
	int result = 0;
			
	XalanDOMString	name;
	
	const XalanNode * childNode = node->getFirstChild();
	const XalanNode * siblingNode;
		
	siblingNode = childNode;
	vector<Column> columnList;
	int columnNum = 0;
		

	//get all siblings (not using getChildNodes since it is not working) :-(
	while (siblingNode != NULL) 
	{
		Column c;
		// process only element nodes, not text nodes.
		// To do - find some way of avoiding text nodes.

		//cout << "Next node name is " << siblingNode->getNodeName() << endl;
		//cout << "Next node type is " << siblingNode->getNodeType() << endl;
		if (siblingNode->getNodeType() == XalanNode::ELEMENT_NODE)
		{
			name = siblingNode->getNodeName();
			//cout << "Node name is " << siblingNode->getNodeName() << endl;

			// check if node is 'TD' node
			if (name.compare(((const XalanDOMString)ELE_TD)) == 0) {
				bool isUnicode = false;
				field_datatype d;
#ifdef  DEBUG_MODE
						cout << "Col Num  " << columnNum << endl;		
#endif

				if (columnNum < fields.size ()) 
				{
					Field f = fields[columnNum];

					f.getDatatype(d, &result);
#ifdef  DEBUG_MODE
						cout << "Field  " << columnNum  << " dt is " << d << endl;		
#endif
					if (d == UnicodeCharType)
					{
						isUnicode = true;
#ifdef  DEBUG_MODE
						cout << "Getting unicode data for column " << columnNum << endl;		
#endif
					}

				
				}
				getColumn(siblingNode, c, isUnicode, fields, columnNum, &result);					

				/*if(d == BitType)
				{

					int numOfElements;
					char *cArray;
					if (c.getBitArray(cArray, numOfElements, status) == SUCCESS)
					{
						cout << endl << "Number of elements is " << numOfElements << endl;
						// numOfBytes will contain the no of bytes which will hold the bits.
						int numOfBytes;

						if((numOfElements % 8) == 0)
						{
							numOfBytes = (numOfElements/8);
						}
						else 
						{
							numOfBytes = (numOfElements /8) + 1;
						}
						cout << "Data is " ;
						for (int i = 0; i < numOfBytes; i++)
						{
							if(numOfElements >= 8)
							{
								//printBits(cArray[i], 8);
							}
							else
							{
								//printBits(cArray[i], numOfElements);
							}

							numOfElements -= 8;
							// cout << cArray[i] << " ";
						}
						
						delete[] cArray;
					}

				}*/

				columnList.push_back(c);
				columnNum++;
			} 
			
		}
		siblingNode = siblingNode->getNextSibling();
	
	}

	row.setColumns(columnList, &result);


	return SUCCESS;

}

/*
* Extract information from 'Column' node.
*/
int XPathHelper::getColumn(const XalanNode *node, Column &c, bool isUnicode, const vector<Field> &fields, int columnNum, int *status)
{
	int result = 0;

	getColumnAttributes(node, c, &result);

	XalanDOMString	name;
		
	const XalanNode * childNode = node->getFirstChild();

	char * data = NULL;
	
	if (childNode == NULL)
	{
		
		data = new char[4];
		strcpy(data, "NaN");

		
#ifdef  DEBUG_MODE
		cout << "TD has no child nodes." << endl;		
#endif
	} 
	else 
	{
		if (isUnicode)
		{
			int size = 0;
			unsigned short *unicodeData = getShortString(childNode->getNodeValue(), size);
			c.setUnicodeData(unicodeData, size, status);
		}
		else
		{
			data = getCharString(childNode->getNodeValue());

			Field f = fields[columnNum];
			Values v; 
			char * ref;
			char * nullValue;
			
			if(f.getValues(v, 0, status) == SUCCESS)
			{
				if(v.getRef(ref, status) == SUCCESS && (ref != NULL))
				{
					for(int j=0; j < columnNum; j++)
					{
						Field ff = fields[j];
						Values vv;
						char * id;
						if(ff.getValues(vv, 0, status) == SUCCESS)
						{
							if(vv.getID(id, status) == SUCCESS)
							{
								if(strcmp(ref, id) == 0)
								{
									if(vv.getNull(nullValue, status) == SUCCESS)
									{
										vector <char *> string;
										char * str;
										char seps[] = " \t\n\v";
										str = strtok(data, seps );

										// get string sepated by spaces.
										while( str != NULL )
										{   
										  //cout << "Token is " << str << endl;
										  string.push_back (str);
										  str = strtok( NULL, seps );
										}

										for(int i=0; i< string.size(); i++)
										{
											if(strcmp(string[i], nullValue) == 0)
											{
												string[i] = new char[4];
												strcpy(string[i], "NaN");
											}
										}

										strcpy(data, string[0]);

										for(int i=1; i<string.size(); i++)
										{
											data = strcat(data, " ");
											data = strcat(data, string[i]);
										}
									}

									break;
								}
							}
						}
					}				
				}
				else if(v.getNull(nullValue, status) == SUCCESS && (nullValue != NULL))
				{
					vector <char *> string;
					char * str;
					char seps[] = " \t\n\v";
					str = strtok(data, seps );

					// get string sepated by spaces.
					while( str != NULL )
					{   
					  //cout << "Token is " << str << endl;
					  string.push_back (str);
					  str = strtok( NULL, seps );
					}

					int len = 0;
					for(int i=0; i< string.size(); i++)
					{
						if(strcmp(string[i], nullValue) == 0)
						{
							string[i] = new char[4];
							strcpy(string[i], "NaN");
						}
						len += strlen(string[i] + 1);
					}
					
					data = new char[len];
					strcpy(data, string[0]);

					for(int i=1; i<string.size(); i++)
					{
						cout << i << " " << string[i] << endl;
						data = strcat(data, " ");
						data = strcat(data, string[i]);
					}
				}
			}


			
#ifdef  DEBUG_MODE
		cout << "Data in TD is " << data << endl;
#endif
		}
	}

	// set the data
	c.setCharData(data, status);
	
	return SUCCESS;
	
}



/*
* Extract information from 'Binary' node.
*/
int XPathHelper::getBinaryData(const XalanNode *node, vector<Row> &rowList, BinaryData &bd,
						 const vector<Field> &fields, int *status)
{
	int result = 0;
			
	XalanDOMString	name;
	
	const XalanNode * childNode = node->getFirstChild();
	const XalanNode * siblingNode;
		
	Stream st;
	siblingNode = childNode;
		

	//get all siblings (not using getChildNodes since it is not working) :-(
	while (siblingNode != NULL) 
	{
		Row r;
		// process only element nodes, not text nodes.
		// To do - find some way of avoiding text nodes.

		//cout << "Next node name is " << siblingNode->getNodeName() << endl;
		//cout << "Next node type is " << siblingNode->getNodeType() << endl;
		if (siblingNode->getNodeType() == XalanNode::ELEMENT_NODE)
		{
			name = siblingNode->getNodeName();
			// cout << "Node name is " << siblingNode->getNodeName() << endl;

			// check if node is 'Option' node
			if (name.compare(((const XalanDOMString)ELE_STREAM)) == 0) {
										
				getBinaryStream(siblingNode, rowList, st, fields, &result);
				// rowList.push_back(r);
			} 
			
		}
		siblingNode = siblingNode->getNextSibling();
	}
	
	bd.setStream(st, &result);
	*status = SUCCESS;
	return SUCCESS;
}





/*
* Extract information from 'Stream' node.
*/
int XPathHelper::getBinaryStream(const XalanNode *node, vector<Row> &rowList, Stream &st,
						const vector<Field> &fields, int *status)
{
	int result = 0;
	getStreamAttributes(node, st, &result);
	
	XalanDOMString	name;
	
	const XalanNode * childNode = node->getFirstChild();
			
	XMLByte * data = NULL;
	
		
	if (childNode == NULL)
	{
#ifdef  DEBUG_MODE
		cout << "TD has no child nodes." << endl;		
#endif
	} 
	else 
	{
		using xercesc::Base64;
		using xercesc::XMLString;
		
		data = getXMLByteString(childNode->getNodeValue());

		st.setData(((char *)data), status);
 
		XMLSize_t streamLength = 0;
		unsigned int counter = 0;
		char * str;
		XMLByte * decodedStream = NULL;
		if (st.getEncoding(str, status) == SUCCESS && str != NULL)
		{
			if(_stricmp(str, "base64") == 0)
			{
				decodedStream = Base64::decode(data, &streamLength);
			}
			else
			{
				cout << "The stream is not base 64 encoded !" << endl;
				return SUCCESS;
			}
		}



		if(NULL == decodedStream)
		{
			*status = DECODING_ERROR;
			return DECODING_ERROR;
		}


		char* decodedData = (char*)decodedStream;

		char c, *strTemp, strBuff[50];

		char * temp;

		unsigned int numOfItems = 0;
		
		

		while(counter < streamLength)
		{
			Row r;
			
			vector<Column> columnList;
			
			for(int columnNum = 0; columnNum < fields.size(); columnNum++)
			{
				Column col;
				field_datatype d;

				strTemp = new char[200];

				char * str = NULL;

				Field f = fields[columnNum];

				f.getDatatype(d, &result);

				//cout << "column = " << columnNum << " ";
				//cout << "datatype = " << d << " ";

				if (f.getArraySize(str, status) == SUCCESS && str != NULL)
				{
					// check whether it's a variable sized array.
					if(strchr(str, '*') != NULL)
					{
						c = decodedData[counter];
						decodedData[counter] = decodedData[counter + 3];
						decodedData[counter + 3] = c;

						c = decodedData[counter + 1];
						decodedData[counter + 1] = decodedData[counter + 2];
						decodedData[counter + 2] = c;	

						numOfItems = *((int *)(decodedData + counter));

						counter += 4;
					}
					else 
					{
						numOfItems = atoi(str);
					}				
				}
				else 
				{
					numOfItems = 1;
				}

				//cout << "numOfItems = " << numOfItems << " ";


				switch(d)
				{
					
					case BitType:
					
						// We need to store the numOfItems which is the actual no of
						// bits present in the stream.
						_itoa(numOfItems, strBuff, 10);

						// Here numOfItems is used to calculate the no of bytes
						// which are used to hold all the bits in the array.
						numOfItems = (numOfItems + 7)/8;

						copyString(strTemp, (decodedData + counter), numOfItems);

						strTemp[numOfItems] = ' ';

						temp = (strTemp + numOfItems + 1);
						strcpy(temp, strBuff);
						
						counter += numOfItems;
											
						col.setCharData(strTemp, status);
						
						break;
					
					
					case UnsignedByteType:
						{
							Values v;
							char * nullValue;
							int nullVal;
							bool nullPresent = false;
							if((f.getValues(v, 0, status)) == SUCCESS)
							{
								if((v.getNull(nullValue, status)) == SUCCESS)
								{
									nullPresent = true;
									nullVal = atoi(nullValue);
								}
							}
										
							for(int i=0; i< numOfItems; i++)
							{								
								if(nullPresent)
								{
									if(((int)*((unsigned char *)(decodedData + counter + i))) == nullVal)
									{
										strcpy(strBuff, "missing");
									}
									else 
									{
										_itoa(((int)*((unsigned char *)(decodedData + counter + i))), strBuff, 10);
									}
								}
								else 
								{
									_itoa(((int)*((unsigned char *)(decodedData + counter + i))), strBuff, 10);
								}
								if(i == 0)
								{
									strcpy(strTemp, strBuff);
								}
								else 
								{
									strTemp = strcat(strTemp, " ");
									strTemp = strcat(strTemp, strBuff);								
								}

							}
							counter += numOfItems;
												
							col.setCharData(strTemp, status);

							break;
						}

					case BooleanType:
					case CharType:
		
						copyString(strTemp, (decodedData + counter), numOfItems);

						strTemp[numOfItems] = 0;

						// If the length of the String is less than the actual no of elements
						// then some of the elements contain NULL characters which 
						// corresponds to missing data
						if(strlen(strTemp) < numOfItems)
						{
							strcpy(strTemp, "missing");
						}
						
						counter += numOfItems;
											
						col.setCharData(strTemp, status);

						break;

						

					case UnicodeCharType:
						{
							unsigned short *unicodeData = new unsigned short[numOfItems + 1];
							for(int i=0; i< numOfItems; i++)
							{	
								unicodeData[i] = (*(unsigned short *)(decodedData + counter + i*2));
								
							}
							
							counter += numOfItems * 2;

							col.setUnicodeData(unicodeData, numOfItems, status);

							break;
						}	
					

					case ShortType:
						{
							Values v;
							char * nullValue;
							int nullVal;
							bool nullPresent = false;
							if((f.getValues(v, 0, status)) == SUCCESS)
							{
								if((v.getNull(nullValue, status)) == SUCCESS)
								{
									nullPresent = true;
									nullVal = atoi(nullValue);
								}
							}

							for(int i=0; i< numOfItems; i++)
							{
								c = decodedData[counter + i*2];
								decodedData[counter + i*2] = decodedData[counter + i*2 + 1];
								decodedData[counter + i*2 + 1] = c;
								
								if(nullPresent)
								{
									if((*((short *)(decodedData + counter + i*2))) == nullVal)
									{
										strcpy(strBuff, "missing");
									}
									else 
									{
										_itoa((*((short *)(decodedData + counter + i*2))), strBuff, 10);
									}
								}
								else 
								{
									_itoa((*((short *)(decodedData + counter + i*2))), strBuff, 10);
								}
								if(i == 0)
								{
									strcpy(strTemp, strBuff);
								}
								else 
								{
									strTemp = strcat(strTemp, " ");
									strTemp = strcat(strTemp, strBuff);								
								}

							}

							counter += numOfItems * 2;
							
							col.setCharData(strTemp, status);		
					
							break;
						}
				
					case IntType:
						{
							Values v;
							char * nullValue;
							int nullVal;
							bool nullPresent = false;
							if((f.getValues(v, 0, status)) == SUCCESS)
							{
								if((v.getNull(nullValue, status)) == SUCCESS)
								{
									nullPresent = true;
									nullVal = atoi(nullValue);
								}
							}

							for(int i=0; i< numOfItems; i++)
							{
								c = decodedData[counter + i*4];
								decodedData[counter + i*4] = decodedData[counter + i*4 + 3];
								decodedData[counter + i*4 + 3] = c;

								c = decodedData[counter + i*4 + 1];
								decodedData[counter + i*4 + 1] = decodedData[counter + i*4 + 2];
								decodedData[counter + i*4 + 2] = c;

								
								if(nullPresent)
								{
									if((*((int *)(decodedData + counter + i*4))) == nullVal)
									{
										strcpy(strBuff, "missing");
									}
									else 
									{
										_itoa((*((int *)(decodedData + counter + i*4))), strBuff, 10);
									}
								}
								else 
								{
									_itoa((*((int *)(decodedData + counter + i*4))), strBuff, 10);
								}
								
								if(i == 0)
								{
									strcpy(strTemp, strBuff);
								}
								else 
								{
									strTemp = strcat(strTemp, " ");
									strTemp = strcat(strTemp, strBuff);								
								}
							}
					
							
							counter += numOfItems * 4;
							
							col.setCharData(strTemp, status);
										
							break;
						}

					case FloatType:
						{

						strcpy(strTemp, "");
						for(int i=0; i< numOfItems; i++)
						{
							c = decodedData[counter + i*4];
							decodedData[counter + i*4] = decodedData[counter + i*4 + 3];
							decodedData[counter + i*4 + 3] = c;

							c = decodedData[counter + i*4 + 1];
							decodedData[counter + i*4 + 1] = decodedData[counter + i*4 + 2];
							decodedData[counter + i*4 + 2] = c;

							if(_isnan((*((float *)(decodedData + counter + i*4)))))
							{
								strcpy(strBuff, "missing");
							}
							else 
							{
								_gcvt((*((float *)(decodedData + counter + i*4))), 6, strBuff);
							}
							if(i == 0)
							{
								strcpy(strTemp, strBuff);
							}
							else 
							{
								strTemp = strcat(strTemp, " ");
								strTemp = strcat(strTemp, strBuff);								
							}
						}
				
						counter += numOfItems * 4;
						
						col.setCharData(strTemp, status);
																	
						break;

						}

					case LongType:
						{
							Values v;
							char * nullValue;
							int nullVal;
							bool nullPresent = false;
							if((f.getValues(v, 0, status)) == SUCCESS)
							{
								if((v.getNull(nullValue, status)) == SUCCESS)
								{
									nullPresent = true;
									nullVal = atoi(nullValue);
								}
							}
						
							temp = strTemp;
							for(int i=0; i< numOfItems; i++)
							{
								c = decodedData[counter + i*8];
								decodedData[counter + i*8] = decodedData[counter + i*8 + 7];
								decodedData[counter + i*8 + 7] = c;

								c = decodedData[counter + i*8 + 1];
								decodedData[counter + i*8 + 1] = decodedData[counter + i*8 + 6];
								decodedData[counter + i*8 + 6] = c;

								c = decodedData[counter + i*8 + 2];
								decodedData[counter + i*8 + 2] = decodedData[counter + i*8 + 5];
								decodedData[counter + i*8 + 5] = c;

								c = decodedData[counter + i*8 + 3];
								decodedData[counter + i*8 + 3] = decodedData[counter + i*8 + 4];
								decodedData[counter + i*8 + 4] = c;

								if(nullPresent)
								{
									if((*((long *)(decodedData + counter + i*8))) == nullVal)
									{
										strcpy(strBuff, "missing");
									}
									else 
									{
										_ltoa((*((long *)(decodedData + counter + i*8))), strBuff, 10);
									}
								}
								else 
								{
									_ltoa((*((long *)(decodedData + counter + i*8))), strBuff, 10);
								}
								if(i == 0)
								{
									strcpy(strTemp, strBuff);
								}
								else 
								{
									strTemp = strcat(strTemp, " ");
									strTemp = strcat(strTemp, strBuff);								
								}
							}

							counter += numOfItems * 8;
							
							col.setCharData(strTemp, status);
												
							break;
						}

					
					case DoubleType:
					case FloatComplexType:
						{
						temp = strTemp;
						for(int i=0; i< numOfItems; i++)
						{
							c = decodedData[counter + i*8];
							decodedData[counter + i*8] = decodedData[counter + i*8 + 7];
							decodedData[counter + i*8 + 7] = c;

							c = decodedData[counter + i*8 + 1];
							decodedData[counter + i*8 + 1] = decodedData[counter + i*8 + 6];
							decodedData[counter + i*8 + 6] = c;

							c = decodedData[counter + i*8 + 2];
							decodedData[counter + i*8 + 2] = decodedData[counter + i*8 + 5];
							decodedData[counter + i*8 + 5] = c;

							c = decodedData[counter + i*8 + 3];
							decodedData[counter + i*8 + 3] = decodedData[counter + i*8 + 4];
							decodedData[counter + i*8 + 4] = c;

							if(_isnan((*((double *)(decodedData + counter + i*8)))))
							{
								strcpy(strBuff, "missing");
							}
							else 
							{
								_gcvt((*((double *)(decodedData + counter + i*8))), 6, strBuff);
							}
							if(i == 0)
							{
								strcpy(strTemp, strBuff);
							}
							else 
							{
								strTemp = strcat(strTemp, " ");
								strTemp = strcat(strTemp, strBuff);								
							}
						}

						counter += numOfItems * 8;
						
						col.setCharData(strTemp, status);
											
						break;
						}


					case DoubleComplexType:
						{
						temp = strTemp;
						for(int i=0; i< numOfItems; i++)
						{
							c = decodedData[counter + i*16];
							decodedData[counter + i*16] = decodedData[counter + i*16 + 15];
							decodedData[counter + i*16 + 15] = c;

							c = decodedData[counter + i*16 + 1];
							decodedData[counter + i*16 + 1] = decodedData[counter + i*16 + 14];
							decodedData[counter + i*16 + 14] = c;

							c = decodedData[counter + i*16 + 2];
							decodedData[counter + i*16 + 2] = decodedData[counter + i*16 + 13];
							decodedData[counter + i*16 + 13] = c;

							c = decodedData[counter + i*16 + 3];
							decodedData[counter + i*16 + 3] = decodedData[counter + i*16 + 12];
							decodedData[counter + i*16 + 12] = c;

							c = decodedData[counter + i*16 + 4];
							decodedData[counter + i*16 + 4] = decodedData[counter + i*16 + 11];
							decodedData[counter + i*16 + 11] = c;

							c = decodedData[counter + i*16 + 5];
							decodedData[counter + i*16 + 5] = decodedData[counter + i*16 + 10];
							decodedData[counter + i*16 + 10] = c;

							c = decodedData[counter + i*16 + 6];
							decodedData[counter + i*16 + 6] = decodedData[counter + i*16 + 9];
							decodedData[counter + i*16 + 9] = c;

							c = decodedData[counter + i*16 + 7];
							decodedData[counter + i*16 + 7] = decodedData[counter + i*16 + 8];
							decodedData[counter + i*16 + 8] = c;

							if(_isnan((*((double *)(decodedData + counter + i*16)))))
							{
								strcpy(strBuff, "missing");
							}
							else 
							{
								_gcvt((*((double *)(decodedData + counter + i*16))), 6, strBuff);
							}
							if(i == 0)
							{
								strcpy(strTemp, strBuff);
							}
							else 
							{
								strTemp = strcat(strTemp, " ");
								strTemp = strcat(strTemp, strBuff);								
							}
						}

						counter += numOfItems * 16;
						
						col.setCharData(strTemp, status);
											
						break;
						}
						
				
				}

				columnList.push_back(col);

				delete strTemp;

			}

			r.setColumns(columnList, &result);

			rowList.push_back(r);
		}
		
												
#ifdef  DEBUG_MODE
		cout << "Data in Binary Stream is " << decodedStream << endl;
#endif

	}
	return SUCCESS;
}



/*
* Extract information from 'Fits' node.
*/
int XPathHelper::getFitsData(const XalanNode *node, FitsData &fd,
						 const vector<Field> &fields, int *status)
{
	int result = 0;
	// get attributes of Values node
	getFitsAttributes(node, fd, &result);
	
	XalanDOMString	name;
	
	const XalanNode * childNode = node->getFirstChild();
	const XalanNode * siblingNode;
		
	Stream st;
	siblingNode = childNode;
		

	//get all siblings (not using getChildNodes since it is not working) :-(
	while (siblingNode != NULL) 
	{
		Row r;
		// process only element nodes, not text nodes.
		// To do - find some way of avoiding text nodes.

		//cout << "Next node name is " << siblingNode->getNodeName() << endl;
		//cout << "Next node type is " << siblingNode->getNodeType() << endl;
		if (siblingNode->getNodeType() == XalanNode::ELEMENT_NODE)
		{
			name = siblingNode->getNodeName();
			// cout << "Node name is " << siblingNode->getNodeName() << endl;

			// check if node is 'Option' node
			if (name.compare(((const XalanDOMString)ELE_STREAM)) == 0) {
										
				getFitsStream(siblingNode, st, fields, &result);
				// rowList.push_back(r);
			} 
			
		}
		siblingNode = siblingNode->getNextSibling();
	}
	
	fd.setStream(st, &result);
	*status = SUCCESS;
	return SUCCESS;

}



/*
* Extract information from 'Stream' node.
*/
int XPathHelper::getFitsStream(const XalanNode *node, Stream &st,
						const vector<Field> &fields, int *status)
{
	int result = 0;
	getStreamAttributes(node, st, &result);
			
	XalanDOMString	name;
	
	const XalanNode * childNode = node->getFirstChild();
			
	XMLByte * data = NULL;
	
		
	if (childNode == NULL)
	{
#ifdef  DEBUG_MODE
		cout << "TD has no child nodes." << endl;		
#endif
	} 		
	return SUCCESS;
}

/*
 * The strncpy function either copies first n chars or upto the null char. Hence we need
 * this function to copy the binary string
 */

int XPathHelper::copyString(char * strDest, const char * strSrc, unsigned int numBytes)
{
	int count = 0;

	while(count < numBytes)
	{
		strDest[count] = strSrc[count];
		count++;
	}
	strDest[numBytes] = 0;
	return SUCCESS;
}
