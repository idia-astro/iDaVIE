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

#ifndef VTABLE_H
#define VTABLE_H

#include "global.h"
#include "TableMetaData.h"
#include "TableData.h"
#include "BinaryData.h"
#include "FitsData.h"




/**
* This class represents &lt;TABLE&gt; (Virtual Table) element in a Resource.
*
* A virtual table is a memory representation of a Table in VOTable.
* A VTable consists of metadata and data.
*
*/
//* Date created - 02 May 2002

class VTable {

	public:
		/**
		* Default Constructor.
		*/
		VTable();    // Constructor:  initialize variables, allocate space.

		//VTable(TableMetaData tmd, TableData td, char *ID, char *name, char *ref);

		/**
		* Destructor
		*/
		~VTable();

		/**
		* Assignment Operator Overloaded.
		*/
		VTable operator=(const VTable &v);

		/**
		* Copy Constructor
		*/
		VTable(const VTable &v);

		/**
		* Constructs a 'VTable' from a &lt;TABLE&gt; element from the given VOTABLE file,
		* the xpath denotes the position of the &lt;TABLE&gt; element to be  opened.
		*
		* Example -
		* To read the first &lt;TABLE&gt; in the first <RESOURCE>
		* use "/RESOURCE[1]/TABLE[1]" in the xpath.
		* To read a &lt;TABLE&gt; with ID as 'ycat' use "/RESOURCE[1]/TABLE[@ID='ycat']"
		* in the xpath.
		*
		* Make sure that the xpath points to a single &lt;TABLE&gt; element,
		* else VOERROR will be returned. Note that xpath is case sensitive.
		*
		* Currently the parameter 'iomode' is ignored since all
		* files are opened in 'readonly' mode.
		*/
		VTable(const char * filename, const char * path, int iomode, int * status);

		/**
		* Constructs a 'VTable' from a &lt;TABLE&gt; element from the given VOTABLE,
		* the xpath denotes the position of the &lt;TABLE&gt; element to be  opened.
		* The VOTable is present in the memory. The buffer is the pointer to the memory location
		* where VOTable is loaded.
		* Example -
		* To read the first &lt;TABLE&gt; in the first <RESOURCE>
		* use "/RESOURCE[1]/TABLE[1]" in the xpath.
		* To read a &lt;TABLE&gt; with ID as 'ycat' use "/RESOURCE[1]/TABLE[@ID='ycat']"
		* in the xpath.
		*
		* Make sure that the xpath points to a single &lt;TABLE&gt; element,
		* else VOERROR will be returned. Note that xpath is case sensitive.
		*
		* The buffer points to a memory location from where to read the VOTable.
		* bufferLength is the length of the file in bytes.
		* systemID is fake system id for the buffer. It will be displayed as the source
		* of the error in error messages.
		*/
		VTable(const char * buffer, int bufferLength, const char * systemID,
			const char * xpath, int *status);


		/**
		* Opens a &lt;TABLE&gt; element from the given VOTABLE file,
		* the xpath denotes the position of the &lt;TABLE&gt; element to be  opened.
		*
		* Example -
		* To read the first &lt;TABLE&gt; in the first <RESOURCE>
		* use "/RESOURCE[1]/TABLE[1]" in the xpath.
		* To read a &lt;TABLE&gt; with ID as 'ycat' use "/RESOURCE[1]/TABLE[@ID='ycat']"
		* in the xpath.
		*
		* Make sure that the xpath points to a single &lt;TABLE&gt; element,
		* else VOERROR will be returned.
		*
		* Currently the parameter 'iomode' is ignored since all
		* files are opened in 'readonly' mode.
		*/
		int openFile(const char * filename,
			const char * path, int iomode, int * status);


		/**
		* Opens a &lt;TABLE&gt; element from the given VOTABLE,
		* the xpath denotes the position of the &lt;TABLE&gt; element to be  opened.
		* The VOTable is present in the memory. The buffer is the pointer to the memory location
		* where VOTable is loaded.
		* Example -
		* To read the first &lt;TABLE&gt; in the first <RESOURCE>
		* use "/RESOURCE[1]/TABLE[1]" in the xpath.
		* To read a &lt;TABLE&gt; with ID as 'ycat' use "/RESOURCE[1]/TABLE[@ID='ycat']"
		* in the xpath.
		*
		* Make sure that the xpath points to a single &lt;TABLE&gt; element,
		* else VOERROR will be returned.
		*
		*
		* The buffer points to a memory location from where to read the VOTable.
		* bufferLength is the length of the file in bytes.
		* systemID is fake system id for the buffer. It will be displayed as the source
		* of the error in error messages.
		*/

		int loadVOTableFromMemory(const char * buffer, int bufferLength,
				const char * systemID, const char * xpath, int * status);

		/**
		* Close the VTable.
		*/
		int closeFile(int * status);

		// Set MetaData
		int setMetaData(TableMetaData tmd, int * status);

		// Set Table Data
		int setData(TableData td, int * status);

		// Set BinaryData
		int setBinaryData(BinaryData bd, int * status);

		// Set FitsData
		int setFitsData(FitsData fd, int * status);

		// Set Table name
		int setName(char * name, int * status);

		// Set Table ID.
		int setID(char * ID, int * status);

		// Set Table utype.
		int setUtype(char * utype, int * status);

		// Set Table Reference
		int setRef(char * ref, int * status);

		static int setBinaryFlag(bool flag, int *status);

		/**
		* Gets the 'TableMetaData'.
		*/
		int getMetaData(TableMetaData &tmd, int * status);

		/**
		* Gets the 'TableData'.
		*/
		int getData(TableData &td, int * status);

		/**
		* Gets the 'BinaryData'.
		*/
		int getBinaryData(BinaryData &bd, int * status);

		/**
		* Gets the 'FitsData'.
		*/
		int getFitsData(FitsData &fd, int * status);


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

		static int getBinaryFlag(bool &flag, int * status);


	private:

		TableMetaData m_tmd;
		TableData m_td;
		BinaryData m_bd;
		FitsData m_fd;

		char * m_ID;
		char * m_name;
		char * m_ref;
		char * m_utype;

		static bool m_isBinary;

		void cleanup(void);
		void makecopy(const VTable &v);
		void init(void);

};

#endif
