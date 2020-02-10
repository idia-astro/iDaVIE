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
#include "Info.h"
#include "VOUtils.h"

/*
* This class represents the INFO element in a 'RESOURCE'.  
*
* A INFO element contains ID, name and value.
* 
* Date created - 27 May 2002
*
*/

Info::Info()
{
	init();
}

Info::~Info()
{
	cleanup();
}

Info Info::operator=(const Info &i)
{
	if (this != &i)
	{
		cleanup();
		init();
		makecopy(i);
	}
	return *this;
}

Info::Info(const Info &i)
{
	init();
	makecopy(i);
}

int Info::setValue(char * infoValue, int * status)
{
	delete[] m_infoValue;
	VOUtils::copyString(m_infoValue, infoValue, status);
	//m_infoValue = infoValue;
	return SUCCESS;

}

int Info::setID(char * infoID, int * status)
{
	delete[] m_ID;
	VOUtils::copyString(m_ID, infoID, status);
	//m_ID = infoID;
	return SUCCESS;

}

int Info::setName(char * infoName, int * status)
{
	delete[] m_name;
	VOUtils::copyString(m_name, infoName, status);
	//m_name = infoName;
	return SUCCESS;

}

int Info::setPCData(char * pcdata, int * status)
{
	delete[] m_pcdata;
	VOUtils::copyString(m_pcdata, pcdata, status);
	//m_pcdata = pcdata;
	return SUCCESS;

}

int Info::getValue(char * &infovalue, int * status)
{
	VOUtils::copyString(infovalue, m_infoValue, status);	
	return *status;

}

int Info::getID(char * &infoID, int * status)
{
	VOUtils::copyString(infoID, m_ID, status);	
	return *status;

}

int Info::getName(char * &infoName, int * status)
{
	VOUtils::copyString(infoName, m_name, status);		
	return *status;

}

int Info::getPCData(char * &pcdata, int * status)
{
	VOUtils::copyString(pcdata, m_pcdata, status);	
	return *status;

}



void Info::cleanup()
{
	delete[] m_ID;
	delete[] m_name;
	delete[] m_infoValue;
	delete[] m_pcdata;

}

void Info::makecopy(const Info &i)
{

	//try 
	{
		if (NULL != i.m_ID)
		{
			m_ID = new char[strlen(i.m_ID) + 1];
			strcpy(m_ID, i.m_ID);
		}

		if (NULL != i.m_name)
		{
			m_name = new char[strlen(i.m_name) + 1];
			strcpy(m_name, i.m_name);
		}

		if (NULL != i.m_infoValue)
		{
			m_infoValue = new char[strlen(i.m_infoValue) + 1];
			strcpy(m_infoValue, i.m_infoValue);
		}

		if (NULL != i.m_pcdata)
		{
			m_pcdata = new char[strlen(i.m_pcdata) + 1];
			strcpy(m_pcdata, i.m_pcdata);
		}
	} 
	//catch  (bad_alloc ex)
	//{
		// Ignore ??
	//}

}

void Info::init(void)
{
	m_ID = NULL;
	m_name = NULL;
	m_infoValue = NULL;
	m_pcdata = NULL;
}



