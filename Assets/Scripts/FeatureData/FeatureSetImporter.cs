using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Valve.Newtonsoft.Json;
using VoTableReader;
using UnityEngine;
using System.Globalization;
using System.Text;
using System.Runtime.InteropServices;
using System.Linq;

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

        public static VoTable GetFeatureDataFromVOTable(string fileName)
        {
            VoTable voTable = new VoTable(fileName);
            return voTable;
        }

        public static Dictionary<string, string>[] GetFeatureDataFromASCII(string fileName)
        {
            return null;
        }


        public static FeatureSetImporter CreateSetFromVOTable(string fileName, string mappingFileName)
        {
            string mappingJson = File.ReadAllText(mappingFileName);
            FeatureSetImporter featureSet = JsonConvert.DeserializeObject<FeatureSetImporter>(mappingJson);
            featureSet.FileName = fileName;
            VoTable voTable = new VoTable(fileName);
            if (voTable.Rows.Count == 0 || voTable.Column.Count == 0)
            {
                Debug.Log($"Error reading VOTable! Note: Currently the VOTable may not contain xmlns declarations.");
                return featureSet;
            }
            string[] colNames = new string[voTable.Column.Count];
            for (int i = 0; i < voTable.Column.Count; i++)
                colNames[i] = voTable.Column[i].Name;
            featureSet.FeatureData = new Dictionary<string, string>[voTable.Rows.Count];
            int[] xyzIndices = { Array.IndexOf(colNames, featureSet.Mapping.X.Source),
                Array.IndexOf(colNames, featureSet.Mapping.Y.Source),
                Array.IndexOf(colNames, featureSet.Mapping.Z.Source) };
            if ( xyzIndices[0] < 0 ||  xyzIndices[1] < 0 ||  xyzIndices[2] < 0)
            {
                Debug.Log($"Minimum column parameters not found!");
                return featureSet;
            }
            int[] boxIndices =
            {
                Array.IndexOf(colNames, featureSet.Mapping.XMin.Source),
                Array.IndexOf(colNames, featureSet.Mapping.XMax.Source),
                Array.IndexOf(colNames, featureSet.Mapping.YMin.Source),
                Array.IndexOf(colNames, featureSet.Mapping.YMax.Source),
                Array.IndexOf(colNames, featureSet.Mapping.ZMin.Source),
                Array.IndexOf(colNames, featureSet.Mapping.ZMax.Source),
            };
            int nameIndex = Array.IndexOf(colNames, featureSet.Mapping.Name.Source);
            featureSet.NumberFeatures = voTable.Rows.Count;
            featureSet.FeatureNames = new string[featureSet.NumberFeatures];
            featureSet.FeaturePositions = new Vector3[featureSet.NumberFeatures];

            // if there are box dimensions, initialize array with number of features, otherwise initialize empty array
            if (boxIndices.Min() > 0)
            {
                featureSet.BoxMinPositions = new Vector3[featureSet.NumberFeatures];
                featureSet.BoxMaxPositions = new Vector3[featureSet.NumberFeatures];
            }
            else
            {
                featureSet.BoxMinPositions = new Vector3[0];
                featureSet.BoxMaxPositions = new Vector3[0];
            }
            for (int row = 0; row < voTable.Rows.Count; row++)   // For each row (feature)...
            {

                for (int i = 0; i < xyzIndices.Length; i++)
                {
                    float value = float.Parse((string)voTable.Rows[row].ColumnData[xyzIndices[i]]);
                    switch (i)
                    {
                        case 0:
                            featureSet.FeaturePositions[row].x = value;
                            break;
                        case 1:
                            featureSet.FeaturePositions[row].y = value;
                            break;
                        case 2:
                            featureSet.FeaturePositions[row].z = value;
                            break;
                    }
                }
                // ...get box bounds if they exist
                if (boxIndices.Min() > 0)
                {
                    for (int i = 0; i < boxIndices.Length; i++)
                    {
                        float value = float.Parse((string)voTable.Rows[row].ColumnData[boxIndices[i]]);
                        switch (i)
                        {
                            case 0:
                                featureSet.BoxMinPositions[row].x = value;
                                break;
                            case 1:
                                featureSet.BoxMaxPositions[row].x = value;
                                break;
                            case 2:
                                featureSet.BoxMinPositions[row].y = value;
                                break;
                            case 3:
                                featureSet.BoxMaxPositions[row].y = value;
                                break;
                            case 4:
                                featureSet.BoxMinPositions[row].z = value;
                                break;
                            case 5:
                                featureSet.BoxMaxPositions[row].z = value;
                                break;
                        }
                    }
                }
                // ...get name if exists
                if (nameIndex > 0)
                {
                    string value = (string)voTable.Rows[row].ColumnData[nameIndex];
                    featureSet.FeatureNames[row] = value;
                }
                else
                {
                    featureSet.FeatureNames[row] = "Source #" + row;
                }
            }
            return featureSet;
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
                    featureSet.FeatureNames[i] = "Source #" + i;
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
                // If there are no box dimensions, give empty arrays
                featureSet.BoxMinPositions = new Vector3[0];
                featureSet.BoxMaxPositions = new Vector3[0];
            }
            return featureSet;
        }

    }
}