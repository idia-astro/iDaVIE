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
#include "VTable.h"
#include "VOUtils.h"
#include <iostream>
using namespace std;

bool VTable::m_isBinary;

/*
* This class represents the Virtual Table. A virtual
* table is a memory representation of a Table in VOTable. 
* 
* A VTable consists of metadata and data.
*
* Date created - 02 May 2002
*
* Date modified - 22 Dec 2004
*
*/

VTable::VTable()
{
	init();
}

/*VTable::VTable(TableMetaData tmd, TableData td, char *ID, char *name, char *ref)
{
}*/

// Opens a VTable in memory. 
int VTable::openFile(const char * filename, const char * path, int iomode, int * status)
{
	init();
	if (VOUtils::checkIfFileExists(filename) == VOERROR)
	{
		*status = FILE_ERROR;
		return *status;
	}

	*status = VOERROR;
	XPathHelper xh;
	VTable v;

	xh.getVTableTree(v, filename, path, status);
	(*this) = v;
	
	return *status;
}

/*
* Opens a VTable element from the given VOTABlE file,
* the xpath denotes the position of the resource element to be
* opened.
*/
int VTable::loadVOTableFromMemory(const char * buffer, int bufferLength,
				const char * systemID, const char * xpath, int * status)
{
	XPathHelper xh;
	VTable v;

	xh.getVTableTree(v, buffer, bufferLength, systemID, xpath, status);
	(*this) = v;
		
	return *status;
}


/* Constructor when the source is local file
*/

VTable::VTable(const char * filename, const char * path, int iomode, int * status)
{
	openFile(filename, path, iomode, status);
}

VTable::VTable(const char * buffer, int bufferLength, const char * systemID,
			   const char * xpath, int * status)
{
	loadVOTableFromMemory(buffer, bufferLength, systemID, xpath, status);
}

VTable::~VTable()
{
	cleanup();
}

VTable VTable::operator=(const VTable &v)
{
	if (this != &v)
	{
		cleanup();
		init();
		makecopy(v);
	}
	return *this;
}

VTable::VTable(const VTable &v)
{
	init();
	makecopy(v);
}

void VTable::init(void)
{
	m_ID = NULL;
	m_name = NULL;
	m_ref = NULL;
	m_utype = NULL;
}

void VTable::makecopy(const VTable &v)
{
	m_tmd = v.m_tmd; //Meta data
	m_td = v.m_td; // Data
	m_bd = v.m_bd;
	m_fd = v.m_fd;

	//try 
	{
		if (NULL != v.m_ID)
		{
			m_ID = new char[strlen(v.m_ID) + 1];
			strcpy(m_ID, v.m_ID);
		}

		if (NULL != v.m_utype)
		{
			m_utype = new char[strlen(v.m_utype) + 1];
			strcpy(m_utype, v.m_utype);
		}

		if (NULL != v.m_name)
		{
			m_name = new char[strlen(v.m_name) + 1];
			strcpy(m_name, v.m_name);
		}

		if (NULL != v.m_ref)
		{
			m_ref = new char[strlen(v.m_ref) + 1];
			strcpy(m_ref, v.m_ref);
		}
	} 
	//catch  (bad_alloc ex)
	//{
		// Ignore ??
	//}
}

void VTable::cleanup ()
{
	delete[] m_ID;
	delete[] m_name;
	delete[] m_ref;
	delete[] m_utype;
	
}

// Destroyes the VTable.
int VTable::closeFile(int * status)
{
	cleanup();
	init();
	m_tmd.cleanup();
	m_tmd.init();
	m_td.cleanup();
	m_bd.cleanup();
	m_fd.cleanup();

	(*status) = SUCCESS;
	return SUCCESS;
}

// Set MetaData
int VTable::setMetaData(TableMetaData tmd, int * status)
{
	m_tmd = tmd;
	return SUCCESS;
}

// Set Table Data
int VTable::setData(TableData td, int * status)
{
	m_td = td;
	return SUCCESS;
}


// Set Binary Data
int VTable::setBinaryData(BinaryData bd, int * status)
{
	m_bd = bd;
	return SUCCESS;
}


// Set Fits Data
int VTable::setFitsData(FitsData fd, int * status)
{
	m_fd = fd;
	return SUCCESS;
}

// Set Table name
int VTable::setName(char * name, int * status)
{
	delete[] m_name;
	VOUtils::copyString(m_name, name, status);
	//m_name = name;
	return SUCCESS;
}

// Set Table ID.
int VTable::setID(char * ID, int * status)
{
	delete[] m_ID;
	VOUtils::copyString(m_ID, ID, status);
	//m_ID = ID;
	return SUCCESS;
}

// Set Table utype.
int VTable::setUtype(char * utype, int * status)
{
	delete[] m_utype;
	VOUtils::copyString(m_utype, utype, status);
	//m_utype = utype;
	return SUCCESS;
}

// Set Table Reference
int VTable::setRef(char * ref, int * status)
{
	delete[] m_ref;
	VOUtils::copyString(m_ref, ref, status);
	//m_ref = ref;
	return SUCCESS;
}


// Set Table Reference
int VTable::setBinaryFlag(bool flag, int * status)
{
	VTable::m_isBinary = flag;
	return SUCCESS;
}


int VTable::getBinaryFlag(bool &flag, int * status)
{
	flag = VTable::m_isBinary;
	*status = SUCCESS;
	return SUCCESS;
}


// Get MetaData
int VTable::getMetaData(TableMetaData &t, int * status)
{
	t = m_tmd;
	*status = SUCCESS;
	return SUCCESS;
}

// Get Table Data
int VTable::getData(TableData &td, int * status)
{
	td = m_td;
	*status = SUCCESS;
	return SUCCESS;
}


// Get Binary Data
int VTable::getBinaryData(BinaryData &bd, int * status)
{
	bd = m_bd;
	*status = SUCCESS;
	return SUCCESS;
}


// Get Fits Data
int VTable::getFitsData(FitsData &fd, int * status)
{
	fd = m_fd;
	*status = SUCCESS;
	return SUCCESS;
}

// Get Table name
int VTable::getName(char * &name, int * status)
{
	VOUtils::copyString(name, m_name, status);
	return *status;
}

// Get Table ID.
int VTable::getID(char * &ID, int * status)
{
	VOUtils::copyString(ID, m_ID, status);
	return *status;
}

// Get Table utype.
int VTable::getUtype(char * &utype, int * status)
{
	VOUtils::copyString(utype, m_utype, status);
	return *status;
}


// Get Table Reference
int VTable::getRef(char * &ref, int * status)
{
	VOUtils::copyString(ref, m_ref, status);	
	return *status;
}


