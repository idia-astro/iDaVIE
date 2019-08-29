#include "votable_reader.h"

int VOTableOpenFile(VTable* vptr, char* filename, char* xpath, int* status)
{
	vptr->openFile(filename, xpath, 0, status);
	return 0;
}