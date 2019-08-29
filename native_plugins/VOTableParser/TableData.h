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
#ifndef TABLEDATA_H
#define TABLEDATA_H

#include "global.h"
#include "Row.h"
#include <vector>
using namespace std;

/**
* This class represents the data in a Table.  
* 
* TableData consists of list of 'Row' objects. 
*
*/
//Date created - 03 May 2002
class TableData {

	public:
		/**
		* Default Constructor
		*/
		TableData();

		/**
		* Destructor
		*/
		~TableData();

		//TableData(Row rows[], int numOfRows);
		int setRows(vector<Row> rowList, int *status);

		/**
		* Gets total number of rows.
		*/
		int getNumOfRows(int  &nrows, int *status);

		/**
		* Gets the 'Row', given the index.
		* Index starts at 0.
		*/
		int getRow(Row &row, int index, int *status);		

		// called internally
		void cleanup();
	
	private: 
		vector<Row> m_rowList;


};

#endif
