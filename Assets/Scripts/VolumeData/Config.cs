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
        
        public bool maxModeDownsampling = false;
        public bool foveatedRendering = true;
        public bool bilinearFiltering = false;
        public int gpuMemoryLimitMb = 250;
        public int maxRaymarchingSteps = 384;
        
        [JsonConverter(typeof(StringEnumConverter))]
        public ColorMapEnum defaultColorMap = ColorMapEnum.Inferno;

        [JsonConverter(typeof(StringEnumConverter))]
        public ConfidenceLevel voiceCommandConfidenceLevel = ConfidenceLevel.Low;

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