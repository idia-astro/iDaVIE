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

#ifndef BINARYDATA_H
#define BINARYDATA_H

#include "global.h"
#include "Stream.h"

/**
* This class represents &lt;BINARY&gt; element.  
*
* A Binary element contains Stream.
*/
//Date created - 22 Dec 2004

class BinaryData {

	public:
		/**
		* Default constructor
		*/
		BinaryData();

		/**
		* Destructor
		*/
		~BinaryData();

		/**
		* Assignment operator overloaded.
		*/
		BinaryData operator=(const BinaryData &bd);

		/**
		* Copy Constructor
		*/
		BinaryData(const BinaryData &bd);
		
		int setStream(Stream streamValue, int * status);

		
		/**
		* Gets the 'Stream' element.
		*/
		int getStream(Stream &stream, int * status);

		void cleanup();
			
	private: 
		Stream m_stream;
		
		
		
		void makecopy(const BinaryData &bd);
		void init(void);

};

#endif
