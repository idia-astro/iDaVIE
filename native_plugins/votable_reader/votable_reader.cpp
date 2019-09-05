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

int VOTableGetMetaData(VTable* vptr, TableMetaData** meta_ptr, int* status)
{
	TableMetaData* meta = new TableMetaData();
	vptr->getMetaData(*meta, status);
	*meta_ptr = meta;
	return 0;
}

int VOTableGetTableData(VTable* vptr, TableData** table_ptr, int* status)
{
	TableData* table = new TableData();
	int result = vptr->getData(*table, status);
	*table_ptr = table;
	return result;
}



int VOTableGetName(VTable* vptr, char*& name_ptr, int* status)
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
	return vptr->getName(name_ptr, status);
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

int RowGetColumn(Row* row_ptr, Column** col_ptr, int colIndex, int* status)
{
	Column* col = new Column();
	int result = row_ptr->getColumn(*col, colIndex, status);
	*col_ptr = col;
	return result;
}

int ColumnGetField(Column* )

int FieldGetName(Field* field_ptr, char*& name_ptr, int* status)
{
	return field_ptr->getName(name_ptr, status);
}


int FreeMemory(void* ptrToDelete)
{
	delete(ptrToDelete);
	return 0;
}