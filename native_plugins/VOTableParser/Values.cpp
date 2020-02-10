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
* This class represents the Values element in the field
* of a table.  
*
* A Values element consists of a Minimum, Maximum and 
* a collection of Options.
* 
* Date created - 05 May 2002
*
*/
#include "Values.h"
#include "VOUtils.h"


Values::Values()
{
	init();
	
}

/*Values::Values(char * ID, values_type type, char * nullval, values_invalid_type invalid,
		Range min, Range max, vector<Option> optionlist)
{

}*/

Values::~Values()
{
	cleanup();
}

Values Values::operator=(const Values &v)
{
	if (this != &v)
	{
		cleanup();
		init();
		makecopy(v);
	}
	return *this;

}

Values::Values(const Values &v)
{
	init();
	makecopy(v);
}

void Values::init()
{
	m_ID = NULL;
	m_nullval = NULL;
	m_ref = NULL;
	m_invalid = false;
	m_type = legal;
	m_minimum = NULL;
	m_maximum = NULL;	

}

void Values::makecopy(const Values &v)
{
	m_type = v.m_type;	
	m_invalid = v.m_invalid;	
		
	//try 
	{
		if (NULL != v.m_ID)
		{
			m_ID = new char[strlen(v.m_ID) + 1];
			strcpy(m_ID, v.m_ID);
		}

		if (NULL != v.m_nullval)
		{
			m_nullval = new char[strlen(v.m_nullval) + 1];
			strcpy(m_nullval, v.m_nullval);
		}

		if (NULL != v.m_ref)
		{
			m_ref = new char[strlen(v.m_ref) + 1];
			strcpy(m_ref, v.m_ref);
		}

		if (NULL != v.m_minimum)
		{
			m_minimum = new Range(*(v.m_minimum));			
		}

		if (NULL != v.m_maximum)
		{
			m_maximum = new Range(*(v.m_maximum));			
		}
		
		m_optionList = v.m_optionList;
	} 
//	catch (bad_alloc ex)
//	{
		// Ignore ??
//	}
}

void Values::cleanup(void)
{
	delete[] m_ID;
	delete[] m_nullval;
	delete[] m_ref;
	
	delete m_minimum;
	delete m_maximum;
}

int Values::setID(char * ID, int * status)
{
	VOUtils::copyString(m_ID, ID, status);
	return SUCCESS;
}

int Values::setType(values_type type, int *status)
{
	m_type = type;
	return SUCCESS;
}

int Values::setNull(char * nullval, int *status)
{
	VOUtils::copyString(m_nullval, nullval, status);
	return SUCCESS;
}

int Values::setRef(char * ref, int *status)
{
	VOUtils::copyString(m_ref, ref, status);
	return SUCCESS;
}

int Values::setInvalidFlag(bool invalid , int *status)
{
	m_invalid = invalid;
	return SUCCESS;
}

int Values::setMinimun(Range * min, int *status)
{
	m_minimum = min;
	return SUCCESS;
}

int Values::setMaximum(Range * max, int *status)
{
	m_maximum = max;
	return SUCCESS;
}

int Values::setOptions(vector<Option> options, int *status)
{
	m_optionList = options;
	return SUCCESS;
}


int Values::getID(char * &ID, int * status)
{
	VOUtils::copyString(ID, m_ID, status);
	return *status;
}

int Values::getType(values_type & type, int *status)
{
	type = m_type;

	*status = SUCCESS;
	return *status;
}

int Values::getNull(char * &null, int *status)
{
	VOUtils::copyString(null, m_nullval, status);
	return *status;
}


int Values::getRef(char * &ref, int *status)
{
	VOUtils::copyString(ref, m_ref, status);
	return *status;
}

int Values::isValid(bool &invalid , int *status)
{
	
	invalid = m_invalid;
	*status = SUCCESS;
	return SUCCESS;
}

int Values::getMinimun(Range * &min, int *status)
{
	min = NULL;
	if (NULL == m_minimum)
	{
		*status = SUCCESS;		
		return *status;
	}

		
	try {
		min = new Range(*m_minimum);
	} 
	catch (bad_alloc ex)
	{
		*status = INSUFFICIENT_MEMORY_ERROR;
		return *status;
	}
	
	*status = SUCCESS;

	return *status;

}

int Values::getMaximum(Range * &max, int *status)
{
	max = NULL;
	if (NULL == m_maximum)
	{
		*status = SUCCESS;		
		return *status;
	}

		
	try {
		max = new Range(*m_maximum);
	} 
	catch (bad_alloc ex)
	{
		*status = INSUFFICIENT_MEMORY_ERROR;
		return *status;
	}
	
	*status = SUCCESS;

	return *status;
}

int Values::getOption(Option &option, int index, int *status)
{
	*status = VOERROR;
	if (index >= 0 && index < m_optionList.size() ) 
	{
		try
		{
			option = m_optionList[index];
			*status = SUCCESS;
		} 
		catch (bad_alloc ex)
		{
			*status = INSUFFICIENT_MEMORY_ERROR;
		}
	}
	
	return *status;
}

int Values::getNumOfOptions(int &numOfOptions, int *status)
{
	*status = SUCCESS;
	numOfOptions = m_optionList.size();
	return *status;
}
