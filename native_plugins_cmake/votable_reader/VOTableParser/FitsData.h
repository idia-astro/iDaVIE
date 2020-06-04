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

#ifndef FITSDATA_H
#define FITSDATA_H

#include "global.h"
#include "Stream.h"

/**
* This class represents <Fits> element.  
*
* A Fits element contains Stream element.
*/
//Date created - 23 Dec 2004

class FitsData {

	public:
		/**
		* Default constructor
		*/
		FitsData();

		/**
		* Destructor
		*/
		~FitsData();

		/**
		* Assignment operator overloaded.
		*/
		FitsData operator=(const FitsData &fd);

		/**
		* Copy Constructor
		*/
		FitsData(const FitsData &fd);
		
		int setExtnum(char * extnumValue, int * status);
		int setStream(Stream streamValue, int * status);

		
		/**
		* Gets the 'Extnum' attribute.
		*/
		int getExtnum(char * &extnum, int * status);
		/**
		* Gets the 'Stream' element.
		*/
		int getStream(Stream &stream, int * status);

		void cleanup();
		
	private: 
		char * m_extnum;
		Stream m_stream;
		
		
		
		void makecopy(const FitsData &fd);
		void init(void);

};

#endif
