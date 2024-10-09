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

    }    
}