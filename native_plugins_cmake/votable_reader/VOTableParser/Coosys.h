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

#ifndef COOSYS_H
#define COOSYS_H

#include "global.h"

/**
* This class represents <COOSYS> element.  
*
* A COOSYS element contains ID, equinox, epoch and system.
*/
//Date created - 27 May 2002

class Coosys {

	public:
		/**
		* Default constructor
		*/
		Coosys();

		/**
		* Destructor
		*/
		~Coosys();

		/**
		* Assignment operator overloaded.
		*/
		Coosys operator=(const Coosys &c);

		/**
		* Copy Constructor
		*/
		Coosys(const Coosys &c);
		
		int setEquinox(char * infoValue, int * status);
		int setID(char * infoID, int * status);
		int setEpoch(char * epoch, int * status);
		int setPCData(char * pcdata, int * status);
		int setSystem(coosys_system system, int * status);

		/**
		* Gets the 'equinox' attribute.
		*/
		int getEquinox(char * &equinox, int * status);

		/**
		* Gets the 'ID' attribute.
		*/
		int getID(char * &infoID, int * status);

		/**
		* Gets the 'epoch' attribute.
		*/
		int getEpoch(char * &epoch, int * status);

		/**
		* Gets the PCDATA.
		*/
		int getPCData(char * &pcdata, int * status);

		/**
		* Gets the 'system' attribute.
		*/
		int getSystem(coosys_system &system, int * status);
	
	private: 
		char * m_ID;
		char * m_equinox;
		char * m_epoch;
		coosys_system m_system;
		char * m_pcdata;
		
		void cleanup();
		void makecopy(const Coosys &r);
		void init(void);

};

#endif
