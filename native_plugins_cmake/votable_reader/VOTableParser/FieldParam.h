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
#ifndef FIELDPARAM_H
#define FIELDPARAM_H

#include "global.h"
#include "Values.h"
#include "Link.h"

#include <vector>
using namespace std;

/**
*
* This class is an abstract class, base class of Field and Param. 
*
* This class represents the Field/Param in the MetaData. 
* A Field/Param contains description, a max of 2 values and a
* list of links.
*
* The <FIELD> and <PARAM> class only differ in some attributes.
* <FIELD> contains 'type' and <PARAM> contains 'value'.
*
*/
//Date created - 27 May 2002


class FieldParam {

	public:
		/**
		* Gets the description.
		*/
		int getDescription (char * &desc, int * status);

		/**
		* Gets the number of <VALUES> elements.
		*/
		int getNumOfValues (int &numOfValues, int *status);


		/**
		* Gets 'Values', given the index.
		* Index starts at 0.
		*/
		int getValues (Values &v, int index, int *status);

		/**
		*
		* Gets number of links.
		*/
		int getNumOfLinks (int  &nLinks, int *status);

		/**
		* Gets the 'Link' at the given index.
		* Index starts at 0.
		*/
		int getLink (Link &link, int linkNum, int *status);

		/**
		* Gets the ID.
		*/
		int getID (char * &ID, int *status);

		/**
		* Gets the unit.
		*/
		int getUnit (char * &unit, int *status);

		/**
		* Gets the datatype. 'field_datatype' is an enumeration defined
		* in 'global.h'.
		*/
		int getDatatype (field_datatype &datatype, int *status);

		/**
		* Gets the precision.
		*/
		int getPrecision (char * &precision, int *status);

		/**
		* Gets the width.
		*/
		int getWidth (int &width, int *status);

		/**
		* Gets the ref.
		*/
		int getRef (char * &ref, int *status);

		/**
		* Gets the utype.
		*/
		int getUtype (char * &utype, int *status);

		/**
		* Gets the name.
		*/
		int getName (char * &name, int *status);

		/**
		* Gets the UCD.
		*/
		int getUCD (char * &ucd, int *status);

		/**
		* Gets the arraysize.
		*/
		int getArraySize (char * &arraySize, int *status);
		
		
		/**
		* Checks whether data is of variable type.
		*/
		int isVariableType (bool &b, int *status);

		int setID(char * ID, int *status);
		int setUnit(char * unit, int *status);
		int setDatatype(field_datatype datatype, int *status);
		int setPrecision(char * precision, int *status);
		int setWidth(int width, int *status);
		int setRef(char * ref, int *status);
		int setUtype(char * utype, int *status);
		int setName(char * name, int *status);
		int setUCD(char * ucd, int *status);
		int setArraySize(char * arraySize, int *status);
		virtual int setType(field_type type, int *status);
		virtual int setValue(char *str, int *status);
		
		int setDescription(char * desc, int * status);
		int setLinks(vector<Link> link, int *status);
		int setValues(Values v[], int numOfValues, int *status);

		int replaceValues(Values v, int *status);


	protected: 
		char * m_description;
		vector<Values> m_values;
		vector<Link> m_linkList;

		char * m_ID;
		char * m_unit;
		field_datatype m_datatype;
		char * m_precision;
		unsigned int m_width;
		char * m_ref;
		char * m_name;
		char * m_utype;
		char * m_UCD;
		char * m_arraySize;

		virtual void cleanup(void);
		virtual void makecopy(const FieldParam &f);
		virtual void init(void);

		virtual void doNothing(void)=0;
		


};

#endif
