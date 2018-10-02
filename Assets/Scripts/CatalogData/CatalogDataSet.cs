using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

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

            HashSet<string> numericColumnTypes = new HashSet<string> {"int", "i", "long", "l", "float", "f", "double", "d", "real", "r"};
            string[] metaColumnTypes = {"char", "c", "date"};
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
                        dataSet.ColumnDefinitions[j] = new ColumnInfo {Name = nameEntry.Trim(), Index = j, StartPosition = startPosition};
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

        public static CatalogDataSet LoadCacheFile(string fileName)
        {
            using (var stream = File.Open($"{fileName}.cache", FileMode.Open))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                return (CatalogDataSet) binaryFormatter.Deserialize(stream);
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