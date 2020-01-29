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
#ifndef TABLE_METADATA_H
#define TABLE_METADATA_H

#include "global.h"
#include "Field.h"
#include "Group.h"
#include "Link.h"
#include <vector>
using namespace std;

/**
* This class represents the metadata in a Table.  
* 
* TableMetaData consists of the description, 
* Field collection, Link collection, 
*
*/
//Date created - 03 May 2002


class TableMetaData {

	public:
		/**
		* Default constructor.
		*/
		TableMetaData();
		//TableMetaData(Field field[], int numOfFields, Link link[], 
		//			  int numOfLinks, char *desc);

		/**
		* Destructor
		*/
		~TableMetaData();

		/**
		* Assignment operator overloaded.
		*/
		TableMetaData operator=(const TableMetaData &t);

		/**
		* Copy Constructor.
		*/
		TableMetaData(const TableMetaData &t);

		int setDesciption(char *desc, int *status);
		int setFields(vector<Field> f, int *status);
		int setParams(vector<Param> p, int *status);
		int setGroups(vector<Group> g, int *status);
		int setLinks(vector<Link> l, int *status);

		/**
		* Gets total number of Columns i.e. <FIELD> elements
		* in this table.
		*/
		int getNumOfColumns(int  &ncols, int *status);

		/**
		* Gets total number of Links.
		*/
		int getNumOfLinks(int  &nLinks, int *status);

		/**
		* Gets 'Field', given the index.
		* Index starts at 0.
		*/
		int getField(Field &field, int fieldNum, int *status);

		/**
		* Gets total number  <Param> elements
		* in this table.
		*/
		int getNumOfParams(int  &nParams, int *status);

		/**
		* Gets 'Param', given the index.
		* Index starts at 0.
		*/
		int getParam(Param &param, int paramNum, int *status);

		/**
		* Gets 'Link', given the index.
		* Index starts at 0.
		*/
		int getLink(Link &link, int linkNum, int *status);

		/**
		* Gets total number  <Group> elements
		* in this table.
		*/
		int getNumOfGroups(int  &nGroups, int *status);

		/**
		* Gets 'Group', given the index.
		* Index starts at 0.
		*/
		int getGroup(Group &group, int groupNum, int *status);

		/**
		* Gets the description of the Table.
		*/
		int getDescription(char *&desc, int *status);		

		// called internally
		void cleanup();
		void init(void);

	
	private: 
		char * m_description;
		vector<Field> m_fieldList;
		vector<Link> m_linkList;
		vector<Param> m_paramList;
		vector<Group> m_groupList;

		
		void makecopy(const TableMetaData &t);
		
		

};

#endif
