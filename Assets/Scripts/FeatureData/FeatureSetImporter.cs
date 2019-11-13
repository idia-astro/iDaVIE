using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Valve.Newtonsoft.Json;
using UnityEngine;
using System.Globalization;
using System.Text;
using System.Runtime.InteropServices;

namespace DataFeatures
{

    [Serializable]
    public class Mapping
    {
        public MapEntry Name;
        public MapEntry X;
        public MapEntry Y;
        public MapEntry Z;
        public MapEntry Index;
        public MapEntry RA;
        public MapEntry Dec;
        public MapEntry Vel;
        public MapEntry XMin;
        public MapEntry XMax;
        public MapEntry YMin;
        public MapEntry YMax;
        public MapEntry ZMin;
        public MapEntry ZMax;
    }

    [Serializable]
    public class MapEntry
    {
        public string Source;
    }

    [Serializable]
    public class FeatureSetImporter
    {

        public string X   { get; set; }
        public string Y   { get; set; }
        public string Z   { get; set; }
        public string Name { get; set; }
        public Mapping Mapping { get; set; }
        public string FileName;
        public string MappingFileName;

        public Dictionary<string, string>[] FeatureData { get; private set; }
        public Vector3[] FeaturePositions { get; private set; }
        public Vector3[] BoxMinPositions { get; private set; }
        public Vector3[] BoxMaxPositions { get; private set; }
        public string[] FeatureNames { get; private set; }
        public int NumberFeatures { get; private set; }


        public static FeatureSetImporter CreateSetFromVOTable(string fileName, string mappingFileName)
        {
            string mappingJson = File.ReadAllText(mappingFileName);
            FeatureSetImporter featureSet = JsonConvert.DeserializeObject<FeatureSetImporter>(mappingJson);
            FeatureSetImporter featureSetImporter = new FeatureSetImporter();
            featureSetImporter.FileName = fileName;
            int status, ncols, nrows;
            IntPtr votable_ptr, meta_ptr, field_ptr, name_ptr, data_ptr, row_ptr, column_ptr, float_ptr;
            votable_ptr = meta_ptr = field_ptr = name_ptr = data_ptr = row_ptr = column_ptr = float_ptr = IntPtr.Zero;
            //StringBuilder fieldName = new StringBuilder(70);
            string xpath = "/RESOURCE[1]/TABLE[1]";
            Debug.Log($"xpath: " + xpath );
            VOTableReader.VOTableInitialize(out votable_ptr);
            VOTableReader.VOTableOpenFile(votable_ptr, fileName, xpath, out status);
            if (VOTableReader.VOTableGetName(votable_ptr, out name_ptr, out status) == 0)
            {
                //Debug.Log($"Name of VOTable: " + Marshal.PtrToStringAnsi(name_ptr));
                Debug.Log($"Name of VOTable: " + Marshal.PtrToStringAnsi(name_ptr));
                //Debug.Log($"Name of VOTable: " + Marshal.PtrToStringAnsi(name_ptr));
            }
            VOTableReader.VOTableGetTableData(votable_ptr, out data_ptr, out status);
            VOTableReader.VOTableGetMetaData(votable_ptr, out meta_ptr, out status);
            VOTableReader.MetaDataGetNumCols(meta_ptr, out ncols, out status);
            VOTableReader.TableDataGetNumRows(data_ptr, out nrows, out status);
            Debug.Log($"Number of rows: " + nrows);
            Debug.Log($"Number of columns: " + ncols);
            string[] colNames = new string[ncols];
            for (int i = 0; i < ncols; i++)
            {
                VOTableReader.MetaDataGetField(meta_ptr, out field_ptr, i, out status);
                VOTableReader.FieldGetName(field_ptr, out name_ptr, out status);
                //string name = fieldName.ToString();
                //Debug.Log($"Column name: " + Marshal.PtrToStringAnsi(name_ptr));
                colNames[i] = Marshal.PtrToStringAnsi(name_ptr);
            }
            int[] xyzIndices = { Array.IndexOf(colNames, featureSet.Mapping.X.Source),
                Array.IndexOf(colNames, featureSet.Mapping.Y.Source),
                Array.IndexOf(colNames, featureSet.Mapping.Z.Source) };
            if ( xyzIndices[0] < 0 ||  xyzIndices[1] < 0 ||  xyzIndices[2] < 0)
            {
                Debug.Log($"Minimum column parameters not found!");
                return featureSet;
            }

            int sourceIndex = 0;
            int numElements = 0;
            for (int row = 0; row < nrows; row++)
            {
                if (VOTableReader.TableDataGetRow(data_ptr, out row_ptr, out status) == 0)
                {
                    for (int col = 0; col < ncols; col++)
                    {
                        if (VOTableReader.RowGetColumn(row_ptr, out column_ptr, out status) == 0)
                        {
                            if (col == xyzIndices[0])
                            {
                                VOTableReader.ColumnGetFloatArray(column_ptr, out float_ptr, out numElements, out status);
                                if (numElements > 1)
                                {
                                    Debug.Log("Please use Feature Table with single element values");
                                    return featureSet;
                                }
                                
                            }
                            else if (col == xyzIndices[1])
                            {

                            }
                            else if (col == xyzIndices[2])
                            {

                            }
                        }
                    }
                }
                /*
                VOTableReader.TableDataGetRow(data_ptr, out row_ptr, out status);
                if (sourceIndex == 0)
                    featureSet.FeatureData = new Dictionary<string, string>[lines.Length - i];
                featureSet.FeatureData[sourceIndex] = new Dictionary<string, string>();
                string[] values = System.Text.RegularExpressions.Regex.Split(lines[i], @"\s{2,}");
                for (int j = 0; j < values.Length; j++)
                {
                    featureSet.FeatureData[sourceIndex].Add(keys[j], values[j]);
                }
                sourceIndex++;
                */
            }



            featureSet.NumberFeatures = featureSet.FeatureData.Length;
            featureSet.FeatureNames = new string[featureSet.NumberFeatures];
            featureSet.FeaturePositions = new Vector3[featureSet.NumberFeatures];
            for (int i = 0; i < featureSet.NumberFeatures; i++)
            {
                featureSet.FeaturePositions[i].x = Convert.ToSingle(featureSet.FeatureData[i][featureSet.Mapping.X.Source], CultureInfo.InvariantCulture);
                featureSet.FeaturePositions[i].y = Convert.ToSingle(featureSet.FeatureData[i][featureSet.Mapping.Y.Source], CultureInfo.InvariantCulture);
                featureSet.FeaturePositions[i].z = Convert.ToSingle(featureSet.FeatureData[i][featureSet.Mapping.Z.Source], CultureInfo.InvariantCulture);
            }
            
            if (votable_ptr != IntPtr.Zero)
                VOTableReader.FreeMemory(votable_ptr);
            if (meta_ptr != IntPtr.Zero)
                VOTableReader.FreeMemory(meta_ptr);
            if (name_ptr != IntPtr.Zero)
                VOTableReader.FreeMemory(name_ptr);
            if (field_ptr != IntPtr.Zero)
                VOTableReader.FreeMemory(field_ptr);
            if (data_ptr != IntPtr.Zero)
                VOTableReader.FreeMemory(data_ptr);
            if (row_ptr != IntPtr.Zero)
                VOTableReader.FreeMemory(row_ptr);
            if (column_ptr != IntPtr.Zero)
                VOTableReader.FreeMemory(column_ptr);

            return featureSetImporter;
        }

        public static FeatureSetImporter CreateSetFromAscii(string fileName, string mappingFileName)
        {
            string mappingJson = File.ReadAllText(mappingFileName);
            FeatureSetImporter featureSet = JsonConvert.DeserializeObject<FeatureSetImporter>(mappingJson);
            featureSet.FileName = fileName;
            string[] keys = null;
            string[] lines = File.ReadAllLines(fileName);
            int sourceIndex = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i][0] == '#')
                {
                    if (i == 1)
                    {
                        keys = System.Text.RegularExpressions.Regex.Split(lines[i], @"\s{2,}");  //delimiter in json?
                        if (Array.IndexOf(keys, featureSet.Mapping.X.Source) < 0 || Array.IndexOf(keys, featureSet.Mapping.Y.Source) < 0 || Array.IndexOf(keys, featureSet.Mapping.Z.Source) < 0)
                        {
                            Debug.Log($"Minimum keys not found!");
                            return featureSet;
                        }
                    }
                    else
                        continue;
                }
                else if (keys != null)
                {
                    if (sourceIndex == 0)
                        featureSet.FeatureData = new Dictionary<string, string>[lines.Length - i];
                    featureSet.FeatureData[sourceIndex] = new Dictionary<string, string>();
                    string[] values = System.Text.RegularExpressions.Regex.Split(lines[i], @"\s{2,}");
                    for (int j = 0; j < values.Length; j++)
                    {
                        featureSet.FeatureData[sourceIndex].Add(keys[j], values[j]);
                    }
                    sourceIndex++;
                }
                else
                {
                    Debug.Log($"Keys not found!");
                    return featureSet;
                }
            }
            featureSet.NumberFeatures = featureSet.FeatureData.Length;
            featureSet.FeatureNames = new string[featureSet.NumberFeatures];
            featureSet.FeaturePositions = new Vector3[featureSet.NumberFeatures];
            for (int i = 0; i < featureSet.NumberFeatures; i++)
            {
                featureSet.FeaturePositions[i].x = Convert.ToSingle(featureSet.FeatureData[i][featureSet.Mapping.X.Source], CultureInfo.InvariantCulture);
                featureSet.FeaturePositions[i].y = Convert.ToSingle(featureSet.FeatureData[i][featureSet.Mapping.Y.Source], CultureInfo.InvariantCulture);
                featureSet.FeaturePositions[i].z = Convert.ToSingle(featureSet.FeatureData[i][featureSet.Mapping.Z.Source], CultureInfo.InvariantCulture);
            }
            bool nameSourceFound = Array.IndexOf(keys, featureSet.Mapping.Name.Source) >= 0;
            if (nameSourceFound)
            {
                for (int i = 0; i < featureSet.NumberFeatures; i++)
                {
                    featureSet.FeatureNames[i] = featureSet.FeatureData[i][featureSet.Mapping.Name.Source];
                }
            }
            else
            {
                for (int i = 0; i < featureSet.NumberFeatures; i++)
                {
                    featureSet.FeatureNames[i] = i.ToString();
                }
            }

            bool boundingBoxSourcesFound = Array.IndexOf(keys, featureSet.Mapping.XMin.Source) >= 0 && Array.IndexOf(keys, featureSet.Mapping.XMax.Source) >= 0 &&
                Array.IndexOf(keys, featureSet.Mapping.YMin.Source) >= 0 && Array.IndexOf(keys, featureSet.Mapping.YMax.Source) >= 0 &&
                Array.IndexOf(keys, featureSet.Mapping.ZMin.Source) >= 0 && Array.IndexOf(keys, featureSet.Mapping.ZMax.Source) >= 0;
            if (boundingBoxSourcesFound)
            {
                featureSet.BoxMinPositions = new Vector3[featureSet.NumberFeatures];
                featureSet.BoxMaxPositions = new Vector3[featureSet.NumberFeatures];
                for (int i = 0; i < featureSet.NumberFeatures; i++)
                {
                    featureSet.BoxMinPositions[i].x = Convert.ToSingle(featureSet.FeatureData[i][featureSet.Mapping.XMin.Source], CultureInfo.InvariantCulture);
                    featureSet.BoxMinPositions[i].y = Convert.ToSingle(featureSet.FeatureData[i][featureSet.Mapping.YMin.Source], CultureInfo.InvariantCulture);
                    featureSet.BoxMinPositions[i].z = Convert.ToSingle(featureSet.FeatureData[i][featureSet.Mapping.ZMin.Source], CultureInfo.InvariantCulture);
                    featureSet.BoxMaxPositions[i].x = Convert.ToSingle(featureSet.FeatureData[i][featureSet.Mapping.XMax.Source], CultureInfo.InvariantCulture);
                    featureSet.BoxMaxPositions[i].y = Convert.ToSingle(featureSet.FeatureData[i][featureSet.Mapping.YMax.Source], CultureInfo.InvariantCulture);
                    featureSet.BoxMaxPositions[i].z = Convert.ToSingle(featureSet.FeatureData[i][featureSet.Mapping.ZMax.Source], CultureInfo.InvariantCulture);
                }
            }
            else
            {
                featureSet.BoxMinPositions = new Vector3[0];
                featureSet.BoxMaxPositions = new Vector3[0];
            }
            return featureSet;
        }

    }
}