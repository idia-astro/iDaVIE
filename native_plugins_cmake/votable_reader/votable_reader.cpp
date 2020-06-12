#include "votable_reader.h"

int VOTableInitialize(VTable** table_ptr)
{
	*table_ptr = new VTable();
	return 0;
}

int VOTableOpenFile(VTable* table_ptr, char* filename, char* xpath, int* status)
{
	table_ptr->openFile(filename, xpath, 0, status);
	return 0;
}

int VOTableGetMetaData(VTable* table_ptr, TableMetaData** meta_ptr, int* status)
{
	TableMetaData* meta = new TableMetaData();
	table_ptr->getMetaData(*meta, status);
	*meta_ptr = meta;
	return 0;
}

int VOTableGetTableData(VTable* table_ptr, TableData** data_ptr, int* status)
{
	TableData* table = new TableData();
	int result = table_ptr->getData(*table, status);
	*data_ptr = table;
	return result;
}

int VOTableGetName(VTable* table_ptr, char*& name_ptr, int* status)
{
	name_ptr = new char[STRING_SIZE];
	return table_ptr->getName(name_ptr, status);
	return 0;
}

int MetaDataGetNumCols(TableMetaData* meta_ptr, int* ncols, int* status)
{
	int ncols_value;
	meta_ptr->getNumOfColumns(ncols_value, status);
	*ncols = ncols_value;
	return 0;
}

int MetaDataGetField(TableMetaData* meta_ptr, Field** field_ptr, int fieldNum, int* status)
{
	Field* field = new Field();
	meta_ptr->getField(*field, fieldNum, status);
	*field_ptr = field;
	return 0;
}

int TableDataGetRow(TableData* data_ptr, Row** row_ptr, int rowIndex, int* status)
{
	Row* row = new Row();
	int result = data_ptr->getRow(*row, rowIndex, status);
	*row_ptr = row;
	return result;
}


int TableDataGetNumRows(TableData* data_ptr, int* nrows, int* status)
{
	int nrows_value;
	data_ptr->getNumOfRows(nrows_value, status);
	*nrows = nrows_value;
	return 0;
}

int RowGetColumn(Row* row_ptr, Column** col_ptr, int colIndex, int* status)
{
	Column* col = new Column();
	int result = row_ptr->getColumn(*col, colIndex, status);
	*col_ptr = col;
	return result;
}

int ColumnGetFloatArray(Column* col_ptr, float** float_ptr, int* numElements, int* status)
{
	float* float_array;
	int num_elements;
	int result = col_ptr->getFloatArray(float_array, num_elements, status);
	*float_ptr = float_array;
	numElements = &num_elements;
	return result;
}

int FieldGetName(Field* field_ptr, char*& name_ptr, int* status)
{
	return field_ptr->getName(name_ptr, status);
}

int FieldGetDataType(Field* field_ptr, int* datatype_ptr, int* status)
{
	field_datatype datatype = datatype_not_specified;
	int result = field_ptr->getDatatype(datatype, status);
	*datatype_ptr = datatype;
	return result;
}

int ColumnGetFloatArray(Column* col_ptr, float*& float_array, int* numELements, int* status)
{
	int number_elements;
	int result = col_ptr->getFloatArray(float_array, number_elements, status);
	*numELements = number_elements;
	return result;
}

int ColumnGetIntArray(Column* col_ptr, int*& int_array, int* numELements, int* status)
{
	int number_elements;
	int result = col_ptr->getIntArray(int_array, number_elements, status);
	*numELements = number_elements;
	return result;
}

int ColumnGetCharArray(Column* col_ptr, char*& char_array, int* numELements, int* status)
{
	int number_elements;
	int result = col_ptr->getCharArray(char_array, number_elements, status);
	*numELements = number_elements;
	return result;
}

int FreeMemory(void* ptrToDelete)
{
	delete[] ptrToDelete;
	return 0;
}