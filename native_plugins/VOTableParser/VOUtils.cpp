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
#include "VOUtils.h"
#include <iostream>
using namespace std;

/*
* check if a file exists in given path.
*/
int VOUtils::checkIfFileExists(const char * filename)
{
	ifstream file(filename);
	if (!file) {
		//cout << "File " << filename << " does not exist." << endl;
		return VOERROR;
	} 

	file.close();
	
	return SUCCESS;
	
}

/*
* Make a copy of string.
*
* Similar to strdup() but returns proper status and
* uses new instead of malloc to alloate memory.
*
* Used internally
*/
int VOUtils::copyString(char *&dest, const char *src, int *status)
{
	dest = NULL;	
	if (NULL == src)
	{
		*status = SUCCESS;	
		return *status;
	}
	
	char *str;

	try {
		str = new char[strlen(src) + 1];
	} 
	catch (bad_alloc ex)
	{
		*status = INSUFFICIENT_MEMORY_ERROR;
		return *status;
	}

	strcpy(str, src);
	dest = str;
	*status = SUCCESS;

	return *status;

}


int VOUtils::stringCopy(char *&dest, const char *src, int numOfChars, int *status)
{
	int count = 0;

	dest = new char[strlen(src) + 1];

	while(count < numOfChars)
	{
		dest[count] = src[count];
		count++;
	}
	dest[numOfChars] = 0;
	
	return SUCCESS;

}