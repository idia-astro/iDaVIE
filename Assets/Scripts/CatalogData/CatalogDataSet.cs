using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using System.Text;

namespace CatalogData
{
    [Serializable]
    public class CatalogDataSet
    {

        public ColumnInfo[] ColumnDefinitions { get; private set; }
        public string FileName { get; private set; }
        public string[][] MetaColumns { get; private set; }
        public float[][] DataColumns { get; private set; }
        public int N { get; private set; }

        public int GetDataColumnIndex(string name)
        {
            foreach (var column in ColumnDefinitions)
            {
                if (column.Type == ColumnType.Numeric && column.Name == name)
                {
                    return column.NumericIndex;
                }
            }

            return -1;
        }

        public ColumnInfo GetColumnDefinition(string name)
        {
            foreach (var column in ColumnDefinitions)
            {
                if (column.Name == name)
                {
                    return column;
                }
            }

            return new ColumnInfo();
        }

        public static CatalogDataSet LoadIpacTable(string fileName)
        {
            CatalogDataSet dataSet = new CatalogDataSet();
            dataSet.FileName = fileName;
            string[] lines = File.ReadAllLines(fileName);

            bool hasNameDefinition = false;
            bool hasTypeDefinition = false;
            bool hasUnitDefinition = false;
            int firstDataLine = -1;

            HashSet<string> numericColumnTypes = new HashSet<string> { "int", "i", "long", "l", "float", "f", "double", "d", "real", "r" };
            string[] metaColumnTypes = { "char", "c", "date" };
            // Parse lines for the column info 
            for (var i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                // Skip keyword and comment lines
                if (line.StartsWith("\\"))
                {
                    continue;
                }

                // Split the line up by the pipe separator            
                var splitLines = line.Substring(1, line.Length - 2).Split('|');
                int numSplits = splitLines.Length;
                // If we've reached a data column, stop parsing and move on
                if (numSplits <= 1)
                {
                    firstDataLine = i;
                    break;
                }

                if (!hasNameDefinition)
                {
                    dataSet.ColumnDefinitions = new ColumnInfo[numSplits];
                    int startPosition = 0;
                    for (var j = 0; j < numSplits; j++)
                    {
                        string nameEntry = splitLines[j];
                        dataSet.ColumnDefinitions[j] = new ColumnInfo { Name = nameEntry.Trim(), Index = j, StartPosition = startPosition };
                        startPosition += 1 + nameEntry.Length;

                        // Specify the previous column's length 
                        if (j > 0)
                        {
                            dataSet.ColumnDefinitions[j - 1].TextLength = dataSet.ColumnDefinitions[j].StartPosition - dataSet.ColumnDefinitions[j - 1].StartPosition;
                        }

                        // Update the final column's length
                        if (j == numSplits - 1)
                        {
                            dataSet.ColumnDefinitions[j].TextLength = startPosition - dataSet.ColumnDefinitions[j].StartPosition;
                        }
                    }

                    hasNameDefinition = true;
                }
                else if (numSplits == dataSet.ColumnDefinitions.Length)
                {
                    if (!hasTypeDefinition)
                    {
                        int dataColumnCounter = 0;
                        int metaColumnCounter = 0;
                        for (var j = 0; j < numSplits; j++)
                        {
                            string typeEntry = splitLines[j].Trim().ToLower();
                            if (numericColumnTypes.Contains(typeEntry))
                            {
                                dataSet.ColumnDefinitions[j].Type = ColumnType.Numeric;
                                dataSet.ColumnDefinitions[j].NumericIndex = dataColumnCounter;
                                dataColumnCounter++;
                            }
                            else
                            {
                                dataSet.ColumnDefinitions[j].Type = ColumnType.String;
                                dataSet.ColumnDefinitions[j].MetaIndex = metaColumnCounter;
                                metaColumnCounter++;
                            }
                        }

                        hasTypeDefinition = true;
                    }
                    else if (!hasUnitDefinition)
                    {
                        for (var j = 0; j < numSplits; j++)
                        {
                            string unitEntry = splitLines[j].Trim().Trim('-');
                            dataSet.ColumnDefinitions[j].Unit = unitEntry;
                        }

                        hasUnitDefinition = true;
                    }
                }
                else
                {
                    Debug.Log("Issue reading table");
                    return dataSet;
                }
            }

            // Parse the rest of the table
            if (hasNameDefinition && hasTypeDefinition && firstDataLine > 0)
            {
                int numDataColumns = dataSet.ColumnDefinitions.Count(c => c.Type == ColumnType.Numeric);
                int numMetaColumns = dataSet.ColumnDefinitions.Count(c => c.Type == ColumnType.String);
                dataSet.N = 0;
                int maxDataEntries = lines.Length - firstDataLine + 1;
                dataSet.MetaColumns = new string[numMetaColumns][];
                for (var i = 0; i < numMetaColumns; i++)
                {
                    dataSet.MetaColumns[i] = new string[maxDataEntries];
                }

                dataSet.DataColumns = new float[numDataColumns][];
                for (var i = 0; i < numDataColumns; i++)
                {
                    dataSet.DataColumns[i] = new float[maxDataEntries];
                }

                Debug.Log("Parsing data");
                for (var i = firstDataLine; i < lines.Length; i++)
                {
                    var line = lines[i];
                    bool canParse = true;
                    foreach (var column in dataSet.ColumnDefinitions)
                    {
                        if (line.Length < column.StartPosition + column.TextLength)
                        {
                            canParse = false;
                            break;
                        }

                        var subString = line.Substring(column.StartPosition, column.TextLength);
                        if (column.Type == ColumnType.String)
                        {
                            dataSet.MetaColumns[column.MetaIndex][dataSet.N] = subString.Trim();
                        }
                        else
                        {
                            float val;
                            canParse = float.TryParse(subString, NumberStyles.Any, CultureInfo.InvariantCulture, out val);
                            if (!canParse)
                            {
                                Debug.Log($"Problem parsing {subString} to float");
                                break;
                            }

                            dataSet.DataColumns[column.NumericIndex][dataSet.N] = val;
                        }
                    }

                    if (canParse)
                    {
                        dataSet.N++;
                    }
                }

                // Resize the column arrays to get rid of wasted contents
                for (var i = 0; i < numMetaColumns; i++)
                {
                    Array.Resize(ref dataSet.MetaColumns[i], dataSet.N);
                }

                for (var i = 0; i < numDataColumns; i++)
                {
                    Array.Resize(ref dataSet.DataColumns[i], dataSet.N);
                }

                Debug.Log($"Parsed and added {dataSet.N} data points ({numDataColumns} data columns and {numMetaColumns} metadata columns)");
            }
            else
            {
                if (!hasNameDefinition)
                {
                    Debug.Log("Table is missing column names");
                }

                if (!hasTypeDefinition)
                {
                    Debug.Log("Table is missing column types");
                }

                if (firstDataLine <= 0)
                {
                    Debug.Log("Table is missing data");
                }
            }

            return dataSet;
        }

        public static CatalogDataSet LoadFitsTable(string fileName)
        {
            CatalogDataSet dataSet = new CatalogDataSet();
            dataSet.FileName = fileName;
            IntPtr fptr; // pointer to the FITS file, defined in fitsio.h
            int status, hdunum, hdutype, ncols;
            long nrows;
            if (FitsReader.FitsOpenFile(out fptr, dataSet.FileName, out status) != 0)
            {
                Debug.Log("Fits Failure... cfits code #" + status.ToString());
                return dataSet;
            }
            long frow = 1;
            long felem = 1;
            hdunum = 2;
            int dataColumnCounter = 0;
            int metaColumnCounter = 0;
            // move to the HDU
            if (FitsReader.FitsMovabsHdu(fptr, hdunum, out hdutype, out status) != 0)
            {
                Debug.Log("Fits HDU Read error #" + status.ToString());
                FitsReader.FitsCloseFile(fptr, out status);
                return dataSet;
            }
            // Need to specify which table??
            if (FitsReader.FitsGetNumRows(fptr, out nrows, out status) != 0 || FitsReader.FitsGetNumCols(fptr, out ncols, out status) != 0)
            {
                Debug.Log("Fits Read table size error #" + status.ToString());
                FitsReader.FitsCloseFile(fptr, out status);
                return dataSet;
            }
            dataSet.ColumnDefinitions = new ColumnInfo[ncols];
            StringBuilder keyword = new StringBuilder(75);
            StringBuilder colName = new StringBuilder(71);
            StringBuilder colFormat = new StringBuilder(71);
            StringBuilder colUnit = new StringBuilder(71);
            dataSet.N = (int)nrows;
            dataSet.DataColumns = new float[ncols][];
            for (var i = 0; i < ncols; i++)
            {
                dataSet.DataColumns[i] = new float[nrows];
            }
            for (int col = 0; col < ncols; col++)
            {
                float[] dataFromColumn = new float[nrows];
                IntPtr ptrDataFromColumn;
                FitsReader.FitsMakeKeyN("TTYPE", col + 1, keyword, out status);
                if (FitsReader.FitsReadKey(fptr, 16, keyword.ToString(), colName, IntPtr.Zero, out status) != 0)
                {
                    Debug.Log("Fits Read column name error #" + status.ToString());
                    FitsReader.FitsCloseFile(fptr, out status);
                    return dataSet;
                }
                keyword.Clear();
                FitsReader.FitsMakeKeyN("TFORM", col + 1, keyword, out status);
                if (FitsReader.FitsReadKey(fptr, 16, keyword.ToString(), colFormat, IntPtr.Zero, out status) != 0)
                {
                    Debug.Log("Fits Read column format error #" + status.ToString());
                    FitsReader.FitsCloseFile(fptr, out status);
                    return dataSet;
                }
                string colFormatLetter = colFormat.ToString().Substring(1, 1);
                keyword.Clear();
                FitsReader.FitsMakeKeyN("TUNIT", col + 1, keyword, out status);
                if (FitsReader.FitsReadKey(fptr, 16, keyword.ToString(), colUnit, IntPtr.Zero, out status) != 0)
                {
                    if (status == 202)
                    {
                        Debug.Log("No unit in column #" + col);
                        status = 0;
                    }
                    else
                    {
                        Debug.Log("Fits Read unit error #" + status.ToString());
                        FitsReader.FitsCloseFile(fptr, out status);
                        return dataSet;
                    }
                }
                if (colFormatLetter == "A")
                {
                    dataSet.ColumnDefinitions[col] = new ColumnInfo
                    {
                        Name = colName.ToString(),
                        Index = col,
                        Type = ColumnType.String,
                        MetaIndex = metaColumnCounter,
                        Unit = colUnit.ToString()
                    };
                    metaColumnCounter++;
                }
                else
                {
                    dataSet.ColumnDefinitions[col] = new ColumnInfo
                    {
                        Name = colName.ToString(),
                        Index = col,
                        Type = ColumnType.Numeric,
                        NumericIndex = dataColumnCounter,
                        Unit = colUnit.ToString()
                    };
                    dataColumnCounter++;
                }
                if (FitsReader.FitsReadCol(fptr, 42, col + 1, frow, felem, nrows, out ptrDataFromColumn, out status) != 0)
                {
                    Debug.Log("Fits Read column data error #" + status.ToString());
                    FitsReader.FitsCloseFile(fptr, out status);
                    return dataSet;
                }
                Marshal.Copy(ptrDataFromColumn, dataFromColumn, 0, (int)nrows);
                FitsReader.FreeMemory(ptrDataFromColumn);
                dataSet.DataColumns[col] = dataFromColumn;
            }
            FitsReader.FitsCloseFile(fptr, out status);
            return dataSet;
        }

        public static CatalogDataSet LoadCacheFile(string fileName)
        {
            using (var stream = File.Open($"{fileName}.cache", FileMode.Open))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                return (CatalogDataSet)binaryFormatter.Deserialize(stream);
            }
        }

        public void WriteCacheFile()
        {
            using (var stream = File.Open($"{FileName}.cache", FileMode.Create))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(stream, this);
            }
        }
    }
}
