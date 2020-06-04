#ifndef MISSING_VALUE_EXCEPTION_H
#define MISSING_VALUE_EXCEPTION_H

/**
 *  This class represents the Missing Value Exception.
 *  This Exception is thrown when the data value is missing.
 *
 */


class MissingValueException {
public :
	MissingValueException();

	~MissingValueException();


	/**
	 *	Get the message for MissingValue Exception
	 */
	char * getMessage();
};

#endif