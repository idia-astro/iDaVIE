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
        public MapEntry Freq;
        public MapEntry Redshift;
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
    public class FeatureMapping
    {
        public Mapping Mapping { get; set; }

        public static FeatureMapping GetMappingFromFile(string fileName)
        {
            string mappingJson = File.ReadAllText(fileName);
            FeatureMapping map = JsonConvert.DeserializeObject<FeatureMapping>(mappingJson);
            return map;
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