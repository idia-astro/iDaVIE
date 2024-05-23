using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Windows.Speech;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Converters;
using ErrorEventArgs = Valve.Newtonsoft.Json.Serialization.ErrorEventArgs;

namespace VolumeData
{
    public class Config
    {
        [JsonProperty("$schema")]
        private readonly string _schemaUri = "https://idavie.readthedocs.io/en/latest/_static/idavie_config_1.json";
        
        public bool maxModeDownsampling = true;
        public bool foveatedRendering = true;
        public bool bilinearFiltering = false;
        public int gpuMemoryLimitMb = 384;
        public int maxRaymarchingSteps = 384;

        [JsonConverter(typeof(StringEnumConverter))]
        public AngleCoordFormat angleCoordFormat = AngleCoordFormat.Sexagesimal;
        
        [JsonConverter(typeof(StringEnumConverter))]
        public VelocityUnit velocityUnit = VelocityUnit.Km;

        [JsonConverter(typeof(StringEnumConverter))]
        public ColorMapEnum defaultColorMap = ColorMapEnum.Inferno;

        [JsonConverter(typeof(StringEnumConverter))]
        public ScalingType defaultScalingType = ScalingType.Linear;

        [JsonConverter(typeof(StringEnumConverter))]
        public ConfidenceLevel voiceCommandConfidenceLevel = ConfidenceLevel.Low;

        public string[] flags = {"-1", "0", "1"};
        
        // Default rest frequencies in GHz. These are used for frequency <-> velocity conversions
        public Dictionary<String,double> restFrequenciesGHz = new Dictionary<string, double>
        {
            {"HI", 1.420406},
            {"12CO(1-0)", 115.271},
            {"12CO(2-1)", 230.538},
            {"12CO(3-2)", 345.796},
            {"Halpha", 456806}
        };
        
        public bool tunnellingVignetteOn = true;
        public float tunnellingVignetteIntensity = 1.0f;
        public float tunnellingVignetteEnd = 0.40f;

        public bool importedFeaturesStartVisible = true;
        
        public class RenderConfig
        {
            [JsonConverter(typeof(StringEnumConverter))]
            public ColorMapEnum colorMap = ColorMapEnum.Inferno;

            [JsonConverter(typeof(StringEnumConverter))]
            public ScalingType scalingType = ScalingType.Linear;
        }

        public class MomentConfig
        {
            [JsonConverter(typeof(StringEnumConverter))]
            public MomentMapMenuController.ThresholdType defaultThresholdType = MomentMapMenuController.ThresholdType.Mask;
            [JsonConverter(typeof(StringEnumConverter))]
            public MomentMapMenuController.LimitType defaultLimitType = MomentMapMenuController.LimitType.ZScale;
            public float defaultThreshold = 0;
            public float mom1MaskThreshold = 0;
            
            public RenderConfig m0 = new RenderConfig { colorMap = ColorMapEnum.Plasma, scalingType = ScalingType.Sqrt };
            public RenderConfig m1 = new RenderConfig { colorMap = ColorMapEnum.Turbo, scalingType = ScalingType.Linear };
        }

        public MomentConfig momentMaps = new MomentConfig();
        

        private static Config _instance;

        private static string DefaultPath
        {
            get
            {
                var parentPath = Path.GetDirectoryName(Application.dataPath);
                return $"{parentPath}/config.json";
            }
        }

        private static void LogJsonErrors(object sender, ErrorEventArgs e)
        {
            Debug.LogWarning(e.ToString());
        }
        
        private static Config FromFile(string filepath = "")
        {
            if (filepath.Length == 0)
            {
                filepath = DefaultPath;
            }

            // Return default config if it doesn't exist
            if (!File.Exists(filepath))
            {
                var defaultConfig = new Config();
                
                defaultConfig.WriteToFile();
                return defaultConfig;
            }


            Config result;
            try
            {
                using (var sr = new StreamReader(filepath))
                {
                    var jsonString = sr.ReadToEnd();
                    result = JsonConvert.DeserializeObject<Config>(jsonString, new JsonSerializerSettings
                    {
                        Error = LogJsonErrors
                    });
                }
            }
            catch (JsonReaderException ex)
            {
                Debug.LogWarning("Config file has invalid entries. Using default config");
                return new Config();
            }
            
            return result;
        }

        public static Config Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FromFile();
                }

                return _instance;
            }
        }

        private void WriteToFile(string filepath = "")
        {
            if (filepath.Length == 0)
            {
                filepath = DefaultPath;
            }

            var jsonString = JsonConvert.SerializeObject(this, Formatting.Indented);
            using (StreamWriter sw = new StreamWriter(filepath))
            {
                sw.Write(jsonString);
            }

            Debug.Log($"Config file written to {filepath}");
        }
    }
}