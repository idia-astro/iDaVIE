using System;

namespace CatalogData
{
    public enum ColumnType
    {
        Numeric,
        String
    }

    [Serializable]
    public struct ColumnInfo
    {
        public string Name;
        public ColumnType Type;
        public string Unit;
        public int Index;
        public int TextLength;
        public int StartPosition;
        public int MetaIndex;
        public int NumericIndex;
    }
}