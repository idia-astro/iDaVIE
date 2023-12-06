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
using Valve.Newtonsoft.Json.Linq;

namespace DataFeatures
{

    [Serializable]
    public class Mapping
    {
        public MapEntry ID;
        public MapEntry X;
        public MapEntry Y;
        public MapEntry Z;
        public MapEntry RA;
        public MapEntry Dec;
        public MapEntry Vel;
        public MapEntry Freq;
        public MapEntry Redshift;
        public MapEntry XMin;
        public MapEntry XMax;
        public MapEntry YMin;
        public MapEntry YMax;
        public MapEntry ZMin;
        public MapEntry ZMax;

        public MapEntry Flag;
        public string[] ImportedColumns;
    }

    [Serializable]
    public class MapEntry
    {
        public string Source;
    }

    [Serializable]
    public class FeatureMapping
    {
        public Mapping Mapping { get; set; }

        public static FeatureMapping GetMappingFromFile(string fileName)
        {
            string mappingJson = File.ReadAllText(fileName);
            FeatureMapping map = JsonConvert.DeserializeObject<FeatureMapping>(mappingJson);
            return map;
        }

        public void SaveMappingToFile(string fileName)
        {
            JObject o = JObject.FromObject(this);
            File.WriteAllText(fileName, o.ToString());
        }
    }

    public static class FeatureMapper
    {
        public static VoTable GetVOTableFromFile(string fileName)
        {
            VoTable voTable = new VoTable(fileName);
            return voTable;
        }
    }    
}