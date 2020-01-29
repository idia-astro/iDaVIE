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
* This class represents the Field in the MetaData.  
* 
* A field contains description, a max of 2 values and a
* list of links.
*
* Date created - 03 May 2002
*
*/
#include "Param.h"
#include "VOUtils.h"

Param::Param(const Param &f)
{
	init();
	makecopy(f);

}

Param Param::operator=(const Param &f)
{
	if (this != &f)
	{
		cleanup();	
		init();
		makecopy(f);
	}
	return (*this);

}

Param::Param()
{
	init();
}

Param::~Param()
{
	cleanup();	
}

int Param::setValue(char * str, int *status)
{
	delete[] m_paramValue;
	VOUtils::copyString(m_paramValue, str, status);
	//m_paramValue = str;
	return SUCCESS;
}

int Param::getValue(char * &paramValue, int *status)
{
	VOUtils::copyString(paramValue, m_paramValue, status);
	return *status;

}

void Param::init(void)
{
	m_paramValue = NULL;
	FieldParam::init ();	
}

void Param::cleanup(void)
{
	delete[] m_paramValue;
	FieldParam::cleanup ();	
}

void Param::makecopy (const Param &f)
{
	FieldParam::makecopy(f);
	
	if (NULL != f.m_paramValue)
	{
		m_paramValue = new char[strlen(f.m_paramValue) + 1];
		strcpy(m_paramValue, f.m_paramValue);

	}

	return;
}



