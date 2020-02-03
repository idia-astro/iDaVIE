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


#ifndef COLUMN_H
#define COLUMN_H

#include "global.h"

#include <vector>
using namespace std;

/**
* This class represents &lt;TD&gt; element in a Table.  
* 
* Column contains data in the primitive form.
* It may also contain an array. 
*
*
*/
//Date created - 03 May 2002

class Column {

	public:

		/**
		* Default Constructor
		*/
		Column();

		// not used
		//Column(char * data);

		/**
		* Destructor
		*/
		~Column();

		/**
		* Assignment operator overloaded.
		*/
		Column operator=(const Column &c);

		/**
		* Copy Constructor
		*/
		Column(const Column &c);

		int setCharData(char * data, int *status);
		int setUnicodeData (unsigned short *data, int size, int *status);
		int setRef(char * ref, int *status);

		/**
		* Gets data in boolean array.
		*/
		int getLogicalArray(Bool *&b , int &numOfElements, int *status) ;

		/**
		* Gets bit array.
		*/
		int getBitArray(char *&c, int &numOfElements, int *status) ;

		/**
		* Gets byte array.
		*/
		int getByteArray(unsigned char *&c, int &numOfElements, int *status) ;

		/**
		* Gets short array.
		*/
		int getShortArray(short *&array,int &numOfElements, int *status) ;

		/**
		* Gets integer array.
		*/
		int getIntArray(int *&array,int &numOfElements, int *status) ;

		/**
		* Gets long array.
		*/
		int getLongArray(long *&array,int &numOfElements, int *status) ;

		/**
		* Gets character array.
		*/
		int getCharArray(char *&array,int &numOfElements, int *status) ;

		/**
		* Gets Unicode array.
		*/
		int getUnicodeArray(unsigned short *&array, int &numOfElements, int *status) ;
		
		/**
		* Gets float array.
		*/
		int getFloatArray(float *&array, int &numOfElements, int *status) ;

		/**
		* Gets double array.
		*/
		int getDoubleArray(double *&array, int &numOfElements, int *status) ;

		/**
		* Gets float complex array.
		*/
		int getFloatComplexArray(float *&array, int &numOfElements, int *status) ;

		/**
		* Gets double complex array.
		*/
		int getDoubleComplexArray(double *&array,int &numOfElements, int *status) ;

		/**
		* Gets attribute 'ref' of &lt;TD&gt;.
		*/
		int getRef(char * &ref, int * status);

		
	
	
	private: 
		char * m_data;
		char * m_ref;	
		unsigned short * m_unicodeData;
		int m_size; // size of unicode data
		bool m_isUnicode;

		vector <char *> getArrayOfStrings(char *data, int &numberOfElets);
		int getData(char *&c, int &len, int *status);
		void cleanup();
		void makecopy(const Column &c);
		void init(void);
		void trim(char *&str);


};

#endif
