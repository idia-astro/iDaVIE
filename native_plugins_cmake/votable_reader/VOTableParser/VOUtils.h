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
#ifndef VO_UTILS_H
#define VO_UTILS_H

#include "global.h"

#include <fstream>
using namespace std;

/*
* This class contains utlility functions used by VOTable API.
* This is an internal class used by the parser.
*/
// Date created - 28 May 2002

class VOUtils
{
	public:
		/*
		* check if a file exists in given path.
		*/
		int static checkIfFileExists(const char * filename);

		/*
		* Make a copy of string.
		*
		* Similar to strdup() but returns proper status and
		* uses new instead of malloc to alloate memory.
		*
		* Used internally
		*/
		int static copyString(char *&dest, const char *src, int *status);

		/*
		 * Copies the String upto the n characters.
		 *
		 */
		int static stringCopy(char *&dest, const char *src, int noOfChars, int *status);
	
};

#endif

