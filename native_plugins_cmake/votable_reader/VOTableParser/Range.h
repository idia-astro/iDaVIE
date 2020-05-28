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

#ifndef RANGE_H
#define RANGE_H

#include "global.h"

/**
* This class represents <MIN> and <MAX> element inside <VALUES> element.  
*
* Range consists of value and inclusive flag.
*/
//Date created - 05 May 2002

class Range {

	public:
		/**
		* Default Constructor
		*/
		Range();

		//Range(char * rangevalue, bool inclusive);

		/**
		* Destructor
		*/
		~Range();

		/**
		* Assignment operator overloaded.
		*/
		Range operator=(const Range &o);

		/**
		* Copy Constructor
		*/
		Range(const Range &o);

		int setValue(char * rangeValue, int * status);
		int setPCData(char * pcdata, int * status);
		int setInclusiveFlag(bool inclusive, int * status);

		/**
		* Gets the 'value'
		*/
		int getValue(char * &rangevalue, int * status);

		/**
		* Gets 'false' if inclusive is 'no', true otherwise. 
		*/
		int isInclusive(bool &inclusive, int * status);		

		/**
		* Gets the PCDATA. 
		*/
		int getPCData(char * &pcdata, int * status);
	
	private: 
		char * m_rangeValue;
		bool m_inclusive;
		char * m_pcdata;

		void cleanup();
		void makecopy(const Range &r);
		void init(void);

};

#endif
