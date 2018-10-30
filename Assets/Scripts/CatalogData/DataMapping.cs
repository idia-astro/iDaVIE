using System;
using System.IO;
using UnityEngine;
using Valve.Newtonsoft.Json;

namespace CatalogData
{
    [Serializable]
    public class DataMapping
    {
        public ColorMapEnum ColorMap = ColorMapEnum.Accent;
        public bool Spherical;
        public RenderType RenderType = RenderType.Billboard;
        public bool UniformColor;
        public bool UniformPointSize;
        public bool UniformOpacity;
        public MappingUniforms Uniforms = new MappingUniforms();

        public Mapping Mapping;
        public MetaMapping MetaMapping;

        public static DataMapping CreateFromJson(string jsonString)
        {
            DataMapping dataMapping = JsonConvert.DeserializeObject<DataMapping>(jsonString);
            if (dataMapping.UniformColor && dataMapping.Uniforms != null)
            {
                Color parsedColor;
                if (ColorUtility.TryParseHtmlString(dataMapping.Uniforms.ColorString, out parsedColor))
                {
                    dataMapping.Uniforms.Color = parsedColor;
                }
            }

            return dataMapping;
        }

        public static DataMapping CreateFromFile(string fileName)
        {
            string mappingJson = File.ReadAllText(fileName);
            return CreateFromJson(mappingJson);
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static DataMapping DefaultXyzMapping
        {
            get
            {
                DataMapping mapping = new DataMapping
                {
                    Spherical = false,
                    RenderType = RenderType.Billboard,
                    ColorMap = ColorMapEnum.Plasma,
                    UniformColor = true,
                    UniformPointSize = true,
                    UniformOpacity = true,
                    Uniforms = new MappingUniforms
                    {
                        Scale = 0.001f,
                        PointSize = 0.3f,
                        Color = Color.red
                    },
                    Mapping = new Mapping
                    {
                        X = new MapFloatEntry {Source = "X"},
                        Y = new MapFloatEntry {Source = "Y"},
                        Z = new MapFloatEntry {Source = "Z"},
                    }
                };
                return mapping;
            }
        }

        public static DataMapping DefaultSphericalMapping
        {
            get
            {
                DataMapping mapping = new DataMapping
                {
                    Spherical = true,
                    RenderType = RenderType.Billboard,
                    UniformColor = true,
                    UniformPointSize = true,
                    UniformOpacity = true,
                    Uniforms = new MappingUniforms
                    {
                        Scale = 0.001f,
                        PointSize = 0.3f,
                        Color = Color.red
                    },
                    Mapping = new Mapping
                    {
                        Lat = new MapFloatEntry {Source = "glon"},
                        Lng = new MapFloatEntry {Source = "glat"},
                        R = new MapFloatEntry {Source = "Dm"}
                    }
                };
                return mapping;
            }
        }
    }

    [Serializable]
    public class MappingUniforms
    {
        [HideInInspector] public string ColorString;
        public Color Color;
        [HideInInspector] public float Scale = 1;
        public float PointSize = 0.1f;
        [Range(0.0f, 1.0f)] public float Opacity = 1.0f;
    }

    [Serializable]
    public class Mapping
    {
        public MapFloatEntry Cmap;
        public MapFloatEntry Lat;
        public MapFloatEntry Lng;
        public MapFloatEntry Opacity;
        public MapFloatEntry R;
        public MapFloatEntry PointSize;
        public MapFloatEntry X;
        public MapFloatEntry Y;
        public MapFloatEntry Z;
        public MapFloatEntry X2;
        public MapFloatEntry Y2;
        public MapFloatEntry Z2;
    }

    [Serializable]
    public class MapFloatEntry
    {
        public bool Clamped;
        public float MaxVal;
        public float MinVal;
        public float Offset;
        public float Scale = 1;
        public ScalingType ScalingType = ScalingType.Linear;
        public string Source;
    }

    [Serializable]
    public class MetaMapping
    {
        public MapMetaEntry Name;
    }

    [Serializable]
    public class MapMetaEntry
    {
        public string Source;
    }

    [Serializable]
    public enum RenderType
    {
        Billboard,
        Line
    };

    [Serializable]
    public enum ScalingType
    {
        Linear,
        Log,
        Sqrt,
        Squared,
        Exp
    };
}