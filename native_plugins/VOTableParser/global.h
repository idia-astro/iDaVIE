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
/* global.h  */
#ifndef FILE_GLOBAL_SEEN
#define FILE_GLOBAL_SEEN

//#include "SimpleFileLogger.h"
#include <string.h>
#include "error.h"

#include <new>
using namespace std;


/**
*
* global.h
*
* This class containd global definations.
*
*
*/
// Date created - 03 May 2002

/**
* Various datatypes allowed for a Field
*/
enum field_datatype { datatype_not_specified = 0,
				 BooleanType = 1,
				 BitType,
				 UnsignedByteType,
				 ShortType,
				 IntType,
				 LongType,
				 CharType,
				 UnicodeCharType,
				 FloatType,
				 DoubleType, 
				 FloatComplexType,
				 DoubleComplexType
};

/**
* The types allowed for a field
*/
enum field_type {type_not_specified = 0, 
				 hidden =1,
				 no_query,
				 trigger
};

/**
* 'content-role' types in a <LINK>
*/
enum content_role {role_not_specified = 0, 
				   query = 1,
                   hints,
				   doc
};

/**
* <RESOURCE> types
*/
enum resource_type { results = 1,
					meta
};

/**
* Type in a <VALUES>.
*/
enum values_type { legal = 1,
					actual
};


//enum values_invalid_type { no = 0,
//						   yes
//};


/**
* 'system' types in a <COOSYS>.
*/
enum coosys_system {
	eq_FK4 = 1,
	eq_FK5,
	ICRS,
	ecl_FK4,
	ecl_FK5,
	galactic,
	supergalactic,
	xy,
	barycentric,
	geo_app
};

/**
* 'bool' with Null.
*/
enum Bool {
	True = true,
	False = false,
	Null = 3 // Not NULL since it's value is same as false.
};


#endif /* !FILE_GLOBAL_SEEN */
