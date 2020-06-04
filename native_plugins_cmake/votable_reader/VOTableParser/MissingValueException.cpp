#include "MissingValueException.h"
#include <string.h>

MissingValueException::MissingValueException()
{

}


MissingValueException::~MissingValueException()
{

}

char * MissingValueException::getMessage()
{
	int len = strlen("Missing value error!");
	char * str = new char[len + 1];
	strcpy(str, "Missing value error!");
	return str;
}