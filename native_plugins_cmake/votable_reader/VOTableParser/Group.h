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

#ifndef GROUP_H
#define GROUP_H

#include "global.h"
#include "FieldRef.h"
#include "ParamRef.h"
#include "Param.h"


#include <vector>

/**
* This class represents Group in a table
*
* A Group contains Description, FieldRef, ParamRef, Param, and other Group elements.
*
*
* Date created - 03 Jan 2005
*/

class Group {

	public:

		
		/**
		* Default Constructor.
		*/
		Group();    // Constructor:  initialize variables, allocate space.

		/**
		* Destructor
		*/
		~Group();

		/**
		* Assignment Operator Overloaded.
		*/
		Group operator=(const Group &g);

		/**
		* Copy Constructor
		*/
		Group(const Group &g);

		int setDescription(char *desc, int *status);

		// Set  name
		int setName(char * name, int * status);

		// Set  ID.
		int setID(char * ID, int * status);

		// Set  utype.
		int setUtype(char * utype, int * status);

		// Set  Reference
		int setRef(char * ref, int * status);

		// Set  Ucd
		int setUcd(char * ucd, int * status);

		int setParams(vector <Param> infoList, int * status);
		int setFieldRefs(vector <FieldRef> infoList, int * status);
		int setParamRefs(vector <ParamRef> infoList, int * status);
		int setGroups(vector <Group> infoList, int * status);
		
		/**
		* Gets 'Ucd'.
		*/
		int getUcd(char * &ucd, int * status);
		/**
		* Gets 'name'.
		*/
		int getName(char * &name, int * status);

		/**
		* Gets 'ID'.
		*/
		int getID(char * &ID, int * status);

		/**
		* Gets 'utype'.
		*/
		int getUtype(char * &utype, int * status);

		/**
		* Gets 'Ref'.
		*/
		int getRef(char * &ref, int * status);

		/**
		* Gets the description of the Group.
		*/
		int getDescription(char *&desc, int *status);


		/**
		* Get the number of 'Param' elements.
		*/
		int getNumOfParams(int &numOfElements, int * status);

		/**
		* Get the 'Param' element, given the index.
		* Index starts from 0.
		*/
		int getParam(Param &param, int index, int * status);


		/**
		* Get the number of 'FieldRef' elements.
		*/
		int getNumOfFieldRefs(int &numOfElements, int * status);

		/**
		* Get the 'FieldRef' element, given the index.
		* Index starts from 0.
		*/
		int getFieldRef(FieldRef &fieldRef, int index, int * status);

		/**
		* Get the number of 'ParamRef' elements.
		*/
		int getNumOfParamRefs(int &numOfElements, int * status);

		/**
		* Get the 'ParamRef' element, given the index.
		* Index starts from 0.
		*/
		int getParamRef(ParamRef &paramRef, int index, int * status);

		
		/**
		* Get the number of 'Group' elements.
		*/
		int getNumOfGroups(int &numOfElements, int * status);

		/**
		* Get the 'Group' element, given the index.
		* Index starts from 0.
		*/
		int getGroup(Group &group, int index, int * status);
		
		void cleanup(void);

	private:
		char * m_description;

		char * m_ID;
		char * m_name;
		char * m_ref;
		char * m_ucd;
		char * m_utype;

		vector <Param> m_paramList;
		vector <FieldRef> m_fieldRefList;
		vector <ParamRef> m_paramRefList;
		vector <Group> m_groupList;

		
		void makecopy(const Group &g);
		void init(void);

};

#endif
