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
#ifndef FIELD_H
#define FIELD_H

#include "FieldParam.h"

/**
* This class represents <FIELD> element in a &lt;TABLE&gt;.
*
* It is contained in the TableMetaData class.  
* A field contains description, a max of 2 values and a
* list of links.
*
*/
//Date created - 03 May 2002

class Field : public FieldParam {

	public:

		/**
		* Default constructor
		*/
		Field();

		/**
		* Desctructor
		*/
		~Field();

		/**
		* Assignment operator overloaded.
		*/
		Field operator=(const Field &f);

		/**
		* Copy constructor
		*/
		Field(const Field &f);
		
	
		/**
		* Gets the 'type' of the field. 'field_type' is defined in 'global.h'.
		*/
		int getType (field_type &type, int *status);

		int setType(field_type type, int *status);
		

		
	private: 
		
		field_type m_type;
		void doNothing(void)
		{
			// does nothing.
		}

		void init(void);
		void cleanup(void);
		void makecopy(const Field &f);



};

#endif
