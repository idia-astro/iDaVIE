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
* This class represents the Link in the MetaData.  
* 
* Date created - 05 May 2002
*
*/
#include "Link.h"
#include <stdio.h>
#include "VOUtils.h"


Link::Link()
{
	init();
	
}

void Link::init()
{
	m_ID = NULL;
	m_pcdata = NULL;
	m_contentType = NULL;
	m_contentRole = role_not_specified;
	m_title = NULL;
	m_linkvalue = NULL;
	m_href = NULL;
	m_gref = NULL;
	m_action = NULL;

}

Link::Link(const Link &l)
{
	init();
	makeCopy(l);
}

/*Link::Link(char *ID, char * pcdata, char * contentType, content_role contentRole,
			  char * title, char * linkvalue, char *value, char * href, char * gref, 
			  char *action)
{

}*/

Link::~Link()
{
	cleanup();
}

Link Link::operator=(const Link &l)
{
	if (this != &l)
	{
		cleanup();
		init();
		makeCopy(l);
	}
	return (*this);
}



void Link::cleanup(void)
{
	delete[] m_ID;
	delete[] m_pcdata;
	delete[] m_contentType;
	
	delete[] m_title;
	delete[] m_linkvalue;
	delete[] m_href;
	delete[] m_gref;
	delete[] m_action;
	
}

void Link::makeCopy(const Link &l)
{

	m_contentRole = l.m_contentRole;	
		
	//try 
	{
		if (NULL != l.m_ID)
		{
			m_ID = new char[strlen(l.m_ID) + 1];
			strcpy(m_ID, l.m_ID);
		}
		if (NULL != l.m_pcdata)
		{
			m_pcdata = new char[strlen(l.m_pcdata) + 1];
			strcpy(m_pcdata, l.m_pcdata);
		}
		if (NULL != l.m_contentType)
		{
			m_contentType = new char[strlen(l.m_contentType) + 1];
			strcpy(m_contentType, l.m_contentType);
		}
		if (NULL != l.m_title)
		{
			m_title = new char[strlen(l.m_title) + 1];
			strcpy(m_title, l.m_title);
		}
		if (NULL != l.m_linkvalue)
		{
			m_linkvalue = new char[strlen(l.m_linkvalue) + 1];
			strcpy(m_linkvalue, l.m_linkvalue);
		}
		if (NULL != l.m_href)
		{
			m_href = new char[strlen(l.m_href) + 1];
			strcpy(m_href, l.m_href);
		}
		if (NULL != l.m_gref)
		{
			m_gref = new char[strlen(l.m_gref) + 1];
			strcpy(m_gref, l.m_gref);
		}
		if (NULL != l.m_action)
		{
			m_action = new char[strlen(l.m_action) + 1];
			strcpy(m_action, l.m_action);
		}
	}
	//catch (bad_alloc ex)
	//{
		// Ignore ??
	//}

}

int Link::setID(char * ID, int * status)
{
	delete[] m_ID;
	VOUtils::copyString(m_ID, ID, status);
	//m_ID = ID;
	return SUCCESS;
}

int Link::setPCData(char * pcdata, int *status)
{
	delete[] m_pcdata;
	VOUtils::copyString(m_pcdata, pcdata, status);
	//m_pcdata = pcdata;
	return SUCCESS;
}

int Link::setContentType(char * contentType, int *status)
{
	delete[] m_contentType;
	VOUtils::copyString(m_contentType, contentType, status);
	//m_contentType = contentType;
	return SUCCESS;
}

int Link::setContentRole(content_role contentRole , int *status)
{
	m_contentRole = contentRole;
	return SUCCESS;
}

int Link::setTitle(char * title, int *status)
{
	delete[] m_title;
	VOUtils::copyString(m_title, title, status);
	//m_title = title;
	return SUCCESS;
}

int Link::setValue(char * value, int *status)
{
	delete[] m_linkvalue;
	VOUtils::copyString(m_linkvalue, value, status);
	//m_linkvalue = value;
	return SUCCESS;
}

int Link::setHRef(char * href, int *status)
{
	delete[] m_href;
	VOUtils::copyString(m_href, href, status);
	//m_href = href;
	return SUCCESS;
}

int Link::setGRef(char * gref, int *status)
{
	delete[] m_gref;
	VOUtils::copyString(m_gref, gref, status);
	//m_gref = gref;
	return SUCCESS;
}

int Link::setAction(char * action, int *status)
{
	delete[] m_action;
	VOUtils::copyString(m_action, action, status);
	//m_action = action;
	return SUCCESS;
}


int Link::getID(char * &ID, int * status)
{
	VOUtils::copyString(ID, m_ID, status);	
	return *status;
}

int Link::getPCData(char * &pcdata, int *status)
{
	VOUtils::copyString(pcdata, m_pcdata, status);
	return *status;
	
}

int Link::getContentType(char * &contentType, int *status)
{
	VOUtils::copyString(contentType, m_contentType, status);
	return *status;
	
}

int Link::getContentRole(content_role & contentRole , int *status)
{
	contentRole = m_contentRole;
	*status = SUCCESS;

	return *status;
}

int Link::getTitle(char * &title, int *status)
{
	VOUtils::copyString(title, m_title, status);	
	return *status;
}

int Link::getValue(char * &value, int *status)
{
	VOUtils::copyString(value, m_linkvalue, status);	
	return *status;
}

int Link::getHRef(char * &href, int *status)
{
	VOUtils::copyString(href, m_href, status);
	return *status;
}

int Link::getGRef(char * &gref, int *status)
{
	VOUtils::copyString(gref, m_gref, status);
	return *status;
}

int Link::getAction(char * &action, int *status)
{
	VOUtils::copyString(action, m_action, status);
	return *status;
}
		




