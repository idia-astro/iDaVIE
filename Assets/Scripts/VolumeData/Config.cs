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
        private readonly string _schemaUri = "https://idavie.readthedocs.io/en/latest/_static/idavie_config_2.json";
        
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

        /// <summary>
        /// The different flags that can be applied to sources in a source list, and exported with them.
        /// </summary>
        /// <value>Default values are [-1, 0, 1].</value>
        public string[] flags = {"-1", "0", "1"};

        /// <summary>
        /// The number of steps for a full range when incrementing the histogram min/max.
        /// </summary>
        public int histogramIncrementSteps = 40;

        /// <summary>
        /// The number of steps per second when incrementing the histogram min/max.
        /// </summary>
        public int histogramStepsPerSecond = 8;

        /// <summary>
        /// The number of steps for a full range when incrementing the moment map threshold.
        /// </summary>
        public int momentMapThresholdSteps = 40;

        /// <summary>
        /// The number of steps per second when incrementing the histogram min/max.
        /// </summary>
        public int momentMapStepsPerSecond = 2;

        /// <summary>
        /// Use the quick, less precise percentile calculation for the scale min/max
        /// that uses the histogram instead of the full data set.
        /// </summary>
        public bool useQuickModeForPercentiles = true;
        
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
        
        // Allow the controller to display information outside the volume cube
        public bool displayCursorInfoOutsideCube = false;

        // Display the voice command status in the cursor information
        public bool displayVoiceCommandStatus = true;

        // Enable the requirement that the secondary button on the primary controller
        // must be held down to use voice commands
        public bool usePushToTalk = false;
        
        // Use the simple voice command status indicator
        public bool useSimpleVoiceCommandStatus = true;
        
        public bool importedFeaturesStartVisible = true;

        /// <summary>
        /// The size of the box that highlights where a cursor location is in the video recording mode, as a fraction of the overall cube size.
        /// </summary>
        public float videoCursorLocHighlightSize = 0.06f;
        
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

                // Tell debug log that new config file was created
                PlayerPrefs.SetInt("NewConfigFileCreated", 1);
                PlayerPrefs.SetString("ConfigFilePath", filepath);
                PlayerPrefs.Save();
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
            
            PlayerPrefs.SetString("ConfigFilePath", filepath);
            PlayerPrefs.Save();
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