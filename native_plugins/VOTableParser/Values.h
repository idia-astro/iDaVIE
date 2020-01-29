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
#ifndef VALUES_H
#define VALUES_H

#include "global.h"
#include "Range.h"
#include "Option.h"

#include <vector>
using namespace std;

/**
* This class represents <VALUES> element in <FIELD>.
*
* A Values element consists of a Minimum, Maximum and 
* a collection of Options.
*
*/
//* Date created - 05 May 2002

class Values {

	public:
		/**
		* Default Constructor
		*/
		Values();
		//Values(char * ID, values_type type, char * nullval, values_invalid_type invalid,
		//	    Range min, Range max, vector<Option> optionlist);

		/**
		* Destructor
		*/
		~Values();

		/**
		* Assignment operator overloaded.
		*/
		Values operator=(const Values &v);

		/**
		* Copy Constructor
		*/
		Values(const Values &v);

		int setID(char * ID, int * status);
		int setType(values_type type, int *status);
		int setNull(char * null, int *status);
		int setRef(char * ref, int *status);
		int setInvalidFlag(bool invalid , int *status);
		int setMinimun(Range * min, int *status);
		int setMaximum(Range * max, int *status);
		int setOptions(vector<Option> option, int *status);	

		/**
		* Gets the 'ID'.
		*/
		int getID(char * &ID, int * status);

		/**
		* Gets the 'type'. 'values_type' is defined in 'gloabal'h'.
		*/
		int getType(values_type & type, int *status);

		/**
		* Gets the 'null'.
		*/
		int getNull(char * &null, int *status);


		/**
		* Gets the 'ref'.
		*/
		int getRef(char * &ref, int *status);

		/**
		* Gets the 'invalid', true if 'invalid' is 'yes'. 
		*/
		int isValid(bool &invalid , int *status);

		/**
		* Gets the <MIN>.
		*/
		int getMinimun(Range * &min, int *status);

		/**
		* Gets the <MAX>.
		*/
		int getMaximum(Range * &max, int *status);

		/**
		* Gets the 'Option', given the index.
		* Index starts at 0.
		*/
		int getOption(Option & option, int index, int *status);			

		/**
		* Gets the number of Options.
		*/
		int getNumOfOptions(int &numOfOptions, int *status);			
	
	private: 
		char * m_ID;
		values_type m_type;
		char * m_nullval;
		char * m_ref;
		bool m_invalid;
		Range *m_minimum;
		Range *m_maximum;
		vector<Option> m_optionList;

		void makecopy(const Values &v);
		void cleanup(void);
		void init(void);

};

#endif
