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

#include "FitsData.h"
#include "VOUtils.h"

/*
* This class represents the Fits element in a 'Data'.  
*
* A Fits element contains Stream element.
* 
* Date created - 23 Dec 2004
*
*/




/*
* Default constructor
*/
FitsData::FitsData()
{
	init();
}

FitsData::~FitsData()
{
	cleanup();
}

FitsData FitsData::operator=(const FitsData &fd)
{
	if (this != &fd)
	{
		cleanup();
		init();
		makecopy(fd);
	}
	return *this;
}

FitsData::FitsData(const FitsData &fd)
{
	init();
	makecopy(fd);
}

int FitsData::setExtnum(char * extnum, int * status)
{
	delete[] m_extnum;
	VOUtils::copyString(m_extnum, extnum, status);
	//m_extnum = extnum;
	return SUCCESS;
}

// Set Stream
int FitsData::setStream(Stream st, int * status)
{
	m_stream = st;
	return SUCCESS;
}


/*
* Get the 'extnum' attribute.
*/
int FitsData::getExtnum(char * &extnum, int * status)
{
	VOUtils::copyString(extnum, m_extnum, status);
	return *status;
}

// Get Stream
int FitsData::getStream(Stream &st, int * status)
{
	st = m_stream;
	*status = SUCCESS;
	return SUCCESS;
}


void FitsData::cleanup()
{
	delete[] m_extnum;
}

void FitsData::makecopy(const FitsData &fd)
{
	
	m_stream = fd.m_stream;
	//try 
	{
		if (NULL != fd.m_extnum)
		{
			m_extnum = new char[strlen(fd.m_extnum) + 1];
			strcpy(m_extnum, fd.m_extnum);
		}
		
	} 
	//catch  (bad_alloc ex)
	//{
		// Ignore ??
	//}
	

}

void FitsData::init(void)
{
	m_extnum = NULL;
}

