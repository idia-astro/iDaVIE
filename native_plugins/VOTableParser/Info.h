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

#ifndef INFO_H
#define INFO_H

#include "global.h"

/**
* This class represents <INFO> element in a Resource.  
*
* A INFO element contains ID, name and value.
* 
*/
//Date created - 27 May 2002
class Info {

	public:
		/**
		* Default constructor
		*/
		Info();

		/**
		* Destructor
		*/
		~Info();

		/**
		* Assignment operator overloaded.
		*/
		Info operator=(const Info &i);

		/**
		* Copy constructor.
		*/
		Info(const Info &i);
		
		int setValue(char * infoValue, int * status);
		int setID(char * infoID, int * status);
		int setName(char * infoName, int * status);
		int setPCData(char * pcdata, int * status);

		/**
		* Gets the 'value' attribute.
		*/
		int getValue(char * &infovalue, int * status);

		/**
		* Gets the 'ID' attribute.
		*/
		int getID(char * &infoID, int * status);

		/**
		* Gets the 'name' attribute.
		*/
		int getName(char * &infoName, int * status);

		/*
		* Gets the 'PCDATA'.
		*/
		int getPCData(char * &pcdata, int * status);

	
	private: 
		char * m_ID;
		char * m_name;
		char * m_infoValue;
		char * m_pcdata;
		

		void cleanup();
		void makecopy(const Info &r);
		void init(void);

};

#endif
