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
* This class represents the data in a Table.  
* 
* TableData consists of list of rows and columns. 
*
* Date created - 03 May 2002
*
*/
#include "TableData.h"

TableData::TableData()
{
}

/*TableData::TableData(Row rows[], int numOfRows)
{
	
}*/

TableData::~TableData()
{
	//if (! rowList.empty ())
	//	rowList.clear();
}

int TableData::setRows(vector<Row> rowList, int *status)
{
	
	m_rowList = rowList;
	return SUCCESS;
}

int TableData::getNumOfRows(int  &nrows, int *status)
{
	nrows = m_rowList.size ();
	return SUCCESS;
}

int TableData::getRow(Row &row, int index, int *status)
{
	*status = VOERROR;
	if (index >=0 && index < m_rowList.size())
	{
		try
		{
			row = m_rowList[index];
			*status = SUCCESS;
		} 
		catch (bad_alloc ex)
		{
			*status = INSUFFICIENT_MEMORY_ERROR;
		}
	}
	
	return *status;
}

void TableData::cleanup()
{
	if (! m_rowList.empty())
	{
		m_rowList.clear();
	}
}


