#ifndef DATA_MISMATCH_EXCEPTION_H
#define DATA_MISMATCH_EXCEPTION_H

/**
 *  This class represents the Datatype Mismatch Exception.
 *  This exception is thrown when the data value doesn't match
 *  with its datatype mentioned in the Field element.
 */
class DatatypeMismatchException {
public :
	DatatypeMismatchException();

	~DatatypeMismatchException();

	/**
	 *	Get the message for DatatypeMismatch Exception
	 */
	char * getMessage();
};

#endif