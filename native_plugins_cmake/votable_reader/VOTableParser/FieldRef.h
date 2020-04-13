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
#ifndef FIELDREF_H
#define FIELDREF_H

#include "FieldParamRef.h"

/**
* This class represents <FIELDRef> element in <Group>.
*
* A FieldRef contains a ref attr. which refers to a Field.
*
*/
//Date created - 03 Jan 2005

class FieldRef : public FieldParamRef {

	public:

		/**
		* Default constructor
		*/
		FieldRef();

		/**
		* Desctructor
		*/
		~FieldRef();

		/**
		* Assignment operator overloaded.
		*/
		FieldRef operator=(const FieldRef &f);

		/**
		* Copy constructor
		*/
		FieldRef(const FieldRef &f);
		
		
	private: 
				
		void doNothing(void)
		{
			// does nothing.
		}

		void init(void);
		void cleanup(void);
		void makecopy(const FieldRef &f);



};

#endif
