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

#ifndef LINK_H
#define LINK_H

#include "global.h"

/**
* This class represents <LINK> element. 
*
* A <LINK> consists of ID, content-role, content-type, title, value,
* href, gref and action.
*/
//Date created - 05 May 2002

class Link {

	public:
		/**
		* Default constructor.
		*/
		Link();

		//Link(char *ID, char * pcdata, char * contentType, content_role contentRole,
		//	  char * title, char * linkvalue, char *value, char * href, char * gref, 
		//	  char *action);

		/**
		* Destructor.
		*/
		~Link();

		/**
		* Assignment operator overloaded.
		*/
		Link operator=(const Link &f);

		/**
		* Copy constructor.
		*/
		Link(const Link &l);

		int setID(char * ID, int * status);
		int setPCData(char * pcdata, int *status);
		int setContentType(char * contentType, int *status);
		int setContentRole(content_role contentRole , int *status);
		int setTitle(char * title, int *status);
		int setValue(char * value, int *status);
		int setHRef(char * href, int *status);
		int setGRef(char * gref, int *status);
		int setAction(char * action, int *status);	

		/**
		* Gets the 'ID'.
		*/
		int getID(char * &ID, int * status);

		/**
		* Gets the 'PCData'.
		*/
		int getPCData(char * &pcdata, int *status);

		/**
		* Gets the 'content-type'.
		*/
		int getContentType(char * &contentType, int *status);

		/**
		* Gets the 'content-role'.'content-role' is defined in 'global.h'.
		*/
		int getContentRole(content_role & contentRole , int *status);

		/**
		* Gets the 'title'.
		*/
		int getTitle(char * &title, int *status);

		/**
		* Gets the 'value'.
		*/
		int getValue(char * &value, int *status);

		/**
		* Gets the 'href'.
		*/
		int getHRef(char * &href, int *status);

		/**
		* Gets the 'gref'.
		*/
		int getGRef(char * &gref, int *status);

		/**
		* Gets the 'action'.
		*/
		int getAction(char * &action, int *status);		
	
	private: 
		char * m_ID;
		char * m_pcdata;
		char * m_contentType;
		content_role m_contentRole;
		char * m_title;
		char * m_linkvalue;
		char * m_href;
		char * m_gref;
		char * m_action;

		void makeCopy(const Link &l);
		void cleanup(void);
		void init(void);


};

#endif
