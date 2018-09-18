using System;
using System.IO;
using UnityEngine;

namespace PointData
{
    [Serializable]
    public class DataMapping
    {
        public string Colormap;
        public Defaults Defaults;
        public Mapping Mapping;
        public MetaMapping MetaMapping;

        public static DataMapping CreateFromJson(string jsonString)
        {
            return JsonUtility.FromJson<DataMapping>(jsonString);
        }

        public static DataMapping CreateFromFile(string fileName)
        {
            string mappingJson = File.ReadAllText(fileName);
            return CreateFromJson(mappingJson);
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        public static DataMapping DefaultXyzMapping
        {
            get
            {
                DataMapping mapping = new DataMapping
                {
                    Defaults = new Defaults
                    {
                        Shape = DisplayShape.Billboard,
                        Color = "#FF0000",
                        Scale = 0.001f,
                        PointSize = 1f
                    },
                    Mapping = new Mapping
                    {
                        X = new MapFloatEntry {Source = "X"},
                        Y = new MapFloatEntry {Source = "Y"},
                        Z = new MapFloatEntry {Source = "Z"},
                        Cmap = new MapFloatEntry
                        {
                            Source = "Kmag",
                            Offset = -2,
                            Scale = 0.25f,
                            ScalingType = ScalingType.Linear,
                            Clamped = false
                        },
                        Size = new MapFloatEntry
                        {
                            Source = "Dm",
                            Offset = 1,
                            Scale = 0.01f,
                            ScalingType = ScalingType.Linear,
                            Clamped = false,
                            MinVal = 0.1f,
                            MaxVal = 4
                        }
                    }
                };
                return mapping;
            }
        }
    }

    [Serializable]
    public class Defaults
    {
        public string Color;
        public float Scale = 1;
        public float PointSize = 0.1f;
        public DisplayShape Shape = DisplayShape.Billboard;
    }

    [Serializable]
    public class Mapping
    {
        public MapFloatEntry Cmap;
        public MapFloatEntry Lat;
        public MapFloatEntry Long;
        public MapFloatEntry Opacity;
        public MapFloatEntry R;
        public MapFloatEntry Size;
        public MapFloatEntry X;
        public MapFloatEntry Y;
        public MapFloatEntry Z;
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
        public Name Name;
    }

    [Serializable]
    public class Name
    {
        public string Source;
    }

    [Serializable]
    public enum DisplayShape
    {
        Billboard,
        Cube,
        Sphere
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