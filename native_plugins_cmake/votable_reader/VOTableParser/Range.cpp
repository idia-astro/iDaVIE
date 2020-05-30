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
* This class represents the Range element.  
*
* A Range element represents a minimum or maximum in a Vlaues element.
* 
* Date created - 05 May 2002
*
*/
#include "Range.h"
#include <stdio.h>
#include "VOUtils.h"

Range::Range()
{
	init();
	
}

/*Range::Range(char *rangeval, bool inclusive)
{
	
}*/

void Range::init()
{
	m_rangeValue = NULL;
	m_inclusive = true;
	m_pcdata = NULL;
}

Range::~Range()
{
	cleanup();
}

Range Range::operator=(const Range &r)
{
	if (this != &r)
	{
		cleanup();
		init();
		makecopy(r);
	}
	return (*this);

}

Range::Range(const Range &r)
{
	init();
	makecopy(r);
}

void Range::makecopy (const Range &r)
{
	//try 
	{		
		if (NULL != r.m_rangeValue)
		{
			m_rangeValue = new char[strlen(r.m_rangeValue) + 1];
			strcpy(m_rangeValue, r.m_rangeValue);
		}

		if (NULL != r.m_pcdata)
		{
			m_pcdata = new char[strlen(r.m_pcdata) + 1];
			strcpy(m_pcdata, r.m_pcdata);
		}

		m_inclusive = r.m_inclusive ;
	} 
	//catch  (bad_alloc ex)
	//{
		// Ignore ??
	//}

}

void Range::cleanup(void)
{
	delete [] m_rangeValue;
	delete [] m_pcdata;
}

int Range::setPCData(char * str, int * status)
{
	delete[] m_pcdata;
	VOUtils::copyString(m_pcdata, str, status);
	//m_pcdata = str;
	return SUCCESS;
}

int Range::setValue(char * str, int * status)
{
	delete[] m_rangeValue;
	VOUtils::copyString(m_rangeValue, str, status);
	//m_rangeValue = str;
	return SUCCESS;
}


int Range::setInclusiveFlag(bool inclusive, int * status)
{
	m_inclusive = inclusive;
	return SUCCESS;
}

int Range::getValue(char * &value, int * status)
{
	VOUtils::copyString(value, m_rangeValue, status);
	return *status;
}

int Range::getPCData(char * &pcdata, int * status)
{
	VOUtils::copyString(pcdata, m_pcdata, status);
	return *status;
}

int Range::isInclusive(bool & inclusive, int * status)
{
	inclusive = m_inclusive ;
	*status = SUCCESS;
	return SUCCESS;
}

	

		

