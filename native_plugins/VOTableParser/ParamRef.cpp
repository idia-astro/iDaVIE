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
* This class represents the ParamRef in the Group.  
* 
* A ParamRef contains a ref attr. which describes the param to which it refers.
*
*
Date created - 03 Jan 2005
*
*/
#include "ParamRef.h"
#include "VOUtils.h"

ParamRef::ParamRef() 
{
	init();	
}

ParamRef::ParamRef(const ParamRef &f)
{
	init();	
	makecopy(f);
}

ParamRef::~ParamRef()
{
	cleanup();
}

ParamRef ParamRef::operator=(const ParamRef &f)
{
	
	if (this != &f)
	{
		cleanup();
		init();
		makecopy(f);
	}
	return (*this);

}


void ParamRef::init(void)
{
	FieldParamRef::init();
}

void ParamRef::cleanup(void)
{
	FieldParamRef::cleanup();
}

void ParamRef::makecopy(const ParamRef &f)
{
	FieldParamRef::makecopy (f);
}
