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

#ifndef PARAM_H
#define PARAM_H

#include "FieldParam.h"


/**
* This class represents <PARAM> element.  
* 
* A 'Param' contains description, a max of 2 values and a
* list of links. This class is derived from the abstract class
* 'FieldParam'.
*
*/
//Date created - 03 May 2002


class Param : public FieldParam {

	public:
	
		/**
		* Default constructor.
		*/
		Param();

		/**
		* Destructor
		*/
		~Param();

		/**
		* Assignment operator overloaded.
		*/
		Param operator=(const Param &f);

		/**
		* Copy constructor.
		*/
		Param(const Param &f);
		

		int setValue(char * str, int *status);

		/**
		* Gets the 'value'.
		*/
		int getValue(char * &str, int *status);

		
	private: 
		char * m_paramValue;
		void init(void);
		void cleanup (void);
		void makecopy(const Param &p);

		void doNothing(void)
		{
			// does nothing.
		}


};

#endif
