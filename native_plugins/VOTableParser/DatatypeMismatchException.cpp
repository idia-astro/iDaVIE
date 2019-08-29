#include "DatatypeMismatchException.h"
#include <string.h>

DatatypeMismatchException::DatatypeMismatchException()
{

}


DatatypeMismatchException::~DatatypeMismatchException()
{

}


/*
 * Get the message for this exception
 */
char * DatatypeMismatchException::getMessage()
{
	int len = strlen("Datatype mismatch error!");
	char * str = new char[len + 1];
	strcpy(str, "Datatype mismatch error!");
	return str;
}