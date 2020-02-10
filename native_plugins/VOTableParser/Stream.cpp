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

#include "Stream.h"
#include "VOUtils.h"

/*
* This class represents the Stream element in a 'Fits' or Binary element
*
* A Stream element contains type, href, encoding, actuate, expires, rights etc attributes.
* 
* Date created - 22 Dec 2004
*
*/




/*
* Default constructor
*/
Stream::Stream()
{
	init();
}

Stream::~Stream()
{
	cleanup();
}

Stream Stream::operator=(const Stream &s)
{
	if (this != &s)
	{
		cleanup();
		init();
		makecopy(s);
	}
	return *this;
}

Stream::Stream(const Stream &s)
{
	init();
	makecopy(s);
}

int Stream::setType(char * type, int * status)
{
	delete[] m_type;
	VOUtils::copyString(m_type, type, status);
	//m_type = type;
	return SUCCESS;
}

int Stream::setHref(char * href, int * status)
{
	delete[] m_href;
	VOUtils::copyString(m_href, href, status);
	//m_href = href;
	return SUCCESS;
}

int Stream::setActuate(char * actuate, int * status)
{
	delete[] m_actuate;
	VOUtils::copyString(m_actuate, actuate, status);
	//m_actuate = actuate;
	return SUCCESS;
}

int Stream::setEncoding(char * encoding, int * status)
{
	delete[] m_encoding;
	VOUtils::copyString(m_encoding, encoding, status);
	//m_encoding = encoding;
	return SUCCESS;
}

int Stream::setExpires(char * expires, int * status)
{
	delete[] m_expires;
	VOUtils::copyString(m_expires, expires, status);
	//m_expires = expires;
	return SUCCESS;
}

int Stream::setRights(char * rights, int * status)
{
	delete[] m_rights;
	VOUtils::copyString(m_rights, rights, status);
	//m_rights = rights;
	return SUCCESS;
}

int Stream::setData(char * data, int * status)
{
	delete[] m_data;
	VOUtils::copyString(m_data, data, status);
	//m_data = data;
	return SUCCESS;
}



int Stream::getType(char * &type, int * status)
{
	VOUtils::copyString(type, m_type, status);
	return *status;
}

/*
* Get the 'href' attribute.
*/
int Stream::getHref(char * &href, int * status)
{
	VOUtils::copyString(href, m_href, status);
	return *status;
}

/*
* Get the 'actuate' attribute.
*/
int Stream::getActuate(char * &actuate, int * status)
{
	VOUtils::copyString(actuate, m_actuate, status);
	return *status;
}

/*
* Get the 'encoding' attribute.
*/
int Stream::getEncoding(char * &encoding, int * status)
{
	VOUtils::copyString(encoding, m_encoding, status);
	return *status;
}
/*
* Get the 'expires' attribute. 
*/
int Stream::getExpires(char * &expires, int * status)
{
	VOUtils::copyString(expires, m_expires, status);
	return *status;
}


/*
* Get the 'rights' attribute.
*/
int Stream::getRights(char * &rights, int * status)
{
	VOUtils::copyString(rights, m_rights, status);
	return *status;
}


int Stream::getData(char * &data, int * status)
{
	VOUtils::copyString(data, m_data, status);
	return *status;
}

void Stream::cleanup()
{
	delete[] m_type;
	delete[] m_href;
	delete[] m_actuate;
	delete[] m_encoding;
	delete[] m_expires;
	delete[] m_rights;
	delete[] m_data;
}

void Stream::makecopy(const Stream &s)
{
		
	//try 
	{
		if (NULL != s.m_type)
		{
			m_type = new char[strlen(s.m_type) + 1];
			strcpy(m_type, s.m_type);
		}

		if (NULL != s.m_href)
		{
			m_href = new char[strlen(s.m_href) + 1];
			strcpy(m_href, s.m_href);
		}
		
		if (NULL != s.m_actuate)
		{
			m_actuate = new char[strlen(s.m_actuate) + 1];
			strcpy(m_actuate, s.m_actuate);
		}

		if (NULL != s.m_encoding)
		{
			m_encoding = new char[strlen(s.m_encoding) + 1];
			strcpy(m_encoding, s.m_encoding);
		}

		if (NULL != s.m_expires)
		{
			m_expires = new char[strlen(s.m_expires) + 1];
			strcpy(m_expires, s.m_expires);
		}

		if (NULL != s.m_rights)
		{
			m_rights = new char[strlen(s.m_rights) + 1];
			strcpy(m_rights, s.m_rights);
		}

		if (NULL != s.m_data)
		{
			m_data = new char[strlen(s.m_data) + 1];
			strcpy(m_data, s.m_data);
		}
	} 
	//catch  (bad_alloc ex)
	//{
		// Ignore ??
	//}
	

}

void Stream::init(void)
{
	m_type = NULL;
	m_href = NULL;
	m_actuate = NULL;
	m_encoding = NULL;
	m_expires = NULL;
	m_rights = NULL;
	m_data = NULL;

}

