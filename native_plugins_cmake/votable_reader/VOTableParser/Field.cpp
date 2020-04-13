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
* This class represents the Field in the MetaData.  
* 
* A field contains description, a max of 2 values and a
* list of links.
*
* Date created - 03 May 2002
*
*/
#include "Field.h"
#include "VOUtils.h"

Field::Field() 
{
	init();	
}

Field::Field(const Field &f)
{
	init();	
	makecopy(f);
}

Field::~Field()
{
	cleanup();
}

Field Field::operator=(const Field &f)
{
	
	if (this != &f)
	{
		cleanup();
		init();
		makecopy(f);
	}
	return (*this);

}

int Field::setType(field_type type, int *status)
{
	m_type = type;
	return SUCCESS;
}

int Field::getType(field_type &type, int *status)
{
	type = m_type;
	*status = SUCCESS;
	return SUCCESS;
}

void Field::init(void)
{
	FieldParam::init();
	m_type = type_not_specified;
}

void Field::cleanup(void)
{
	FieldParam::cleanup();
}

void Field::makecopy(const Field &f)
{
	FieldParam::makecopy (f);
	m_type = f.m_type;

}
