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
	//char* str = new char[70];
	//vptr->getName(str, status);
	/*
	if (vptr->getName(*name_ptr, status) == SUCCESS && str != NULL)
	{
		name_ptr = &str;
		//freopen("native_plugins/debug.txt", "a", stdout);
		//printf("%s\n", str);
		return 0;
	}
	return 1;
	*/
	return table_ptr->getName(name_ptr, status);
	//name_ptr = new char[70];
	//strcpy(str,"dude");
	//name_ptr = str;
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


int FreeMemory(void* ptrToDelete)
{
	delete(ptrToDelete);
	return 0;
}