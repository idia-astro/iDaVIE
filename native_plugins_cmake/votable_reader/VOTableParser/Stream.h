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

#ifndef STREAM_H
#define STREAM_H

#include "global.h"

/**
* This class represents <Stream> element.  
*
* A Stream element has type, href, actuate, encoding, expires, rights etc. attributes.
*/
//Date created - 22 Dec 2004

class Stream {

	public:
		/**
		* Default constructor
		*/
		Stream();

		/**
		* Destructor
		*/
		~Stream();

		/**
		* Assignment operator overloaded.
		*/
		Stream operator=(const Stream &s);

		/**
		* Copy Constructor
		*/
		Stream(const Stream &s);
		
		int setType(char * type, int * status);
		int setHref(char * href, int * status);
		int setActuate(char * actuate, int * status);
		int setEncoding(char * encoding, int * status);
		int setExpires(char * expires, int * status);
		int setRights(char * rights, int * status);
		int setData(char * data, int * status);

		/**
		* Gets the 'type' attribute.
		*/
		int getType(char * &type, int * status);

		/**
		* Gets the 'href' attribute.
		*/
		int getHref(char * &href, int * status);

		/**
		* Gets the 'actuate' attribute.
		*/
		int getActuate(char * &actuate, int * status);

		/**
		* Gets the 'encoding' attribute
		*/
		int getEncoding(char * &encoding, int * status);

		/**
		* Gets the 'expires' attribute.
		*/
		int getExpires(char * &expires, int * status);

		/**
		* Gets the 'rights' attribute.
		*/
		int getRights(char * &rights, int * status);

		/**
		* Gets the 'data' in binary format for the BINARY element
		*/
		int getData(char * &data, int * status);
	
	private: 
		char * m_type;
		char * m_href;
		char * m_actuate;
		char * m_encoding;
		char * m_expires;
		char * m_rights;
		char * m_data;
		
		void cleanup();
		void makecopy(const Stream &s);
		void init(void);

};

#endif
