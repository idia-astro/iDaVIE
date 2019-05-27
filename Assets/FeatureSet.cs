using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Valve.Newtonsoft.Json;
using UnityEngine;


namespace DataFeatures
{
    public class FeatureSet
    {

        const string xKey = "x";
        const string yKey = "y";
        const string zKey = "z";
        const string nameKey = "name";

        public string FileName { get; private set; }

        public Dictionary<string, string>[] FeatureData { get; private set; }
        public Vector3[] FeaturePositions { get; private set; }
        public string[] FeatureNames { get; private set; }
        public int NumberFeatures { get; private set; }

        public static FeatureSet CreateSetFromAscii(string fileName)
        {
            FeatureSet featureset = new FeatureSet();

            featureset.FileName = fileName;
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
                        if (Array.IndexOf(keys, xKey) < 0 || Array.IndexOf(keys, yKey) < 0 || Array.IndexOf(keys, zKey) < 0 || Array.IndexOf(keys, nameKey) < 0)
                        {
                            Debug.Log($"Minimum keys not found!");
                            return featureset;
                        }
                    }
                    else
                        continue;
                }
                else if (keys != null)
                {
                    if (sourceIndex == 0)
                        featureset.FeatureData = new Dictionary<string, string>[lines.Length - i];
                    featureset.FeatureData[sourceIndex] = new Dictionary<string, string>();
                    string[] values = System.Text.RegularExpressions.Regex.Split(lines[i], @"\s{2,}");
                    for (int j = 0; j < values.Length; j++)
                    {
                        featureset.FeatureData[sourceIndex].Add(keys[j], values[j]);
                    }
                    sourceIndex++;
                }
                else
                {
                    Debug.Log($"Keys not found!");
                    return featureset;
                }
            }
            featureset.NumberFeatures = featureset.FeatureData.Length;
            featureset.FeatureNames = new string[featureset.NumberFeatures];
            featureset.FeaturePositions = new Vector3[featureset.NumberFeatures];
            for (int i = 0; i < featureset.NumberFeatures; i++)
            {
                featureset.FeatureNames[i] = featureset.FeatureData[i][nameKey];
                featureset.FeaturePositions[i].x = Convert.ToSingle(featureset.FeatureData[i][xKey]);
                featureset.FeaturePositions[i].y = Convert.ToSingle(featureset.FeatureData[i][yKey]);
                featureset.FeaturePositions[i].z = Convert.ToSingle(featureset.FeatureData[i][zKey]);

            }
            return featureset;
        }
    }
}