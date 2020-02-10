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
* This class represents the FieldParam in the MetaData.  
* 
* A field contains description, a max of 2 values and a
* list of links.
*
* Date created - 03 May 2002
*
*/
#include "FieldParam.h"
#include "VOUtils.h"


void FieldParam::init()
{
	m_description = NULL;

	m_ID = NULL;
	m_unit = NULL;
	m_precision = NULL;
	m_width = 0;
	m_ref = NULL;
	m_utype = NULL;
	m_name = NULL;
	m_UCD = NULL;
	m_arraySize = NULL;
	
}

void FieldParam::makecopy(const FieldParam &f)
{

	m_datatype = f.m_datatype;
	m_width = f.m_width;
	m_values = f.m_values;
	m_linkList = f.m_linkList;


	//try 
	{
		if (NULL != f.m_ID)
		{
			m_ID = new char[strlen(f.m_ID) + 1];
			strcpy(m_ID, f.m_ID);
		}
		if (NULL != f.m_utype)
		{
			m_utype = new char[strlen(f.m_utype) + 1];
			strcpy(m_utype, f.m_utype);
		}

		if (NULL != f.m_unit)
		{
			m_unit = new char[strlen(f.m_unit) + 1];
			strcpy(m_unit, f.m_unit);
		}

		if (NULL != f.m_name)
		{
			m_name = new char[strlen(f.m_name) + 1];
			strcpy(m_name, f.m_name);
		}

		if (NULL != f.m_precision)
		{
			m_precision = new char[strlen(f.m_precision) + 1];
			strcpy(m_precision, f.m_precision);
		}

		if (NULL != f.m_ref)
		{
			m_ref = new char[strlen(f.m_ref) + 1];
			strcpy(m_ref, f.m_ref);
		}

		if (NULL != f.m_arraySize)
		{
			m_arraySize = new char[strlen(f.m_arraySize) + 1];
			strcpy(m_arraySize, f.m_arraySize);
		}

		if (NULL != f.m_UCD)
		{
			m_UCD = new char[strlen(f.m_UCD) + 1];
			strcpy(m_UCD, f.m_UCD);
		}

		if (NULL != f.m_description)
		{
			m_description = new char[strlen(f.m_description) + 1];
			strcpy(m_description, f.m_description);
		}
	
	} 
	//catch (bad_alloc ex) 
	//{
		// Ignore ?
	//}


}

void FieldParam::cleanup(void)
{
	delete[] m_ID;
	delete[] m_utype;
	delete[] m_unit;
	delete[] m_precision;
	delete[] m_ref;
	delete[] m_name;
	delete[] m_UCD;
	delete[] m_arraySize;
	delete[] m_description;

}

/*
* To be implemented in the subclasses.
*/
int FieldParam::setValue(char *s, int *status)
{
	// does nothing
	// do not expect this to be called.
	return SUCCESS;
}

/*
* To be implemented in the subclasses.
*/
int FieldParam::setType(field_type type, int *status)
{
	// does nothing
	// do not expect this to be called.
	return SUCCESS;
}


int FieldParam::setDescription(char * desc, int *status)
{
	delete[] m_description;
	VOUtils::copyString(m_description, desc, status);
	//m_description = desc;
	return SUCCESS;
}

int FieldParam::setID(char * ID, int *status)
{
	delete[] m_ID;
	VOUtils::copyString(m_ID, ID, status);
	//m_ID = ID;
	return SUCCESS;
}

int FieldParam::setUtype(char * utype, int *status)
{
	delete[] m_utype;
	VOUtils::copyString(m_utype, utype, status);
	//m_utype = utype;
	return SUCCESS;
}

int FieldParam::setUnit(char * unit, int *status)
{
	delete[] m_unit;
	VOUtils::copyString(m_unit, unit, status);
	//m_unit = unit;
	return SUCCESS;
}

int FieldParam::setDatatype(field_datatype datatype, int *status)
{
	m_datatype = datatype;
	return SUCCESS;
}

int FieldParam::setPrecision(char * precision, int *status)
{
	delete[] m_precision;
	VOUtils::copyString(m_precision, precision, status);
	//m_precision = precision;
	return SUCCESS;
}

int FieldParam::setWidth(int width, int *status)
{
	m_width = width;
	return SUCCESS;
}

int FieldParam::setRef(char * ref, int *status)
{
	delete[] m_ref;
	VOUtils::copyString(m_ref, ref, status);
	//m_ref = ref;
	return SUCCESS;
}

int FieldParam::setName(char * name, int *status)
{
	delete[] m_name;
	VOUtils::copyString(m_name, name, status);
	//m_name = name;
	return SUCCESS;
}

int FieldParam::setUCD(char * ucd, int *status)
{
	delete[] m_UCD;
	VOUtils::copyString(m_UCD, ucd, status);
	//m_UCD = ucd;
	return SUCCESS;
}

int FieldParam::setArraySize(char * arraySize, int *status)
{
	delete[] m_arraySize;
	VOUtils::copyString(m_arraySize, arraySize, status);
	//m_arraySize = arraySize;
	return SUCCESS;
}



int FieldParam::setLinks(vector<Link> link, int *status)
{
	m_linkList = link;
	return SUCCESS;
}

int FieldParam::setValues(Values v[], int numOfValues, int *status)
{
	for (int i = 0; i < numOfValues && i < 2; i++)
	{
		m_values.push_back (v[i]);
	}
	return SUCCESS;
}


int FieldParam::replaceValues(Values v, int *status)
{
	m_values.pop_back();
	m_values.push_back(v);
	return SUCCESS;
}

/*
* Get description
*/ 
int FieldParam::getDescription(char * &desc, int * status)
{
	VOUtils::copyString(desc, m_description, status);	
	return *status;
}

/*
* Get 'Values', given an index.
*/
int FieldParam::getValues(Values &v, int index, int *status)
{
	*status = VOERROR;		
	if (index >= 0 && index < m_values.size() )
	{
		// assignment operator overloaded.
		try
		{
			v = m_values[index];
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
* Get number of values.
*/
int FieldParam::getNumOfValues (int &numOfValues, int *status)
{
	numOfValues = m_values.size();
	*status = SUCCESS;	
	return *status;

}

/*
* Get number of links.
*/
int FieldParam::getNumOfLinks(int  &nLinks, int *status)
{
	nLinks = m_linkList.size();
	*status = SUCCESS;
	return *status;
}


/*
* Get the link, given the index.
*/
int FieldParam::getLink(Link &link, int linkNum, int *status)
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


int FieldParam::getID(char * &ID, int *status)
{
	VOUtils::copyString(ID, m_ID, status);	
	return *status;
	
}

int FieldParam::getUtype(char * &utype, int *status)
{
	VOUtils::copyString(utype, m_utype, status);	
	return *status;
	
}

int FieldParam::getUnit(char * &unit, int *status)
{
	VOUtils::copyString(unit, m_unit, status);	
	return *status;
	
}

int FieldParam::getDatatype(field_datatype &datatype, int *status)
{
	datatype = m_datatype;
	*status = SUCCESS;
	return SUCCESS;
}

int FieldParam::getPrecision(char * &precision, int *status)
{
	VOUtils::copyString(precision, m_precision, status);	
	return *status;
}

int FieldParam::getWidth(int &width, int *status)
{
	width = m_width;
	*status = SUCCESS;
	return SUCCESS;
}

int FieldParam::getRef(char * &ref, int *status)
{
	VOUtils::copyString(ref, m_ref, status);
	return *status;
}

int FieldParam::getName(char * &name, int *status)
{
	VOUtils::copyString(name, m_name, status);	
	return *status;
}

int FieldParam::getUCD(char * &ucd, int *status)
{
	VOUtils::copyString(ucd, m_UCD, status);
	return *status;
}

int FieldParam::getArraySize(char * &arraySize, int *status)
{
	VOUtils::copyString(arraySize, m_arraySize, status);
	return *status;
}

int FieldParam::isVariableType(bool &b, int *status)
{
	b = false;
	char c = '*';

	if (m_arraySize != NULL && strchr(m_arraySize, c) != NULL) 
	{
		b = true;
	}

	*status = SUCCESS;
	return SUCCESS;
}
