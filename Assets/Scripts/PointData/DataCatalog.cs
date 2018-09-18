using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace PointData
{
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

        public DataCatalog(string fileName)
        {
            FileName = fileName;
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
                    ColumnDefinitions = new ColumnInfo[numSplits];
                    int startPosition = 0;
                    for (var j = 0; j < numSplits; j++)
                    {
                        string nameEntry = splitLines[j];
                        ColumnDefinitions[j] = new ColumnInfo {Name = nameEntry.Trim(), Index = j, StartPosition = startPosition};
                        startPosition += 1 + nameEntry.Length;

                        // Specify the previous column's length 
                        if (j > 0)
                        {
                            ColumnDefinitions[j - 1].TextLength = ColumnDefinitions[j].StartPosition - ColumnDefinitions[j - 1].StartPosition;
                        }

                        // Update the final column's length
                        if (j == numSplits - 1)
                        {
                            ColumnDefinitions[j].TextLength = startPosition - ColumnDefinitions[j].StartPosition;
                        }
                    }

                    hasNameDefinition = true;
                }
                else if (numSplits == ColumnDefinitions.Length)
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
                                ColumnDefinitions[j].Type = ColumnType.NUMERIC;
                                ColumnDefinitions[j].NumericIndex = dataColumnCounter;
                                dataColumnCounter++;
                            }
                            else
                            {
                                ColumnDefinitions[j].Type = ColumnType.STRING;
                                ColumnDefinitions[j].MetaIndex = metaColumnCounter;
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
                            ColumnDefinitions[j].Unit = unitEntry;
                        }

                        hasUnitDefinition = true;
                    }
                }
                else
                {
                    Debug.Log("Issue reading table");
                    break;
                }
            }

            // Parse the rest of the table
            if (hasNameDefinition && hasTypeDefinition && firstDataLine > 0)
            {
                int numDataColumns = ColumnDefinitions.Count(c => c.Type == ColumnType.NUMERIC);
                int numMetaColumns = ColumnDefinitions.Count(c => c.Type == ColumnType.STRING);
                N = 0;
                int maxDataEntries = lines.Length - firstDataLine + 1;
                MetaColumns = new string[numMetaColumns][];
                for (var i = 0; i < numMetaColumns; i++)
                {
                    MetaColumns[i] = new string[maxDataEntries];
                }

                DataColumns = new float[numDataColumns][];
                for (var i = 0; i < numDataColumns; i++)
                {
                    DataColumns[i] = new float[maxDataEntries];
                }

                Debug.Log("Parsing data");
                for (var i = firstDataLine; i < lines.Length; i++)
                {
                    var line = lines[i];
                    bool canParse = true;
                    foreach (var column in ColumnDefinitions)
                    {
                        if (line.Length < column.StartPosition + column.TextLength)
                        {
                            canParse = false;
                            break;
                        }

                        var subString = line.Substring(column.StartPosition, column.TextLength);
                        if (column.Type == ColumnType.STRING)
                        {
                            MetaColumns[column.MetaIndex][N] = subString.Trim();
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

                            DataColumns[column.NumericIndex][N] = val;
                        }
                    }

                    if (canParse)
                    {
                        N++;
                    }
                }

                // Resize the column arrays to get rid of wasted contents
                for (var i = 0; i < numMetaColumns; i++)
                {
                    Array.Resize(ref MetaColumns[i], N);
                }

                for (var i = 0; i < numDataColumns; i++)
                {
                    Array.Resize(ref DataColumns[i], N);
                }

                Debug.Log($"Parsed and added {N} data points ({numDataColumns} data columns and {numMetaColumns} metadata columns)");
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
        }
    }
}