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

/*
* This class represents the FieldParamRef in the Group.  
* 
*
* Date created - 03 Jan 2005
*
*/
#include "FieldParamRef.h"
#include "VOUtils.h"


void FieldParamRef::init()
{
	m_ref = NULL;	
}


void FieldParamRef::makecopy(const FieldParamRef &f)
{
	if (NULL != f.m_ref)
	{
		m_ref = new char[strlen(f.m_ref) + 1];
		strcpy(m_ref, f.m_ref);
	}
}

void FieldParamRef::cleanup(void)
{
	delete[] m_ref;
}


int FieldParamRef::setRef(char * ref, int *status)
{
	delete[] m_ref;
	VOUtils::copyString(m_ref, ref, status);
	//m_ref = ref;
	return SUCCESS;
}


int FieldParamRef::getRef(char * &ref, int *status)
{
	VOUtils::copyString(ref, m_ref, status);
	return *status;
}

