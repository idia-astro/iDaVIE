#include "votable_reader.h"

int VOTableInitialize(VTable** vptr)
{
	*vptr = new VTable();
	return 0;
}

int VOTableOpenFile(VTable* vptr, char* filename, char* xpath, int* status)
{
	vptr->openFile(filename, xpath, 0, status);
	return 0;
}

int FreeMemory(void* ptrToDelete)
{
	delete(ptrToDelete);
	return 0;
}