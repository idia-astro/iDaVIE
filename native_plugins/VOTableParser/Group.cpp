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
#include "Group.h"
#include "VOUtils.h"

/*
* This class represents the 'Group' element.  
*
* A Group element contains Description, list of 'FieldRef' elements,
* list of 'ParamRef' elements, list of 'Param' elements, 
* and 'list' of Group 
* elements.
* 
* Date created - 3 Jan 2005
*
*/


/*
* Default constructor
*/
Group::Group()
{
	init();
}

Group::~Group()
{
	cleanup();
}

Group Group::operator=(const Group &g)
{
	if (this != &g)
	{
		cleanup();
		init();
		makecopy(g);
	}
	return (*this);
}

Group::Group(const Group &g)
{
	init();
	makecopy(g);
}


int Group::setDescription(char * desc, int * status)
{
	delete[] m_description;
	VOUtils::copyString(m_description, desc, status);
	//m_description = desc;
	return SUCCESS;
}

int Group::setID(char * ID, int * status)
{
	delete[] m_ID;
	VOUtils::copyString(m_ID, ID, status);
	//m_ID = str;
	return SUCCESS;
}


int Group::setUtype(char * utype, int * status)
{
	delete[] m_utype;
	VOUtils::copyString(m_utype, utype, status);
	//m_utype = utype;
	return SUCCESS;
}

int Group::setName(char * str, int * status)
{
	delete[] m_name;
	VOUtils::copyString(m_name, str, status);
	//m_name = str;
	return SUCCESS;
}

int Group::setRef(char * str, int * status)
{
	delete[] m_ref;
	VOUtils::copyString(m_ref, str, status);
	//m_ref = str;
	return SUCCESS;
}

int Group::setUcd(char * ucd, int * status)
{
	delete[] m_ucd;
	VOUtils::copyString(m_ucd, ucd, status);
	//m_ucd = ucd;
	return SUCCESS;
}


int Group::setParams(vector <Param> list, int * status)
{
	m_paramList = list;
	return SUCCESS;
}

int Group::setParamRefs(vector <ParamRef> list, int * status)
{
	m_paramRefList = list;
	return SUCCESS;
}


int Group::setFieldRefs(vector <FieldRef> list, int * status)
{
	m_fieldRefList = list;
	return SUCCESS;
}


int Group::setGroups(vector <Group> list, int * status)
{
	m_groupList = list;
	return SUCCESS;
}

/*
* Get the 'description' of Group.
*/
int Group::getDescription(char * &description, int * status)
{
	VOUtils::copyString(description, m_description, status);
	return *status;
}

/*
* Get the 'ID' of Group.
*/
int Group::getID(char * &newID, int * status)
{
	VOUtils::copyString(newID, m_ID, status);
	return *status;
}


/*
* Get the 'utype' of Group.
*/
int Group::getUtype(char * &utype, int * status)
{
	VOUtils::copyString(utype, m_utype, status);
	return *status;
}

/*
* Get the 'name' of Group.
*/
int Group::getName(char * &name, int * status)
{
	VOUtils::copyString(name, m_name, status);
	return *status;
}


/*
* Get the 'ref' of Group.
*/
int Group::getRef(char * &ref, int * status)
{
	VOUtils::copyString(ref, m_ref, status);
	return *status;
}

/*
* Get the 'ucd' of Group.
*/
int Group::getUcd(char * &ucd, int * status)
{
	VOUtils::copyString(ucd, m_ucd, status);
	return *status;
}



/*
* Get the number of 'Param' elements.
*/
int Group::getNumOfParams(int &numOfElements, int * status)
{
	numOfElements = m_paramList.size();
	*status = SUCCESS;
	return *status;
}

/*
* Get the 'Param' element, given the index.
*/
int Group::getParam(Param &param, int index, int * status)
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
* Get the number of 'paramRef' elements.
*/
int Group::getNumOfParamRefs(int &numOfElements, int * status)
{
	numOfElements = m_paramRefList.size();
	*status = SUCCESS;
	return *status;
}

/*
* Get the 'paramRef' element, given the index.
*/
int Group::getParamRef(ParamRef &paramRef, int index, int * status)
{
	*status = VOERROR;
	if (index >= 0 && index < m_paramRefList.size())
	{
		try
		{
			paramRef = m_paramRefList[index];
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
* Get the number of 'fieldRef' elements.
*/
int Group::getNumOfFieldRefs(int &numOfElements, int * status)
{
	numOfElements = m_fieldRefList.size();
	*status = SUCCESS;
	return *status;
}

/*
* Get the 'fieldRef' element, given the index.
*/
int Group::getFieldRef(FieldRef &fieldRef, int index, int * status)
{
	*status = VOERROR;
	if (index >= 0 && index < m_fieldRefList.size())
	{
		try
		{
			fieldRef = m_fieldRefList[index];
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
* Get the number of 'Group' elements.
*/
int Group::getNumOfGroups(int &numOfElements, int * status)
{
	numOfElements = m_groupList.size();
	*status = SUCCESS;
	return *status;
}

/*
* Get the 'Group' element, given the index.
*/
int Group::getGroup(Group &group, int index, int * status)
{
	*status = VOERROR;
	if (index >= 0 && index < m_groupList.size())
	{
		try
		{
			group = m_groupList[index];
			*status = SUCCESS;
		} 
		catch (bad_alloc ex)
		{
			*status = INSUFFICIENT_MEMORY_ERROR;
		}

	}
	
	return *status;
}


void Group::cleanup()
{
	delete[] m_description;
	delete[] m_ID;
	delete[] m_name;
	delete[] m_utype;
	delete[] m_ref;
	delete[] m_ucd;
}

void Group::makecopy(const Group &g)
{
	m_paramList = g.m_paramList;
	m_paramRefList = g.m_paramRefList;
	m_fieldRefList = g.m_fieldRefList;
	m_groupList = g.m_groupList;

	//try 
	{
		if (NULL != g.m_description)
		{
			m_description = new char[strlen(g.m_description) + 1];
			strcpy(m_description, g.m_description);
		}
		if (NULL != g.m_ID)
		{
			m_ID = new char[strlen(g.m_ID) + 1];
			strcpy(m_ID, g.m_ID);
		}

		if (NULL != g.m_utype)
		{
			m_utype = new char[strlen(g.m_utype) + 1];
			strcpy(m_utype, g.m_utype);
		}

		if (NULL != g.m_ucd)
		{
			m_ucd = new char[strlen(g.m_ucd) + 1];
			strcpy(m_ucd, g.m_ucd);
		}
		if (NULL != g.m_name)
		{
			m_name = new char[strlen(g.m_name) + 1];
			strcpy(m_name, g.m_name);
		}
		if (NULL != g.m_ref)
		{
			m_ref = new char[strlen(g.m_ref) + 1];
			strcpy(m_ref, g.m_ref);
		}
		
	}
	//catch (bad_alloc ex)
	//{
		// ignore ??
	//}

}

void Group::init(void)
{
	m_description = NULL;
	m_ID = NULL;
	m_utype = NULL;
	m_name = NULL;
	m_ucd = NULL;
	m_ref = NULL;
	
	
}

