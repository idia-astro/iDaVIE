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

#include "Coosys.h"
#include "VOUtils.h"

/*
* This class represents the COOSYS element in a 'RESOURCE'.  
*
* A COOSYS element contains ID, equinox, epoch and system.
* 
* Date created - 27 May 2002
*
*/




/*
* Default constructor
*/
Coosys::Coosys()
{
	init();
}

Coosys::~Coosys()
{
	cleanup();
}

Coosys Coosys::operator=(const Coosys &c)
{
	if (this != &c)
	{
		cleanup();
		init();
		makecopy(c);
	}
	return *this;
}

Coosys::Coosys(const Coosys &c)
{
	init();
	makecopy(c);
}

int Coosys::setEquinox(char * equinox, int * status)
{
	delete[] m_equinox;
	VOUtils::copyString(m_equinox, equinox, status);
	//m_equinox = equinox;
	return SUCCESS;
}

int Coosys::setID(char * ID, int * status)
{
	delete[] m_ID;
	VOUtils::copyString(m_ID, ID, status);
	//m_ID = ID;
	return SUCCESS;
}

int Coosys::setEpoch(char * epoch, int * status)
{
	delete[] m_epoch;
	VOUtils::copyString(m_epoch, epoch, status);
	//m_epoch = epoch;
	return SUCCESS;
}

int Coosys::setPCData(char * pcdata, int * status)
{
	delete[] m_pcdata;
	VOUtils::copyString(m_pcdata, pcdata, status);
	//m_pcdata = pcdata;
	return SUCCESS;
}

int Coosys::setSystem(coosys_system system, int * status)
{
	m_system = system;
	return SUCCESS;
}

/*
* Get the 'equinox' attribute.
*/
int Coosys::getEquinox(char * &equinox, int * status)
{
	VOUtils::copyString(equinox, m_equinox, status);
	return *status;
}

/*
* Get the 'ID' attribute.
*/
int Coosys::getID(char * &ID, int * status)
{
	VOUtils::copyString(ID, m_ID, status);
	return *status;
}

/*
* Get the 'epoch' attribute.
*/
int Coosys::getEpoch(char * &epoch, int * status)
{
	VOUtils::copyString(epoch, m_epoch, status);
	return *status;
}
/*
* Get the PCDATA.
*/
int Coosys::getPCData(char * &pcdata, int * status)
{
	VOUtils::copyString(pcdata, m_pcdata, status);
	return *status;
}


/*
* Get the 'system' attribute.
*/
int Coosys::getSystem(coosys_system &system, int * status)
{
	system = m_system ;
	*status = SUCCESS;
	return *status;
}

void Coosys::cleanup()
{
	delete[] m_ID;
	delete[] m_equinox;
	delete[] m_epoch;
	delete[] m_pcdata;
}

void Coosys::makecopy(const Coosys &c)
{
		
	m_system = c.m_system;
	//try 
	{
		if (NULL != c.m_ID)
		{
			m_ID = new char[strlen(c.m_ID) + 1];
			strcpy(m_ID, c.m_ID);
		}

		if (NULL != c.m_equinox)
		{
			m_equinox = new char[strlen(c.m_equinox) + 1];
			strcpy(m_equinox, c.m_equinox);
		}

		if (NULL != c.m_epoch)
		{
			m_epoch = new char[strlen(c.m_epoch) + 1];
			strcpy(m_epoch, c.m_epoch);
		}

		if (NULL != c.m_pcdata)
		{
			m_pcdata = new char[strlen(c.m_pcdata) + 1];
			strcpy(m_pcdata, c.m_pcdata);
		}
	} 
	//catch  (bad_alloc ex)
	//{
		// Ignore ??
	//}
	

}

void Coosys::init(void)
{
	m_ID = NULL;
	m_equinox = NULL;
	m_epoch = NULL;
	m_system = eq_FK5;
	m_pcdata = NULL;

}

