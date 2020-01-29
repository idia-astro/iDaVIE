#include "InvalidDataException.h"
#include <string.h>

InvalidDataException::InvalidDataException()
{

}


InvalidDataException::~InvalidDataException()
{

}


char * InvalidDataException::getMessage()
{
	int len = strlen("Datatype mismatch error!");
	char * str = new char[len + 1];
	strcpy(str, "Datatype mismatch error!");
	return str;
}