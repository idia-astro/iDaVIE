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
* This class represents the metadata in a Table.  
* 
* TableMetaData consists of the description, 
* Fields collection, Link collection, 
*
* Date created - 03 May 2002
*
*/

#include "XPathHelper.h"
#include "TableMetaData.h"
#include "VOUtils.h"

TableMetaData::TableMetaData()
{
	init();
}

/*TableMetaData::TableMetaData(Field field[], int numOfFields, Link link[], 
			  int numOfLinks, char *desc)
{
}*/

TableMetaData::~TableMetaData()
{
	cleanup();
}

void TableMetaData::init ()
{
	m_description = NULL;
}

TableMetaData TableMetaData::operator=(const TableMetaData &t)
{
	
	if (this != &t)
	{
		cleanup();
		init();
		makecopy(t);
	}
	return *this;
}

TableMetaData::TableMetaData(const TableMetaData &t)
{

	init();
	makecopy(t);
}

void TableMetaData::cleanup()
{
	if (! m_fieldList.empty())
	{
		m_fieldList.clear();
	}

	if (! m_paramList.empty())
	{
		m_paramList.clear();
	}

	if (! m_groupList.empty())
	{
		m_groupList.clear();
	}

	if (! m_linkList.empty())
	{
		m_linkList.clear();
	}
	
	delete[] m_description;
	
	
}

void TableMetaData::makecopy(const TableMetaData &t)
{
	m_fieldList = t.m_fieldList;
	m_paramList = t.m_paramList;
	m_linkList = t.m_linkList;
	m_groupList = t.m_groupList;
	//try 
	{
		if (NULL != t.m_description)
		{
			m_description = new char[strlen(t.m_description) + 1];
			strcpy(m_description, t.m_description);
		}
	} 
	//catch  (bad_alloc ex)
	//{
		// Ignore ??
	//}
}

int TableMetaData::setDesciption(char *desc, int *status)
{
	delete[] m_description;
	VOUtils::copyString(m_description, desc, status);
	//m_description = desc;
	return SUCCESS;
}

int TableMetaData::setFields(vector<Field> f, int *status)
{
	m_fieldList = f;
	return SUCCESS;
}

int TableMetaData::setParams(vector<Param> p, int *status)
{
	m_paramList = p;
	return SUCCESS;
}


int TableMetaData::setLinks(vector<Link> l, int *status)
{
	m_linkList = l;
	return SUCCESS;
}

int TableMetaData::setGroups(vector<Group> g, int *status)
{
	m_groupList = g;
	return SUCCESS;
}

int TableMetaData::getNumOfColumns(int  &ncols, int *status)
{
	ncols = m_fieldList.size();
	return SUCCESS;
}

int TableMetaData::getNumOfParams(int  &nParams, int *status)
{
	nParams = m_paramList.size();
	return SUCCESS;
}

int TableMetaData::getNumOfLinks(int  &nLinks, int *status)
{
	nLinks = m_linkList.size();
	return SUCCESS;
}


int TableMetaData::getNumOfGroups(int  &nGroups, int *status)
{
	nGroups = m_groupList.size();
	return SUCCESS;
}

int TableMetaData::getField(Field &field, int fieldNum, int *status)
{
	*status = VOERROR;
	if (fieldNum >= 0 && fieldNum < m_fieldList.size())
	{
		try
		{
			field = m_fieldList[fieldNum];
			*status = SUCCESS;
		} 
		catch (bad_alloc ex)
		{
			*status = INSUFFICIENT_MEMORY_ERROR;
		}
	}
	
	return *status;
}


int TableMetaData::getParam(Param &param, int paramNum, int *status)
{
	*status = VOERROR;
	if (paramNum >= 0 && paramNum < m_paramList.size())
	{
		try
		{
			param = m_paramList[paramNum];
			*status = SUCCESS;
		} 
		catch (bad_alloc ex)
		{
			*status = INSUFFICIENT_MEMORY_ERROR;
		}
	}
	
	return *status;
}

int TableMetaData::getLink(Link &link, int linkNum, int *status)
{
	*status = VOERROR;
	if (linkNum >= 0 && linkNum < m_linkList.size())
	{
		try
		{
			link = m_linkList[linkNum];
			*status = SUCCESS;
		} 
		catch (bad_alloc ex)
		{
			*status = INSUFFICIENT_MEMORY_ERROR;
		}
	}
	
	return *status;
}


int TableMetaData::getGroup(Group &group, int groupNum, int *status)
{
	*status = VOERROR;
	if (groupNum >= 0 && groupNum < m_groupList.size())
	{
		try
		{
			group = m_groupList[groupNum];
			*status = SUCCESS;
		} 
		catch (bad_alloc ex)
		{
			*status = INSUFFICIENT_MEMORY_ERROR;
		}
	}
	
	return *status;
}

int TableMetaData::getDescription(char * &desc, int *status)
{
	VOUtils::copyString(desc, m_description, status);
	return *status;
}
