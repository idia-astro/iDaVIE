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

#include "Option.h"
#include <iostream>
#include <string.h>
#include "VOUtils.h"

/*
* This class represents the Option element.  
*
* An Option element consists of a name and value.
* 
* Date created - 05 May 2002
*
*/

//char * Option::name = NULL;
//char * Option::value = NULL;

Option::Option()
{
	init();

}

Option::Option(const Option &o)
{
	init();
	makecopy(o);
}

Option::~Option()
{
	cleanup();
}

Option Option::operator=(const Option &o)
{
	if (this != &o)
	{
		cleanup();
		init();
		makecopy(o);
	}
	return (*this);

}

void Option::init()
{
	m_name = NULL;
	m_value = NULL;
}

void Option::cleanup()
{
	delete[] m_name;
	delete[] m_value;

	
}

void Option::makecopy(const Option &o)
{
	m_optionList = o.m_optionList;
	//try 
	{
		if (NULL != o.m_name)
		{
			m_name = new char[strlen(o.m_name) + 1];
			strcpy(m_name, o.m_name);
		}
		if (NULL != o.m_value)
		{
			m_value = new char[strlen(o.m_value) + 1];
			strcpy(m_value, o.m_value);
		}
		
	} 
	//catch (bad_alloc ex)
	//{
		// Ignore ??
	//}

}

int Option::setName(char * name, int * status)
{
	delete[] m_name;
	VOUtils::copyString(m_name, name, status);
	//m_name = name;
	return SUCCESS;
}

int Option::setValue(char * value, int * status)
{
	delete[] m_value;
	VOUtils::copyString(m_value, value, status);
	//m_value = value;
	return SUCCESS;
}

int Option::setOptions(vector <Option> optionList, int * status)
{
	m_optionList = optionList;
	return SUCCESS;
}

/*Option::Option(char * name, char * value)
{
	m_name = strcpy(m_name, name);
}*/

int Option::getName(char * &name, int * status)
{
	VOUtils::copyString(name, m_name, status);
	return *status;
}

int Option::getValue(char * &value, int * status)
{
	VOUtils::copyString(value, m_value, status);
	return *status;
}

int Option::getOption(Option &option, int index, int *status)
{
	*status = VOERROR;
	if (index >= 0 && index < m_optionList.size())
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

int Option::getNumOfOptions(int &numOfOptions, int *status)
{
	*status = SUCCESS;
	numOfOptions = m_optionList.size();
	return *status;
}
	
