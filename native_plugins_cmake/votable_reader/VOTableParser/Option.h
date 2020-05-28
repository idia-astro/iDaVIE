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

#ifndef OPTION_H
#define OPTION_H


#include "global.h"
#include <vector>
using namespace std;
/**
* This class represents <OPTION> element.  
*
* An Option element consists of a name and value.
*/
//Date created - 05 May 2002

class Option {

	public:
		/**
		* Default constructor.
		*/
		Option();
		//Option(char * name, char * value);
		
		/**
		* Destructor
		*/
		~Option();

		/**
		* Assignment operator overloaded.
		*/
		Option operator=(const Option &o);

		/**
		* Copy constructor.
		*/
		Option(const Option &o);

		int setName(char * name, int * status);
		int setValue(char * value, int * status);
		int setOptions(vector <Option> optionList, int * status);

		/**
		* Gets the 'name'.
		*/
		int getName(char * &name, int * status);

		/**
		* Gets the 'value'.
		*/
		int getValue(char * &value, int * status);		

		/**
		* Gets the 'Option', given the index.
		* Index starts at 0.
		*/
		int getOption(Option &option, int index, int *status);

		/**
		* Gets the number of options.
		*/
		int getNumOfOptions(int &numOfOptions, int *status);
		
	
	private: 
		char * m_name;
		char * m_value;
		vector <Option> m_optionList;

		void makecopy(const Option &o);
		void cleanup();
		void init(void);

};

#endif
