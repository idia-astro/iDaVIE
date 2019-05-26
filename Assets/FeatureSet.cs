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

        public Dictionary<string, string>[] Features { get; private set; }

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
                        featureset.Features = new Dictionary<string, string>[lines.Length - i];
                    featureset.Features[sourceIndex] = new Dictionary<string, string>();
                    string[] values = System.Text.RegularExpressions.Regex.Split(lines[i], @"\s{2,}");
                    for (int j = 0; j < values.Length; j++)
                    {
                        featureset.Features[sourceIndex].Add(keys[j], values[j]);
                    }
                    sourceIndex++;
                }
                else
                {
                    Debug.Log($"Keys not found!");
                    return featureset;
                }
            }
            return featureset;
        }

        public void SpawnMarkers()
        {
            for (int i = 1; i <= Features.Length; i++)
            {
                
            }
        }

        public string GetFeatureName(int Index)
        {
            return Features[Index - 1][nameKey];
        }

        public Vector3Int GetXYZ(int Index)
        {
            return new Vector3Int(Convert.ToInt32(Features[Index - 1][xKey]), Convert.ToInt32(Features[Index - 1][yKey]), Convert.ToInt32(Features[Index - 1][zKey]));
        }
    }
}