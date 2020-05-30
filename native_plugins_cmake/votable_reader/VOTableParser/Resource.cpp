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
#include "Resource.h"
#include "VOUtils.h"
#include "VOUtils.h"

/*
* This class represents the 'RESOURCE' element.  
*
* A RESOURCE element contains Description, list of 'Info' elements,
* list of 'Coosys' elements, list of 'Param' elements, list of
* 'Link' elements, list of 'Table' elements, and 'list' of Resource 
* elements.
* 
* Date created - 27 May 2002
*
*/


/*
* Default constructor
*/
Resource::Resource()
{
	init();
}

Resource::~Resource()
{
	cleanup();
}

Resource Resource::operator=(const Resource &r)
{
	if (this != &r)
	{
		cleanup();
		init();
		makecopy(r);
	}
	return (*this);
}

Resource::Resource(const Resource &r)
{
	init();
	makecopy(r);
}

Resource::Resource(const char * filename, 
			const char * path, int iomode, int * status)
{
	init();
	openFile(filename, path, iomode, status);
}


Resource::Resource(const char * buffer, int bufferLength,
				const char * systemID, const char * xpath, int * status)
{
	init();
	loadVOTableFromMemory(buffer, bufferLength, systemID, xpath, status);
}

/*
* Opens a Resource element from the given VOTABlE file,
* the xpath denotes the position of the resource element to be
* opened.
*/
int Resource::openFile(const char * filename, 
	const char * xpath, int iomode, int * status)
{
	if (VOUtils::checkIfFileExists(filename) == VOERROR)
	{
		*status = FILE_ERROR;
		//return *status;
	}
	else
	{

		*status = VOERROR;
		XPathHelper xh;
		Resource r;

		xh.getResourceTree(r, filename, xpath, status);
		(*this) = r;
	}
	
	return *status;
}


/*
* Opens a Resource element from the given VOTABlE file,
* the xpath denotes the position of the resource element to be
* opened.
*/
int Resource::loadVOTableFromMemory(const char * buffer, int bufferLength,
				const char * systemID, const char * xpath, int * status)
{
	XPathHelper xh;
	Resource r;

	xh.getResourceTree(r, buffer, bufferLength, systemID, xpath, status);
	(*this) = r;
		
	return *status;
}

/*
* Closes the 'Resource' element.
*/
int Resource::closeFile(int *status)
{
	cleanup();
	init();

	if (!m_infoList.empty ()) 
		m_infoList.clear ();

	if (!m_coosysList.empty ()) 
		m_coosysList.clear ();

	if (!m_paramList.empty ()) 
		m_paramList.clear ();

	if (!m_linkList.empty ()) 
		m_linkList.clear ();

	if (!m_vtableList.empty ()) 
		m_vtableList.clear ();

	if (!m_resourceList.empty ()) 
		m_resourceList.clear ();

	*status = SUCCESS;
	return *status;
	
}

int Resource::setDesc(char * str, int * status)
{
	delete[] m_desc;
	VOUtils::copyString(m_desc, str, status);
	//m_desc = str;
	return SUCCESS;
}

int Resource::setID(char * str, int * status)
{
	delete[] m_ID;
	VOUtils::copyString(m_ID, str, status);
	//m_ID = str;
	return SUCCESS;
}


int Resource::setUtype(char * utype, int * status)
{
	delete[] m_utype;
	VOUtils::copyString(m_utype, utype, status);
	//m_utype = utype;
	return SUCCESS;
}

int Resource::setName(char * str, int * status)
{
	delete[] m_name;
	VOUtils::copyString(m_name, str, status);
	//m_name = str;
	return SUCCESS;
}

int Resource::setType(resource_type t, int * status)
{
	m_type = t;
	return SUCCESS;
}

int Resource::setInfos(vector <Info> infoList, int * status)
{
	m_infoList = infoList;
	return SUCCESS;
}

int Resource::setCoosystems(vector <Coosys> list, int * status)
{
	m_coosysList = list;
	return SUCCESS;
}

int Resource::setParams(vector <Param> list, int * status)
{
	m_paramList = list;
	return SUCCESS;
}

int Resource::setLinks(vector <Link> list, int * status)
{
	m_linkList = list;
	return SUCCESS;
}

int Resource::setTables(vector <VTable> list, int * status)
{
	m_vtableList = list;
	return SUCCESS;
}

int Resource::setResources(vector <Resource> list, int * status)
{
	m_resourceList = list;
	return SUCCESS;
}

/*
* Get the 'description' of resource.
*/
int Resource::getDescription(char * &description, int * status)
{
	VOUtils::copyString(description, m_desc, status);
	return *status;
}

/*
* Get the 'ID' of resource.
*/
int Resource::getID(char * &newID, int * status)
{
	VOUtils::copyString(newID, m_ID, status);
	return *status;
}


/*
* Get the 'utype' of resource.
*/
int Resource::getUtype(char * &utype, int * status)
{
	VOUtils::copyString(utype, m_utype, status);
	return *status;
}

/*
* Get the 'name' of resource.
*/
int Resource::getName(char * &name, int * status)
{
	VOUtils::copyString(name, m_name, status);
	return *status;
}

/*
* Get the 'value' of resource.
*/
int Resource::getType(resource_type &t, int * status)
{
	t = m_type;
	*status = SUCCESS;

	return *status;
}


/*
* Get the number of 'Info' elements.
*/
int Resource::getNumOfInfos(int &numOfElements, int * status)
{
	numOfElements = m_infoList.size();
	*status = SUCCESS;
	return *status;
}

/*
* Get the 'Info' element, given the index.
*/
int Resource::getInfo(Info &info, int index, int * status)
{
	*status = VOERROR;
	if (index >= 0 && index < m_infoList.size())
	{
		try
		{
			info = m_infoList[index];
			*status = SUCCESS;
		} 
		catch (bad_alloc ex)
		{
			*status = INSUFFICIENT_MEMORY_ERROR;
		}
	}
	
	return *status;
}

/*
* Get the number of 'Coosys' elements.
*/
int Resource::getNumOfCoosys(int &numOfElements, int * status)
{
	numOfElements = m_coosysList.size();
	*status = SUCCESS;
	return *status;
}

/*
* Get the 'Coosys' element, given the index.
*/
int Resource::getCoosys(Coosys &coosys, int index, int * status)
{
	*status = VOERROR;
	if (index >= 0 && index < m_coosysList.size())
	{
		try
		{
			coosys = m_coosysList[index];
			*status = SUCCESS;
		} 
		catch (bad_alloc ex)
		{
			*status = INSUFFICIENT_MEMORY_ERROR;
		}

	}
	
	return *status;
}


/*
* Get the number of 'Param' elements.
*/
int Resource::getNumOfParams(int &numOfElements, int * status)
{
	numOfElements = m_paramList.size();
	*status = SUCCESS;
	return *status;
}

/*
* Get the 'Param' element, given the index.
*/
int Resource::getParam(Param &param, int index, int * status)
{
	*status = VOERROR;
	if (index >= 0 && index < m_paramList.size())
	{
		try
		{
			param = m_paramList[index];
			*status = SUCCESS;
		} 
		catch (bad_alloc ex)
		{
			*status = INSUFFICIENT_MEMORY_ERROR;
		}
	}
	
	return *status;
}


/*
* Get the number of 'Link' elements.
*/
int Resource::getNumOfLinks(int &numOfElements, int * status)
{
	numOfElements = m_linkList.size();
	*status = SUCCESS;
	return *status;
}

/*
* Get the 'Link' element, given the index.
*/
int Resource::getLink(Link &link, int index, int * status)
{
	*status = VOERROR;
	if (index >= 0 && index < m_linkList.size())
	{
		try 
		{
			link = m_linkList[index];
			*status = SUCCESS;
		} 
		catch (bad_alloc ex)
		{
			*status = INSUFFICIENT_MEMORY_ERROR;
		}
	}
	
	return *status;
}

/*
* Get the number of 'VTable' elements.
*/
int Resource::getNumOfTables(int &numOfElements, int * status)
{
	numOfElements = m_vtableList.size();
	*status = SUCCESS;
	return *status;
}

/*
* Get the 'VTable' element, given the index.
*/
int Resource::getTable(VTable &table, int index, int * status)
{
	*status = VOERROR;
	if (index >= 0 && index < m_vtableList.size())
	{
		try
		{
			table = m_vtableList[index];
			*status = SUCCESS;
		} 
		catch (bad_alloc ex)
		{
			*status = INSUFFICIENT_MEMORY_ERROR;
		}
	}
	
	return *status;
}


/*
* Get the number of 'Resource' elements.
*/
int Resource::getNumOfResources(int &numOfElements, int * status)
{
	numOfElements = m_resourceList.size();
	*status = SUCCESS;
	return *status;
}

/*
* Get the 'Resource' element, given the index.
*/
int Resource::getResource(Resource &resource, int index, int * status)
{
	*status = VOERROR;
	if (index >= 0 && index < m_resourceList.size())
	{
		try
		{
			resource = m_resourceList[index];
			*status = SUCCESS;
		} 
		catch (bad_alloc ex)
		{
			*status = INSUFFICIENT_MEMORY_ERROR;
		}

	}
	
	return *status;
}


void Resource::cleanup()
{
	delete[] m_desc;
	delete[] m_ID;
	delete[] m_name;
	delete[] m_utype;

}

void Resource::makecopy(const Resource &r)
{
	m_infoList = r.m_infoList;
	m_coosysList = r.m_coosysList;
	m_paramList = r.m_paramList;
	m_linkList = r.m_linkList;
	m_vtableList = r.m_vtableList;
	m_resourceList = r.m_resourceList;

	m_type = r.m_type;

	//try 
	{
		if (NULL != r.m_desc)
		{
			m_desc = new char[strlen(r.m_desc) + 1];
			strcpy(m_desc, r.m_desc);
		}
		if (NULL != r.m_ID)
		{
			m_ID = new char[strlen(r.m_ID) + 1];
			strcpy(m_ID, r.m_ID);
		}

		if (NULL != r.m_utype)
		{
			m_utype = new char[strlen(r.m_utype) + 1];
			strcpy(m_utype, r.m_utype);
		}
		if (NULL != r.m_name)
		{
			m_name = new char[strlen(r.m_name) + 1];
			strcpy(m_name, r.m_name);
		}
		
	}
	//catch (bad_alloc ex)
	//{
		// ignore ??
	//}

}

void Resource::init(void)
{
	m_desc = NULL;
	m_ID = NULL;
	m_utype = NULL;
	m_name = NULL;
	m_type = results;
	
}

