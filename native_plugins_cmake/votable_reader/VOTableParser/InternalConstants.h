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
#ifndef INTERNAL_CONSTANTS
#define INTERNAL_CONSTANTS

/**
* This file contains the constants defined for
* internal use by the C++ Parser.
*
* Globally used constants should be defined in global.h.
*
*/

/* constant strings */

// element names
const char ELE_TABLE[] = "TABLE";
const char ELE_DESC[] = "DESCRIPTION";
const char ELE_FIELD[] = "FIELD";
const char ELE_LINK[] = "LINK";
const char ELE_DATA[] = "DATA";
const char ELE_TABLEDATA[] = "TABLEDATA";
// Added to support VOTable binary format
const char ELE_BINARY[] = "BINARY";
const char ELE_FITS[] = "FITS";
const char ELE_STREAM[] = "STREAM";
const char ELE_TR[] = "TR";
const char ELE_TD[] = "TD";
const char ELE_VALUES[] = "VALUES";
const char ELE_MIN[] = "MIN";
const char ELE_MAX[] = "MAX";
const char ELE_OPTION[] = "OPTION";
const char ELE_RESOURCE[] = "RESOURCE";
const char ELE_INFO[] = "INFO";
const char ELE_COOSYS[] = "COOSYS";
const char ELE_PARAM[] = "PARAM";
const char ELE_PARAM_REF[] = "PARAMref";
const char ELE_FIELD_REF[] = "FIELDref";
const char ELE_GROUP[] = "GROUP";


// Fits attribute names
const char ATTR_EXTNUM[] = "extnum";

// Stream attribute names
const char ATTR_TYPE_STREAM[] = "type";
const char ATTR_HREF_STREAM[] = "href";
const char ATTR_ACTUATE[] = "actuate";
const char ATTR_ENCODING[] = "encoding";
const char ATTR_EXPIRES[] = "expires";
const char ATTR_RIGHTS[] = "rights";


// table attribute names
const char ATTR_ID[] = "ID";
const char ATTR_NAME[] = "name";
const char ATTR_REF[] = "ref";
const char ATTR_UTYPE[] = "utype";

// field attribute names
const char ATTR_UNIT[] = "unit";
const char ATTR_DATATYPE[] = "datatype";
const char ATTR_PRECISION[] = "precision";
const char ATTR_WIDTH[] = "width";
const char ATTR_UCD[] = "ucd";
const char ATTR_ARRAYSIZE[] = "arraysize";
const char ATTR_TYPE[] = "type";

// values attribute names
const char ATTR_NULL[] = "null";
const char ATTR_INVALID[] = "invalid";

// min attribute names
const char ATTR_VALUE[] = "value";
const char ATTR_INCLUSIVE[] = "inclusive";

// link attribute names
const char ATTR_CONTENT_ROLE[] = "content-role";
const char ATTR_CONTENT_TYPE[] = "content-type";
const char ATTR_TITLE[] = "title";
const char ATTR_HREF[] = "href";
const char ATTR_GREF[] = "gref";
const char ATTR_ACTION[] = "action";

// coosys attribute name
const char ATTR_EQUINOX[] = "equinox";
const char ATTR_EPOCH[] = "epoch";
const char ATTR_SYSTEM[] = "system";


const char FIELD_DATATYPE[][16] = {"", "boolean", "bit", "unsignedByte", 
			"short", "int", "long", "char", "unicodeChar", "float", "double",
			"floatComplex", "doubleComplex"};

const int NUM_OF_DATATYPES = 12;

const char FIELD_TYPE[][10] = {"", "hidden", "no_query", "trigger" };

const int NUM_OF_FIELD_TYPES = 4;

const char CONTENT_ROLE[][6] = {"", "query", "hints", "doc" };

const int NUM_OF_CONTENT_ROLES = 4;

const char COOSYS_SYSTEM[][15] = {"", "eq_FK4", "eq_FK5", "ICRS", "ecl_FK5",
		"galactic", "supergalactic", "xy", "barycentric", "geo_app"};
		
const int NUM_OF_COOSYS_SYSTEMS = 10;

const char RESOURCE_META_TYPE[] = "meta";
const char RESOURCE_RESULTS_TYPE[] = "results";

const char VALUES_LEGAL_TYPE[] = "legal";
const char VALUES_ACTUAL_TYPE[] = "actual";

const char YES[] = "yes";
const char NO[] = "no";

const char BOOL_TRUE_TYPE1 = 'T';
const char BOOL_TRUE_TYPE2 = 't';
const char BOOL_TRUE_TYPE3 = '1';
const char BOOL_TRUE_TYPE4[] = "true";

const char BOOL_FALSE_TYPE1 = 'F';
const char BOOL_FALSE_TYPE2 = 'f';
const char BOOL_FALSE_TYPE3 = '0';
const char BOOL_FALSE_TYPE4[] = "false";

#endif
