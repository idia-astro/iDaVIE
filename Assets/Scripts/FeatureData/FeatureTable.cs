/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 *
 * This file is part of the iDaVIE project.
 *
 * iDaVIE is free software: you can redistribute it and/or modify it under the terms 
 * of the GNU Lesser General Public License (LGPL) as published by the Free Software 
 * Foundation, either version 3 of the License, or (at your option) any later version.
 *
 * iDaVIE is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR 
 * PURPOSE. See the GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License along with 
 * iDaVIE in the LICENSE file. If not, see <https://www.gnu.org/licenses/>.
 *
 * Additional information and disclaimers regarding liability and third-party 
 * components can be found in the DISCLAIMER and NOTICE files included with this project.
 *
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using VoTableReader;

namespace DataFeatures
{
 /// <summary>
 /// This class acts as a general container for data from a VOTable or FITS file (or potentially others).
 /// </summary>
    public class FeatureTable
    {
        public List<FeatureColumn> Column = new List<FeatureColumn>();
        public Dictionary<string, FeatureColumn> Columns = new Dictionary<string, FeatureColumn>();
        public List<FeatureRow> Rows = new List<FeatureRow>();

        /// <summary>
        /// Function to create a FeatureTable object from the given file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>FeatureTable containing data of file</returns>
        public static FeatureTable GetFeatureTableFromFile(string fileName)
        {
            if (Path.GetExtension(fileName) == ".xml")
            {
                VoTable voTable = VoTable.GetVOTableFromFile(fileName);
                return GetFeatureTableFromVoTable(voTable);
            }
            else if (Path.GetExtension(fileName) == ".fits" || Path.GetExtension(fileName) == ".fit")
            {
                int status = 0;
                FitsReader.FitsOpenFile(out IntPtr fitsPtr, fileName, out status, true);
                var featureTable = GetFeatureTableFromFits(fitsPtr);
                FitsReader.FitsCloseFile(fitsPtr, out status);
                return featureTable;
            }
            else
            {
                Debug.LogError("Invalid file format for source import! Only compatible with VOTable or FITS.");
                return null;
            }
        }

        /// <summary>
        /// Function to create a FeatureTable object from the given VoTable object.
        /// </summary>
        /// <param name="voTable"></param>
        /// <returns></returns>
        private static FeatureTable GetFeatureTableFromVoTable(VoTable voTable)
        {
            FeatureTable featureTable = new FeatureTable();
            foreach (var column in voTable.Columns)
            {
                FeatureColumn featureColumn =
                    new FeatureColumn(column.Value.Name, column.Value.Index, column.Value.Unit);
                featureTable.Columns.Add(column.Value.Name, featureColumn);
                featureTable.Column.Add(featureColumn);
            }

            foreach (var row in voTable.Rows)
            {
                FeatureRow featureRow = new FeatureRow(featureTable);
                featureRow.ColumnData = new object[voTable.Columns.Count];
                for (int i = 0; i < voTable.Columns.Count; i++)
                {
                    featureRow.ColumnData[i] = row[i];
                }

                featureTable.Rows.Add(featureRow);
            }

            return featureTable;
        }

        /// <summary>
        /// Function to create a FeatureTable object from the given FITS file.
        /// </summary>
        /// <param name="fitsPtr"></param>
        /// <returns></returns>
        private static FeatureTable GetFeatureTableFromFits(IntPtr fitsPtr)
        {
            FeatureTable featureTable = new FeatureTable();
            int status = 0;
            int hduType = 0;
            int numHdus = 0;
            FitsReader.FitsGetNumHdus(fitsPtr, out numHdus, out status);
            FitsReader.FitsGetHduType(fitsPtr, out hduType, out status);

            for (int i = 1; i <= numHdus; i++)
            {
                FitsReader.FitsMovabsHdu(fitsPtr, i, out hduType, out status);
                if (hduType == (int)FitsReader.HduType.BinaryTbl || hduType == (int)FitsReader.HduType.AsciiTbl)
                {
                    break;
                }
            }

            if (hduType != (int)FitsReader.HduType.BinaryTbl && hduType != (int)FitsReader.HduType.AsciiTbl)
            {
                Debug.LogError("No binary or ASCII table found in FITS file.");
                return null;
            }

            if (FitsReader.FitsGetNumCols(fitsPtr, out int numCols, out status) != 0)
            {
                Debug.LogError("Error getting number of columns from FITS file: " + FitsReader.ErrorCodes[status]);
                return null;
            }

            for (int i = 0; i < numCols; i++)
            {
                var colName = FitsReader.FitsTableGetColName(fitsPtr, i);
                var colUnit = FitsReader.FitsTableGetColUnit(fitsPtr, i);
                FeatureColumn featureColumn = new FeatureColumn(colName, i, colUnit);
                featureTable.Columns.Add(colName, featureColumn);
                featureTable.Column.Add(featureColumn);
            }

            FitsReader.FitsGetNumRows(fitsPtr, out long numRows, out status);
            for (int i = 0; i < numRows; i++)
            {
                FeatureRow featureRow = new FeatureRow(featureTable);
                featureRow.ColumnData = new object[numCols];
                IntPtr ptrDataFromColumn = IntPtr.Zero;

                for (int j = 0; j < numCols; j++)
                {
                    var colFormat = FitsReader.FitsTableGetColFormat(fitsPtr, j);
                    if (colFormat.Contains("A"))
                    {
                        FitsReader.FitsReadColString(fitsPtr, j + 1, i + 1, 1, 1, out ptrDataFromColumn, out _,
                            out status);
                        IntPtr[] ptrArray = new IntPtr[1];
                        Marshal.Copy(ptrDataFromColumn, ptrArray, 0, 1);
                        featureRow.ColumnData[j] = Marshal.PtrToStringAnsi(ptrArray[0]);
                    }
                    else
                    {
                        FitsReader.FitsReadColFloat(fitsPtr, j + 1, i + 1, 1, 1, out ptrDataFromColumn, out status);
                        float[] floatArray = new float[1];
                        Marshal.Copy(ptrDataFromColumn, floatArray, 0, 1);
                        //Not a good way of doing this, but makes it compatible with current VOTable functionality.
                        //TODO: Separate the numeric and string data types.
                        featureRow.ColumnData[j] = floatArray[0].ToString();
                    }
                }

                featureTable.Rows.Add(featureRow);
            }

            return featureTable;
        }

    }

 /// <summary>
 /// A class to represent a row in a FeatureTable object.
 /// </summary>
    public class FeatureRow
    {
        public FeatureTable Owner;
        public object[] ColumnData;

        public FeatureRow(FeatureTable owner)
        {
            Owner = owner;
        }

        public object this[int index]
        {
            get
            {
                if (index < 0 || index >= ColumnData.GetLength(0))
                {
                    return null;
                }

                return ColumnData[index];
            }
        }

        public object this[string key]
        {
            get
            {
                if (Owner.Columns[key] != null)
                {
                    return ColumnData[Owner.Columns[key].Index];
                }

                return null;
            }
        }
    }

 /// <summary>
 /// A class to represent a column in a FeatureTable object.
 /// </summary>
    public class FeatureColumn
    {
        public string Unit = "";
        public string Name = "";
        public int Index;

        public FeatureColumn(string name, int index, string unit)
        {
            Name = name;
            Index = index;
            Unit = unit;
        }
    }

}