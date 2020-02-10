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
#ifndef ROW_H
#define ROW_H

#include "global.h"
#include "Column.h"
#include <vector>
using namespace std;

/**
* This class represents the &lt;TR&gt; in a Table.  
* 
* Row contains a list of 'Column' objects. 
*
*/
// Date created - 03 May 2002

class Row {

	public:	
		/**
		* Default Constructor.
		*/
		Row();

		//Row(vector<Column> columnList);

		/**
		* Destructor
		*/
		~Row();

		/**
		* Assignment operator overloaded.
		*/
		Row operator=(const Row &r);

		/**
		* Copy Constructor
		*/
		Row(const Row &r);

		int setColumns(vector<Column> colList, int *status);

		/**
		* Gets the 'Column', given the column index. 
		* Index starts from 0.
		*/
		int getColumn(Column &column, int index, int *status);
	
	private: 
		vector<Column> m_columnList;

		void makecopy(const Row &r);
		void cleanup(void);
		


};

#endif
