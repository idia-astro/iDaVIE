using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Valve.Newtonsoft.Json;
using UnityEngine;
using System.Globalization;

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
                        if (Array.IndexOf(keys, featureSet.Mapping.X.Source) < 0 || Array.IndexOf(keys, featureSet.Mapping.Y.Source) < 0 || Array.IndexOf(keys, featureSet.Mapping.Z.Source) < 0 || Array.IndexOf(keys, featureSet.Mapping.Name.Source) < 0)
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
            // TODO: Check if min max defined 
            featureSet.BoxMinPositions = new Vector3[featureSet.NumberFeatures];
            featureSet.BoxMaxPositions = new Vector3[featureSet.NumberFeatures];
            for (int i = 0; i < featureSet.NumberFeatures; i++)
            {
                featureSet.FeatureNames[i] = featureSet.FeatureData[i][featureSet.Mapping.Name.Source];
                featureSet.BoxMinPositions[i].x = Convert.ToSingle(featureSet.FeatureData[i][featureSet.Mapping.XMin.Source], CultureInfo.InvariantCulture);
                featureSet.BoxMinPositions[i].y = Convert.ToSingle(featureSet.FeatureData[i][featureSet.Mapping.YMin.Source], CultureInfo.InvariantCulture);
                featureSet.BoxMinPositions[i].z = Convert.ToSingle(featureSet.FeatureData[i][featureSet.Mapping.ZMin.Source], CultureInfo.InvariantCulture);
                featureSet.BoxMaxPositions[i].x = Convert.ToSingle(featureSet.FeatureData[i][featureSet.Mapping.XMax.Source], CultureInfo.InvariantCulture);
                featureSet.BoxMaxPositions[i].y = Convert.ToSingle(featureSet.FeatureData[i][featureSet.Mapping.YMax.Source], CultureInfo.InvariantCulture);
                featureSet.BoxMaxPositions[i].z = Convert.ToSingle(featureSet.FeatureData[i][featureSet.Mapping.ZMax.Source], CultureInfo.InvariantCulture);
                featureSet.FeaturePositions[i].x = Convert.ToSingle(featureSet.FeatureData[i][featureSet.Mapping.X.Source], CultureInfo.InvariantCulture);
                featureSet.FeaturePositions[i].y = Convert.ToSingle(featureSet.FeatureData[i][featureSet.Mapping.Y.Source], CultureInfo.InvariantCulture);
                featureSet.FeaturePositions[i].z = Convert.ToSingle(featureSet.FeatureData[i][featureSet.Mapping.Z.Source], CultureInfo.InvariantCulture);

            }
            return featureSet;
        }

    }
}