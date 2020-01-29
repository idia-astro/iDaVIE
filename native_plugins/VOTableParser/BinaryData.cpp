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

#include "BinaryData.h"
#include "VOUtils.h"

/*
* This class represents the Binary element in a 'Data'.  
*
* A Binary element contains Stream element.
* 
* Date created - 22 Dec 2004
*
*/




/*
* Default constructor
*/
BinaryData::BinaryData()
{
	init();
}

BinaryData::~BinaryData()
{
	cleanup();
}

BinaryData BinaryData::operator=(const BinaryData &bd)
{
	if (this != &bd)
	{
		cleanup();
		init();
		makecopy(bd);
	}
	return *this;
}

BinaryData::BinaryData(const BinaryData &bd)
{
	init();
	makecopy(bd);
}


// Set Stream
int BinaryData::setStream(Stream st, int * status)
{
	m_stream = st;
	return SUCCESS;
}


// Get Stream
int BinaryData::getStream(Stream &st, int * status)
{
	st = m_stream;
	*status = SUCCESS;
	return SUCCESS;
}


void BinaryData::cleanup()
{
	
}

void BinaryData::makecopy(const BinaryData &bd)
{
	m_stream = bd.m_stream;	
}

void BinaryData::init(void)
{
	
}

