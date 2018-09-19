using System;

namespace PointData
{
    public enum ColumnType
    {
        NUMERIC,
        STRING
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