using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace PointData
{
    [Serializable]
    public class DataCatalog
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
                if (column.Type == ColumnType.NUMERIC && column.Name == name)
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

        public static DataCatalog LoadIpacTable(string fileName)
        {
            DataCatalog catalog = new DataCatalog();
            catalog.FileName = fileName;
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
                    catalog.ColumnDefinitions = new ColumnInfo[numSplits];
                    int startPosition = 0;
                    for (var j = 0; j < numSplits; j++)
                    {
                        string nameEntry = splitLines[j];
                        catalog.ColumnDefinitions[j] = new ColumnInfo {Name = nameEntry.Trim(), Index = j, StartPosition = startPosition};
                        startPosition += 1 + nameEntry.Length;

                        // Specify the previous column's length 
                        if (j > 0)
                        {
                            catalog.ColumnDefinitions[j - 1].TextLength = catalog.ColumnDefinitions[j].StartPosition - catalog.ColumnDefinitions[j - 1].StartPosition;
                        }

                        // Update the final column's length
                        if (j == numSplits - 1)
                        {
                            catalog.ColumnDefinitions[j].TextLength = startPosition - catalog.ColumnDefinitions[j].StartPosition;
                        }
                    }

                    hasNameDefinition = true;
                }
                else if (numSplits == catalog.ColumnDefinitions.Length)
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
                                catalog.ColumnDefinitions[j].Type = ColumnType.NUMERIC;
                                catalog.ColumnDefinitions[j].NumericIndex = dataColumnCounter;
                                dataColumnCounter++;
                            }
                            else
                            {
                                catalog.ColumnDefinitions[j].Type = ColumnType.STRING;
                                catalog.ColumnDefinitions[j].MetaIndex = metaColumnCounter;
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
                            catalog.ColumnDefinitions[j].Unit = unitEntry;
                        }

                        hasUnitDefinition = true;
                    }
                }
                else
                {
                    Debug.Log("Issue reading table");
                    return catalog;
                }
            }

            // Parse the rest of the table
            if (hasNameDefinition && hasTypeDefinition && firstDataLine > 0)
            {
                int numDataColumns = catalog.ColumnDefinitions.Count(c => c.Type == ColumnType.NUMERIC);
                int numMetaColumns = catalog.ColumnDefinitions.Count(c => c.Type == ColumnType.STRING);
                catalog.N = 0;
                int maxDataEntries = lines.Length - firstDataLine + 1;
                catalog.MetaColumns = new string[numMetaColumns][];
                for (var i = 0; i < numMetaColumns; i++)
                {
                    catalog.MetaColumns[i] = new string[maxDataEntries];
                }

                catalog.DataColumns = new float[numDataColumns][];
                for (var i = 0; i < numDataColumns; i++)
                {
                    catalog.DataColumns[i] = new float[maxDataEntries];
                }

                Debug.Log("Parsing data");
                for (var i = firstDataLine; i < lines.Length; i++)
                {
                    var line = lines[i];
                    bool canParse = true;
                    foreach (var column in catalog.ColumnDefinitions)
                    {
                        if (line.Length < column.StartPosition + column.TextLength)
                        {
                            canParse = false;
                            break;
                        }

                        var subString = line.Substring(column.StartPosition, column.TextLength);
                        if (column.Type == ColumnType.STRING)
                        {
                            catalog.MetaColumns[column.MetaIndex][catalog.N] = subString.Trim();
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

                            catalog.DataColumns[column.NumericIndex][catalog.N] = val;
                        }
                    }

                    if (canParse)
                    {
                        catalog.N++;
                    }
                }

                // Resize the column arrays to get rid of wasted contents
                for (var i = 0; i < numMetaColumns; i++)
                {
                    Array.Resize(ref catalog.MetaColumns[i], catalog.N);
                }

                for (var i = 0; i < numDataColumns; i++)
                {
                    Array.Resize(ref catalog.DataColumns[i], catalog.N);
                }

                Debug.Log($"Parsed and added {catalog.N} data points ({numDataColumns} data columns and {numMetaColumns} metadata columns)");
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

            return catalog;
        }

        public static DataCatalog LoadCacheFile(string fileName)
        {
            using (var stream = File.Open($"{fileName}.cache", FileMode.Open))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                return (DataCatalog) binaryFormatter.Deserialize(stream);
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