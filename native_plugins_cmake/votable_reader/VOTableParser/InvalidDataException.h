#ifndef INVALID_DATA_EXCEPTION_H
#define INVALID_DATA_EXCEPTION_H

class InvalidDataException {
public :
	InvalidDataException();

	~InvalidDataException();

	char * getMessage();
};

#endif