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
#ifndef RESOURCE_H
#define RESOURCE_H

#include "global.h"
#include "Info.h"
#include "Coosys.h"
#include "Param.h"
#include "Link.h"
#include "VTable.h"


#include <vector>
using namespace std;

/**
* This class represents <RESOURCE> element in the VOTable.
*
* A RESOURCE element contains description, list of 'Info' elements,
* list of 'Coosys' elements, list of 'Param' elements, list of
* 'Link' elements, list of 'Table' elements, and 'list' of Resource
* elements.
*
*/
//Date created - 27 May 2002

class Resource {

	public:
		/**
		* Default constructor
		*/
		Resource();

		/**
		* Destructor
		*/
		~Resource();

		/**
		* Assignment operator overloaded.
		*/
		Resource operator=(const Resource &r);

		/**
		* Copy constructor.
		*/
		Resource(const Resource &r);

		/**
		* Constructs a 'Resource' from <RESOURCE> element from the given VOTABLE file,
		* the xpath denotes the position of the resource element to be opened.
		*
		* Example -
		* To read the first <RESOURCE> use "/RESOURCE[1]" in the xpath.
		* To read a <RESOURCE> with ID as 'ycat' use "/RESOURCE[@ID='ycat']"
		* in the xpath.
		*
		* Make sure that the xpath points to a single <RESOURCE> element,
		* else VOERROR will be returned.
		*
		* Currently the parameter 'iomode' is ignored since all
		* files are opened in 'readonly' mode.
		*/
		Resource(const char * filename,
			const char * xpath, int iomode, int * status);

		/**
		* Constructs a 'Resource' from <RESOURCE> element from the given VOTABLE,
		* the xpath denotes the position of the resource element to be opened. The VOTable
		* is present in the memory. The buffer is the pointer to the memory location
		* where VOTable is loaded.
		*
		* Example -
		* To read the first <RESOURCE> use "/RESOURCE[1]" in the xpath.
		* To read a <RESOURCE> with ID as 'ycat' use "/RESOURCE[@ID='ycat']"
		* in the xpath.
		*
		* Make sure that the xpath points to a single <RESOURCE> element,
		* else VOERROR will be returned.
		*
		* The buffer points to a memory location from where to read the VOTable.
		* bufferLength is the length of the file in bytes.
		* systemID is fake system id for the buffer. It will be displayed as the source
		* of the error in error messages.
		*/

		Resource(const char * buffer, int bufferLength,
				const char * systemID, const char * xpath, int * status);

		int setDesc(char * infoValue, int * status);
		int setID(char * id, int *status);
		int setUtype(char * utype, int *status);
		int setName(char * str, int *status);
		int setType(resource_type t, int *status);

		int setInfos(vector <Info> infoList, int * status);
		int setCoosystems(vector <Coosys> infoList, int * status);
		int setParams(vector <Param> infoList, int * status);
		int setLinks(vector <Link> infoList, int * status);
		int setTables(vector <VTable> infoList, int * status);
		int setResources(vector <Resource> infoList, int * status);

		/**
		* Opens a Resource element from the given VOTABLE file,
		* the xpath denotes the position of the resource element to be
		* opened.
		*
		* Example -
		* To read the first <RESOURCE> use "/RESOURCE[1]" in the xpath.
		* To read a <RESOURCE> with ID as 'ycat' use "/RESOURCE[@ID='ycat']"
		* in the xpath. Note that xpath is case sensitive.
		*
		* Make sure that the xpath points to a single <RESOURCE> element,
		* else VOERROR will be returned.
		*
		* Currently the parameter 'iomode' is ignored since all
		* files are opened in 'readonly' mode.
		*/
		int openFile(const char * filename,
			const char * xpath, int iomode, int * status);

		/**
		* Opens a Resource element from the given VOTABLE,
		* the xpath denotes the position of the resource element to be
		* opened.The VOTable is present in the memory.
		* The buffer is the pointer to the memory location where VOTable is loaded.
		*
		* Example -
		* To read the first <RESOURCE> use "/RESOURCE[1]" in the xpath.
		* To read a <RESOURCE> with ID as 'ycat' use "/RESOURCE[@ID='ycat']"
		* in the xpath. Note that xpath is case sensitive.
		*
		* Make sure that the xpath points to a single <RESOURCE> element,
		* else VOERROR will be returned.
		*
		* The buffer points to a memory location from where to read the VOTable.
		* bufferLength is the length of the file in bytes.
		* systemID is fake system id for the buffer. It will be displayed as the source
		* of the error in error messages.
		*/

		int loadVOTableFromMemory(const char * buffer, int bufferLength,
				const char * systemID, const char * xpath, int * status);
		/**
		* Closes the 'Resource' element.
		*/
		int closeFile(int *status);

		/**
		* Get the 'description' of resource.
		*/
		int getDescription(char * &desc, int * status);

		/**
		* Get the 'ID'.
		*/
		int getID(char * &ID, int * status);

		/**
		* Get the 'utype'.
		*/
		int getUtype(char * &utype, int * status);

		/**
		* Get the 'name'.
		*/
		int getName(char * &name, int * status);

		/**
		* Get the 'type'.
		*/
		int getType(resource_type &value, int * status);

		/**
		* Get the number of 'Info' elements.
		*/
		int getNumOfInfos(int &numOfElements, int * status);

		/**
		* Get the 'Info' element, given the index.
		* Index starts from 0.
		*/
		int getInfo(Info &info, int index, int * status);

		/**
		* Get the number of 'Coosys' elements.
		*/
		int getNumOfCoosys(int &numOfElements, int * status);

		/**
		* Get the 'Coosys' element, given the index.
		* Index starts from 0.
		*/
		int getCoosys(Coosys &coosys, int index, int * status);

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
		* Get the number of 'Link' elements.
		*/
		int getNumOfLinks(int &numOfElements, int * status);

		/**
		* Get the 'Link' element, given the index.
		* Index starts from 0.
		*/
		int getLink(Link &link, int index, int * status);

		/**
		* Get the number of 'VTable' elements.
		*/
		int getNumOfTables(int &numOfElements, int * status);

		/**
		* Get the 'VTable' element, given the index.
		* Index starts from 0.
		*/
		int getTable(VTable &table, int index, int * status);

		/**
		* Get the number of 'Resource' elements.
		*/
		int getNumOfResources(int &numOfElements, int * status);

		/**
		* Get the 'Resource' element, given the index.
		* Index starts from 0.
		*/
		int getResource(Resource &resource, int index, int * status);


	private:
		// SONALI - change variable names to m_
		char * m_desc;
		char * m_ID;
		char * m_name;
		char * m_utype;
		resource_type m_type;

		vector <Info> m_infoList;
		vector <Coosys> m_coosysList;
		vector <Param> m_paramList;
		vector <Link> m_linkList;
		vector <VTable> m_vtableList;
		vector <Resource> m_resourceList;

		void cleanup();
		void makecopy(const Resource &r);
		void init(void);

};

#endif
