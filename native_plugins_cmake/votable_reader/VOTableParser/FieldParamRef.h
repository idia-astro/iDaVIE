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
#ifndef FIELDPARAMREF_H
#define FIELDPARAMREF_H

#include "global.h"

using namespace std;

/**
*
* This class is an abstract class, base class of FieldRef and ParamRef. 
*
* This class represents the FieldRef/ParamRef in the Group. 
* A FieldRef/ParamRef contains description, a Values element and a
* list of links.
*
*
*/
//Date created - 3 Jan 2005


class FieldParamRef {

	public:
		
		/**
		* Gets the ref.
		*/
		int getRef (char * &ref, int *status);
				
		int setRef(char * ref, int *status);
		


	protected: 
		char * m_ref;

		virtual void cleanup(void);
		virtual void makecopy(const FieldParamRef &f);
		virtual void init(void);

		virtual void doNothing(void)=0;
		


};

#endif
