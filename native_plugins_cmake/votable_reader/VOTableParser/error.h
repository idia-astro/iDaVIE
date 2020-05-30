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

/* error.h  */
#ifndef ERROR_SEEN
#define ERROR_SEEN

/**
* Error status codes
*/
const int SUCCESS = 0;
const int VOERROR = 1;
const int FILE_ERROR = 2;
const int XPATH_RESOURCE_ERROR = 3;
const int XPATH_VTABLE_ERROR = 4;
const int INSUFFICIENT_MEMORY_ERROR = 5;
const int INVALID_DATATYPE_ERROR = 6;
const int PARSING_ERROR = 7;
const int DECODING_ERROR = 8;

/*
* Error messages.
*/
const char ERROR_MESSAGES [] [60] =
{
	"",													// 0
	"Error",											// 1
	"File does not exist.",								// 2
	"XPath expression does not result in <RESOURCE>.",	// 3
	"XPath expression does not result in <TABLE>.",		// 4
	"Not enough memory.",								// 5
	"Invalid datatype.",								// 6
	"Error in parsing VOTable document.",				// 7
	"Error in decoding Binary data."					// 8

};

const char * const getVOTableErrorMessage(int errorCode);

#endif
