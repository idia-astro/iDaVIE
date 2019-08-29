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
 * /

/*
* This class represents &TD> element in a Table.  
* 
* Column contains data in the primitive form.
* It may also conatin an array. 
*
* Date created - 03 May 2002
*
*/
#include <iostream>

#include "Column.h"
#include "InternalConstants.h"
#include "VOUtils.h"
#include "VTable.h"
#include "MissingValueException.h"
#include "DatatypeMismatchException.h" 


using namespace std;

Column::Column() 
{
	init();	
}

void Column::init()
{
	m_ref = NULL;
	m_data = NULL;
	m_unicodeData = NULL;
	m_size = 0;
	m_isUnicode = false;
}

Column::~Column()
{
	cleanup();
}

Column Column::operator=(const Column &c)
{
	if (this != &c)
	{
		cleanup();
		init();
		makecopy(c);
	}
	return (*this);
}

Column::Column(const Column &c)
{
	init();
	makecopy(c);
}

void Column::cleanup()
{
	delete[] m_ref;
	delete[] m_data;
	delete[] m_unicodeData;
	m_size = 0;

}

void Column::makecopy(const Column &c)
{
	//try 
	{
		m_size = c.m_size;
		m_isUnicode = c.m_isUnicode ;
		if (NULL != c.m_ref)
		{
			m_ref = new char[strlen(c.m_ref) + 1];
			strcpy(m_ref, c.m_ref);
		}

		if (NULL != c.m_data)
		{
			m_data = new char[strlen(c.m_data) + 1];
			strcpy(m_data, c.m_data);
		}

		if (NULL != c.m_unicodeData)
		{
			m_unicodeData = new unsigned short[c.m_size + 1];
			for (int i = 0; i < c.m_size; i++)
			{
				m_unicodeData[i] = c.m_unicodeData[i];
			}
		}
	} 
	//catch (bad_alloc ex)
	//{
		// Ignore ??
	//}
}


int Column::setCharData(char * data, int *status)
{
	delete[] m_data;
	
	VOUtils::copyString(m_data, data, status);
	//m_data = data;
	return SUCCESS;
}

int Column::setUnicodeData(unsigned short * data, int size, int *status)
{
	delete[] m_unicodeData;

	m_unicodeData = data;
	m_size = size;
	m_isUnicode = true;

	return SUCCESS;
}

int Column::setRef(char * ref, int *status)
{
	delete[] m_ref;
	VOUtils::copyString(m_ref, ref, status);
	m_ref = ref;
	return SUCCESS;

}

int Column::getLogicalArray(Bool *&b , int &numOfElements, int *status) 
{
	bool flag;
	int result;
	b = NULL;
	numOfElements = 0;
	if (m_isUnicode)
	{
		*status = INVALID_DATATYPE_ERROR;
		return *status;
	}
	
	if (m_data != NULL)
	{

		if(strcmp(m_data, "missing") == 0 || strcmp(m_data, "NaN") == 0)
		{
			throw MissingValueException();
		}

		char *newdata = NULL;

		try 
		{
			newdata = new char[strlen(m_data) + 1];
		}
		catch (bad_alloc ex)
		{
			*status = INSUFFICIENT_MEMORY_ERROR;
			return *status;

		}
		strcpy(newdata, m_data);
		
		*status = VTable::getBinaryFlag(flag, &result);

		vector <char *> v = getArrayOfStrings(newdata, numOfElements);

		Bool *temp;
		try
		{
			if(flag)
			{
				temp = new Bool[strlen(m_data)];
			}
			else
			{
				temp = new Bool[numOfElements];
			}
		}
		catch (bad_alloc)
		{
			b = NULL;
			*status = INSUFFICIENT_MEMORY_ERROR;
			return *status;
		}
	

		if(flag)
		{
			for (int i = 0; i < strlen(m_data); i++)
			{

				if (BOOL_TRUE_TYPE1 == m_data[i] || 
					BOOL_TRUE_TYPE2 == m_data[i] ||
					BOOL_TRUE_TYPE3 == m_data[i])
				{
					temp[i] = True;
				}
				else if (BOOL_FALSE_TYPE1 == m_data[i] || 
					BOOL_FALSE_TYPE2 == m_data[i] ||
					BOOL_FALSE_TYPE3 == m_data[i])
				{
					temp[i] = False;
				}
				else 
				{
					temp[i] = Null;
				}
			
			}

			numOfElements = strlen(m_data);
		}
		else 
		{
			for (int i = 0; i < v.size(); i++)
			{

				char *str = v[i];		

				if (BOOL_TRUE_TYPE1 == v[i][0] || 
					BOOL_TRUE_TYPE2 == v[i][0] ||
					BOOL_TRUE_TYPE3 == v[i][0] ||
					strcmp(_strlwr(v[i]), BOOL_TRUE_TYPE4) == 0)
				{
					temp[i] = True;
				}
				else if (BOOL_FALSE_TYPE1 == v[i][0] || 
					BOOL_FALSE_TYPE2 == v[i][0] ||
					BOOL_FALSE_TYPE3 == v[i][0] ||
					strcmp(_strlwr(v[i]), BOOL_FALSE_TYPE4) == 0)
				{
					temp[i] = False;
				}
				else 
				{
					temp[i] = Null;
				}		
			}

			numOfElements = v.size();
		}

		b = temp;
		*status = SUCCESS;
	}
	else
	{
		*status = SUCCESS;
	}
	return *status;	
}

int Column::getBitArray(char *&c, int &numOfElements, int *status) 
{
	bool flag;
	int result;
	if (m_isUnicode)
	{
		numOfElements = 0;
		c = NULL;
		*status = INVALID_DATATYPE_ERROR;
		return *status;
	}

	char * newdata = NULL;

	try 
	{
		newdata = new char[strlen(m_data) + 1];
	}
	catch (bad_alloc ex)
	{
		*status = INSUFFICIENT_MEMORY_ERROR;
		return *status;

	}

	strcpy(newdata, m_data);

	vector <char *> v = getArrayOfStrings(newdata, numOfElements);

	*status = VTable::getBinaryFlag(flag, &result);

	if(flag)
	{
		numOfElements = atoi(v[1]);
		VOUtils::stringCopy(c, v[0], (numOfElements + 7)/8, status);
	}

	else 
	{
		// numOfBytes will contain the no of bytes which will hold the bits.
		int numOfBytes;

		/*if(((v.size()) % 8) == 0)
		{
			numOfBytes = ((v.size())/8);
		}
		else 
		{
			numOfBytes = ((v.size()) /8) + 1;
		}
		*/
		numOfBytes = (v.size() + 7)/8;

		c = new char[numOfBytes + 1];

		for(int k=0; k<numOfBytes; k++)
		{
			c[k] = (char)0x00;
		}
		

		unsigned char setmask = (unsigned char) 0x80;
		const unsigned char resetmask = (unsigned char) 0x00;

		for (int i = 0; i < numOfBytes; i++)
		{
			setmask = (char) 0x80;
			for (int j = 0; j < 8; j++)
			{
				// This condition is imp. to retrieve only upto no of bits.
				if((i*8 + j) >= numOfElements)
				{
					break;
				}
				char * str = v[i*8 + j];
				if (BOOL_TRUE_TYPE3 == str[0])
				{
					c[i] = c[i] | setmask;
					setmask = (setmask >> 1);
				}
				else if (BOOL_FALSE_TYPE3 == str[0])
				{
					c[i] = c[i] | resetmask;
					setmask = (setmask >> 1);

				}		
			}
		}
		c[numOfBytes] = 0;
		
		delete[] newdata;
	}

	
	*status = SUCCESS;
	return *status;

	//return getData(c, numOfElements, status);

}
int Column::getByteArray(unsigned char *&array, int &numOfElements, int *status) 
{

	bool flag;
	int result;

	array = NULL;
	numOfElements = 0;

	if (m_isUnicode)
	{
		*status = INVALID_DATATYPE_ERROR;
		return *status;
	}

	if (NULL == m_data)
	{
		*status = SUCCESS;
		return *status;

	}
	char *newdata = NULL;


	try 
	{
		newdata = new char[strlen(m_data) + 1];
	}
	catch (bad_alloc ex)
	{
		*status = INSUFFICIENT_MEMORY_ERROR;
		return *status;

	}
	strcpy(newdata, m_data);

	unsigned char *temp;

	vector <char *> v = getArrayOfStrings(newdata, numOfElements);
	try
	{
		temp = new unsigned char[numOfElements];
	}
	catch (bad_alloc ex)
	{
		delete[] newdata;
		*status = INSUFFICIENT_MEMORY_ERROR;
		return *status;
	}

	*status = VTable::getBinaryFlag(flag, &result);

	for (int i = 0; i < v.size(); i++)
	{
		char * str = v[i];
		temp[i] = 0;
		if(flag)
		{
			if(strcmp(str, "missing") == 0)
			{
				throw MissingValueException();
			}
			temp[i] =  (unsigned char)atoi(str);
		}
		else 
		{
			if(strcmp(str, "NaN") == 0 || strcmp(str, "nan") == 0)
			{
				throw MissingValueException();
			}
			temp[i] =  (unsigned char)atoi(str);
		}
		
		if(temp[i] == 0)
		{
			for(int cnt = 0; cnt < strlen(str); cnt++)
			{
				if(str[cnt] != '0')
				{
					throw DatatypeMismatchException();
				}
			}
		}

	}
	delete[] newdata;

	array = temp;
	*status = SUCCESS;
	return *status;
}

int Column::getShortArray(short *&array,int &numOfElements, int *status) 
{
	int result = 0;
	bool flag;
	array = NULL;
	numOfElements = 0;

	if (m_isUnicode)
	{
		*status = INVALID_DATATYPE_ERROR;
		return *status;
	}

	if (NULL == m_data)
	{
		*status = SUCCESS;
		return *status;

	}
	char * newdata = NULL;

	try
	{
		newdata = new char[strlen(m_data) + 1];
	}
	catch (bad_alloc ex)
	{
		*status = INSUFFICIENT_MEMORY_ERROR;
		return *status;
	}

	strcpy(newdata, m_data);

	short *temp;

	vector <char *> v = getArrayOfStrings(newdata, numOfElements);

	try
	{
		temp = new short[numOfElements];
	}
	catch (bad_alloc ex)
	{
		delete[] newdata;
		*status = INSUFFICIENT_MEMORY_ERROR;
		return *status;

	}

	*status = VTable::getBinaryFlag(flag, &result);

	for (int i = 0; i < v.size(); i++)
	{
		char * str = v[i];
		temp[i] = 0;

		if(flag) 
		{
			if(strcmp(str, "missing") == 0)
			{
				throw MissingValueException();
			}
			temp[i] = (short) atoi(str);

		}
		else 
		{
			if(strcmp(str, "NaN") == 0 || strcmp(str, "nan") == 0)
			{
				throw MissingValueException();
			}
			temp[i] = (short) atoi(str);
		}
		
		if(temp[i] == 0)
		{
			for(int cnt = 0; cnt < strlen(str); cnt++)
			{
				if(str[cnt] != '0')
				{
					throw DatatypeMismatchException();
				}
			}
		}
	}
	delete[] newdata;

	array = temp;
	*status = SUCCESS;
	return *status;
}


/*int Column::getStringArray(vector <char *>&array, int *status) 
{
	char * newdata = new char[strlen(m_data) + 1];
	strcpy(newdata, m_data);

	vector <char *> v = getArrayOfStrings(newdata, numOfElements);

}*/

int Column::getIntArray(int *&array,int &numOfElements, int *status) 
{
	
	int result = 0;
	bool flag;
	array = NULL;
	numOfElements = 0;

	if (m_isUnicode)
	{
		*status = INVALID_DATATYPE_ERROR;
		return *status;
	}

	if (NULL == m_data)
	{
		*status = SUCCESS;
		return *status;
	}

	char * newdata = NULL;

	try 
	{
		newdata = new char[strlen(m_data) + 1];
	}
	catch (bad_alloc ex)
	{
		*status = INSUFFICIENT_MEMORY_ERROR;
		return *status;

	}
	strcpy(newdata, m_data);

	int *temp;

	vector <char *> v = getArrayOfStrings(newdata, numOfElements);
	try
	{
		temp = new int[numOfElements];
	}
	catch (bad_alloc ex)
	{
		delete[] newdata;
		*status = INSUFFICIENT_MEMORY_ERROR;
		return *status;
	}

	*status = VTable::getBinaryFlag(flag, &result);

	for (int i = 0; i < v.size(); i++)
	{
		char * str = v[i];
		temp[i] = 0;
		if(flag)
		{
			if(strcmp(str, "missing") == 0)
			{
				throw MissingValueException();
			}
			temp[i] =  atoi(str);
		}
		else 
		{
			if(strcmp(str, "NaN") == 0 || strcmp(str, "nan") == 0)
			{
				throw MissingValueException();
			}
			temp[i] =  atoi(str);
		}
		
		if(temp[i] == 0)
		{
			for(int cnt = 0; cnt < strlen(str); cnt++)
			{
				if(str[cnt] != '0')
				{
					throw DatatypeMismatchException();
				}
			}
		}

	}
	delete[] newdata;

	array = temp;
	*status = SUCCESS;
	return *status;
}

int Column::getLongArray(long *&array,int &numOfElements, int *status) 
{
	int result = 0;
	bool flag;
	array = NULL;
	numOfElements = 0;

	if (m_isUnicode)
	{
		*status = INVALID_DATATYPE_ERROR;
		return *status;
	}

	if (NULL == m_data)
	{
		*status = SUCCESS;	
		return *status;

	}
	char * newdata = NULL;

	try
	{
		newdata = new char[strlen(m_data) + 1];
	}
	catch (bad_alloc ex)
	{
		*status = INSUFFICIENT_MEMORY_ERROR;
		return *status;
	}
	strcpy(newdata, m_data);

	long *temp;

	vector <char *> v = getArrayOfStrings(newdata, numOfElements);
	try
	{
		temp = new long[numOfElements];
	}
	catch (bad_alloc ex)
	{
		delete[] newdata;
		*status = INSUFFICIENT_MEMORY_ERROR;
		return *status;
	}

	*status = VTable::getBinaryFlag(flag, &result);

	for (int i = 0; i < v.size(); i++)
	{
		char * str = v[i];
		temp[i] = 0;
		
		if(flag)
		{
			if(strcmp(str, "missing") == 0)
			{
				throw MissingValueException();
			}
			temp[i] =  atol(str);
		}
		else 
		{
			if(strcmp(str, "NaN") == 0 || strcmp(str, "nan") == 0)
			{
				throw MissingValueException();
			}
			temp[i] =  atol(str);
		}

		if(temp[i] == 0)
		{
			for(int cnt = 0; cnt < strlen(str); cnt++)
			{
				if(str[cnt] != '0')
				{
					throw DatatypeMismatchException();
				}
			}
		}

	}
	delete[] newdata;

	array = temp;
	*status = SUCCESS;
	return *status;
}

/*
* Get char array.
*/
int Column::getCharArray(char *&array,int &numOfElements, int *status) 
{
	bool flag;
	int result;
	if (m_isUnicode)
	{
		*status = INVALID_DATATYPE_ERROR;
		return *status;
	}

	if (NULL == m_data)
	{
		*status = SUCCESS;	
		return *status;

	}

	char * newdata = NULL;

	try
	{
		newdata = new char[strlen(m_data) + 1];
	}
	catch (bad_alloc ex)
	{
		*status = INSUFFICIENT_MEMORY_ERROR;
		return *status;
	}
	strcpy(newdata, m_data);

	vector <char *> v = getArrayOfStrings(newdata, numOfElements);

	*status = VTable::getBinaryFlag(flag, &result);

	if (getData(array, numOfElements, status) == SUCCESS && array != NULL)
	{
		//std::cout << "printing data" << array << endl;
		trim(array);
		
		if(flag)
		{
			if(strcmp(array, "missing") == 0)
			{
				throw MissingValueException();
			}
		}
		else
		{
			for(int i=0; i < v.size(); i++)
			{
				char *str = v[i];
				if(strcmp(str, "NaN") == 0 || strcmp(str, "nan") == 0)
				{
					throw MissingValueException();
				}
			}

		}
	
		//cout << "printing array" << array << endl;
		(NULL == array) ? (numOfElements = 0) : (numOfElements = strlen(array));

		//cout << "printing elements : " << numOfElements << endl;
		
	}
	return *status;
}

/*int Column::getStringArray(char *&array,int &numOfElements, int *status) 
{
	if (NULL == data)
	{
		*status = VOERROR;
		array = NULL;
		numOfElements = 0;
		return *status;

	}

	numOfElements = 0;
	char * newdata = new char[strlen(data) + 1];
	strcpy(newdata, data);

	char *temp;

	vector <char *> v = getArrayOfStrings(newdata, numOfElements);
	temp = new float[numOfElements];

	for (int i = 0; i < v.size(); i++)
	{
		char * str = v[i];
		temp[i] = 0.0;
		temp[i] =  (float) atof(str);
	}
	delete[] newdata;

	array = temp;
	*status = SUCCESS;
	return *status;

}*/

/*
* Get Unicode data.
*/
int Column::getUnicodeArray(unsigned short *&array, int &numOfElements, int *status) 
{
	array = NULL;
	numOfElements = 0;

	if (m_isUnicode)
	{
		if (m_unicodeData != NULL && m_size > 0)
		{
			try
			{
				array = new unsigned short[m_size + 1]; 
				numOfElements = m_size;
				for (int i = 0; i < m_size; i++)
				{
					array[i] = m_unicodeData[i];
				}
				*status = SUCCESS;
			}
			catch (bad_alloc ex)
			{
				// could not allocate memory.
				*status = INSUFFICIENT_MEMORY_ERROR;
			}
		}
		else
		{
			// no unicode data.
			*status = SUCCESS;
			
		}
	}
	else
	{
		// data is not of type 'unicode'
		*status = INVALID_DATATYPE_ERROR;
		
	}
	return *status;
}

int Column::getFloatArray(float *&array, int &numOfElements, int *status) 
{

	int result = 0;
	bool flag;
	array = NULL;
	numOfElements = 0;

	if (m_isUnicode)
	{
		*status = INVALID_DATATYPE_ERROR;
		return *status;
	}

	if (NULL == m_data)
	{
		*status = SUCCESS;
		return *status;

	}

	char * newdata = NULL;

	numOfElements = 0;
	try
	{
		newdata = new char[strlen(m_data) + 1];
	}
	catch (bad_alloc ex)
	{
		*status = INSUFFICIENT_MEMORY_ERROR;
		return *status;
	}
	strcpy(newdata, m_data);

	//cout << "calculate float from data " << *((float*)m_data) << endl;

	float *temp;

	vector <char *> v = getArrayOfStrings(newdata, numOfElements);

	try
	{
		temp = new float[numOfElements];
	}
	catch (bad_alloc ex)
	{
		delete[] newdata;
		*status = INSUFFICIENT_MEMORY_ERROR;
		return *status;
	}

	*status = VTable::getBinaryFlag(flag, &result);

	for (int i = 0; i < v.size(); i++)
	{
		char * str = v[i];
		temp[i] = 0.0;
	
		if(flag)
		{
			if(strcmp(str, "missing") == 0)
			{
				throw MissingValueException();
			}
			temp[i] =  (float) atof(str);
		}
		else 
		{
			if(strcmp(str, "NaN") == 0 || strcmp(str, "nan") == 0)
			{
				throw MissingValueException();
			}
			temp[i] =  (float) atof(str);
		}
		
		if(temp[i] == 0)
		{
			for(int cnt = 0; cnt < strlen(str); cnt++)
			{
				if(str[cnt] != '0' && str[cnt] != '.')
				{
					throw DatatypeMismatchException();
				}
			}
		}
	}
	delete[] newdata;

	array = temp;
	*status = SUCCESS;
	return *status;

}

int Column::getDoubleArray(double *&array, int &numOfElements, int *status) 
{

	int result = 0;
	bool flag;

	array = NULL;
	numOfElements = 0;

	if (m_isUnicode)
	{
		*status = INVALID_DATATYPE_ERROR;
		return *status;
	}

	if (NULL == m_data)
	{
		*status = SUCCESS;	
		return *status;

	}
	char * newdata = NULL;

	numOfElements = 0;
	try
	{
		newdata = new char[strlen(m_data) + 1];
	} 
	catch (bad_alloc ex)
	{
		*status = INSUFFICIENT_MEMORY_ERROR;
		return *status;
	}
	strcpy(newdata, m_data);

	double *temp;

	vector <char *> v = getArrayOfStrings(newdata, numOfElements);
	try
	{
		temp = new double[numOfElements];
	}
	catch (bad_alloc ex)
	{
		delete[] newdata;
		*status = INSUFFICIENT_MEMORY_ERROR;
		return *status;
	}

	*status = VTable::getBinaryFlag(flag, &result);

	for (int i = 0; i < v.size(); i++)
	{
		char * str = v[i];
		temp[i] = 0.0;

		if(flag)
		{
			if(strcmp(str, "missing") == 0)
			{
				throw MissingValueException();
			}
			temp[i] =  (double)atof(str);
		}
		else 
		{
			if(strcmp(str, "NaN") == 0 || strcmp(str, "nan") == 0)
			{
				throw MissingValueException();
			}
			temp[i] = (double)atof(str);
		}
		
		if(temp[i] == 0)
		{
			for(int cnt = 0; cnt < strlen(str); cnt++)
			{
				if(str[cnt] != '0' && str[cnt] != '.')
				{
					throw DatatypeMismatchException();
				}
			}
		}
	}
	delete[] newdata;

	array = temp;
	*status = SUCCESS;
	return *status;
}

int Column::getFloatComplexArray(float *&array, int &numOfElements, int *status) 
{
	return getFloatArray(array, numOfElements, status);

}

int Column::getDoubleComplexArray(double *&array,int &numOfElements, int *status) 
{
	return getDoubleArray(array, numOfElements, status);
}
		

vector <char *> Column::getArrayOfStrings(char *newdata, int &numberOfElets)
{
	numberOfElets = 0;
	vector <char *> v;

	if (NULL == newdata)
	{
		//cout <<endl << "yes the data is null" << endl;
		return v;
	}

	char * str;
	char seps[] = " \t\n\v";
	str = strtok( newdata, seps );
 

	// get string sepated by spaces.
	while( str != NULL )
    {
      
      //cout << "Token is " << str << endl;
	  v.push_back (str);
	  numberOfElets++;
      str = strtok( NULL, seps );
    }

	return v;
	

}

int Column::getData(char *&c, int &len, int *status)
{
	c = NULL;
	len = 0;

	if (m_isUnicode)
	{
		*status = INVALID_DATATYPE_ERROR;
		return *status;
	}

	if (NULL == m_data)
	{
		*status = SUCCESS;		
		return *status;

	}

	char *str;

	try 
	{
		str = new char[strlen(m_data) + 1];
		len = strlen(m_data);
	} 
	catch (bad_alloc ex)
	{
		*status = INSUFFICIENT_MEMORY_ERROR;
		return *status;
	}

	strcpy(str, m_data);
	c = str;
	*status = SUCCESS;

	return *status;
}

/*
* Trim out the spaces.
*/
void Column::trim(char * &str)
{
	if (NULL == str || strlen(str) == 0)
	{
		return;
	}

	char *temp;
	temp = str + (strlen(str));
	
	while (temp != str  && ' ' == *temp)
	{
		*temp = '\0';
		temp--;
	}
	if (temp == str)
	{
		*temp = '\0';
	}
	temp = NULL;

	return;	

}

/*
* Gets attribute 'ref' of &lt;TD&gt;.
*/
int Column::getRef(char * &ref, int * status)
{
	VOUtils::copyString(ref, m_ref, status);
	return *status;
}
