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
* This class represents the row in a Table.  
* 
* Row contains a set of columns. 
*
* Date created - 03 May 2002
*
*/
#include "Row.h"



Row::Row()
{

}

/*Row::Row(vector<Column> colList)
{

}*/

Row::~Row()
{
	cleanup();

}

Row Row::operator=(const Row &r)
{
	if (this != &r)
	{
		cleanup();
		makecopy(r);
	}
	return (*this);

}

Row::Row(const Row &r)
{
	makecopy(r);

}

void Row::makecopy(const Row &r)
{
	m_columnList = r.m_columnList;

}

void Row::cleanup()
{
	if (! m_columnList.empty())
		m_columnList.clear();
}


int Row::setColumns(vector<Column> colList, int *status)
{
	m_columnList = colList;
	return SUCCESS;

}

int Row::getColumn(Column  &column, int index, int *status)
{
	*status = VOERROR;
	if (index >= 0 && index < m_columnList.size())
	{
		try
		{
			column = m_columnList[index];
			*status = SUCCESS;
		} 
		catch (bad_alloc ex)
		{
			*status = INSUFFICIENT_MEMORY_ERROR;
		}
	}
	

	return *status;
}
		
	


