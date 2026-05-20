using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    public class Object
    {
        public static implicit operator bool(Object value) => value != null;
    }

    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject, new() => new T();
    }

    public interface ISerializationCallbackReceiver
    {
        void OnBeforeSerialize();
        void OnAfterDeserialize();
    }

    public class GameObject : Object
    {
        public Transform transform { get; } = new Transform();

        public GameObject()
        {
        }

        public GameObject(string name)
        {
        }

        public T AddComponent<T>() where T : new() => new T();
        public Object AddComponent(Type componentType) => Activator.CreateInstance(componentType) as Object;
        public T GetComponent<T>() where T : new() => new T();
        public T GetComponentInChildren<T>() where T : new() => new T();
    }

    public class Component : Object
    {
        public GameObject gameObject { get; } = new GameObject();
        public Transform transform { get; } = new Transform();

        public T GetComponent<T>() where T : new() => new T();
        public T GetComponentInChildren<T>() where T : new() => new T();
    }

    public class Behaviour : Component
    {
    }

    public class MonoBehaviour : Behaviour
    {
        protected Coroutine StartCoroutine(IEnumerator routine) => new Coroutine();
        protected static T FindObjectOfType<T>() where T : new() => new T();
        protected static T Instantiate<T>(T original) where T : Object => original;
        protected static void Destroy(Object target) { }
        protected static void DestroyImmediate(Object target) { }
        protected static void DontDestroyOnLoad(Object target) { }
    }

    public sealed class Coroutine
    {
    }

    public sealed class WaitForSeconds
    {
        public WaitForSeconds(float seconds)
        {
        }
    }

    public sealed class HeaderAttribute : Attribute
    {
        public HeaderAttribute(string header)
        {
        }
    }

    public sealed class RangeAttribute : Attribute
    {
        public RangeAttribute(float min, float max)
        {
        }
    }

    public sealed class SerializeField : Attribute
    {
    }

    public sealed class TooltipAttribute : Attribute
    {
        public TooltipAttribute(string tooltip)
        {
        }
    }

    public sealed class HideInInspector : Attribute
    {
    }

    public sealed class RequireComponentAttribute : Attribute
    {
        public RequireComponentAttribute(Type requiredComponent)
        {
        }

        public RequireComponentAttribute(Type requiredComponent, Type requiredComponent2)
        {
        }

        public RequireComponentAttribute(Type requiredComponent, Type requiredComponent2, Type requiredComponent3)
        {
        }
    }

    public enum Space
    {
        Self,
        World
    }

    public enum FilterMode
    {
        Point,
        Bilinear
    }

    public enum TextureFormat
    {
        R16,
        RFloat
    }

    public enum MeshTopology
    {
        Points
    }

    public struct Vector2
    {
        public float x;
        public float y;

        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public static Vector2 zero => new Vector2(0, 0);
        public static Vector2 one => new Vector2(1, 1);

        public static Vector2 operator +(Vector2 left, Vector2 right) =>
            new Vector2(left.x + right.x, left.y + right.y);

        public static Vector2 operator -(Vector2 left, Vector2 right) =>
            new Vector2(left.x - right.x, left.y - right.y);

        public static Vector2 operator *(Vector2 value, float scale) =>
            new Vector2(value.x * scale, value.y * scale);

        public static Vector2 operator *(float scale, Vector2 value) => value * scale;
    }

    public struct Vector3
    {
        public float x;
        public float y;
        public float z;

        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static Vector3 zero => new Vector3(0, 0, 0);
        public static Vector3 one => new Vector3(1, 1, 1);

        public float sqrMagnitude => x * x + y * y + z * z;

        public static Vector3 Min(Vector3 left, Vector3 right) =>
            new Vector3(Math.Min(left.x, right.x), Math.Min(left.y, right.y), Math.Min(left.z, right.z));

        public static Vector3 Max(Vector3 left, Vector3 right) =>
            new Vector3(Math.Max(left.x, right.x), Math.Max(left.y, right.y), Math.Max(left.z, right.z));

        public static Vector3 operator +(Vector3 left, Vector3 right) =>
            new Vector3(left.x + right.x, left.y + right.y, left.z + right.z);

        public static Vector3 operator -(Vector3 left, Vector3 right) =>
            new Vector3(left.x - right.x, left.y - right.y, left.z - right.z);

        public static Vector3 operator *(Vector3 value, float scale) =>
            new Vector3(value.x * scale, value.y * scale, value.z * scale);

        public static Vector3 operator *(float scale, Vector3 value) => value * scale;

        public static Vector3 operator /(Vector3 value, float scale) =>
            new Vector3(value.x / scale, value.y / scale, value.z / scale);
    }

    public struct Vector3Int
    {
        public int x;
        public int y;
        public int z;

        public Vector3Int(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static Vector3Int zero => new Vector3Int(0, 0, 0);
        public static Vector3Int one => new Vector3Int(1, 1, 1);

        public static Vector3Int Min(Vector3Int left, Vector3Int right) =>
            new Vector3Int(Math.Min(left.x, right.x), Math.Min(left.y, right.y), Math.Min(left.z, right.z));

        public static Vector3Int Max(Vector3Int left, Vector3Int right) =>
            new Vector3Int(Math.Max(left.x, right.x), Math.Max(left.y, right.y), Math.Max(left.z, right.z));

        public static Vector3Int FloorToInt(Vector3 value) =>
            new Vector3Int((int)Math.Floor(value.x), (int)Math.Floor(value.y), (int)Math.Floor(value.z));

        public static Vector3Int operator +(Vector3Int left, Vector3Int right) =>
            new Vector3Int(left.x + right.x, left.y + right.y, left.z + right.z);

        public static Vector3Int operator -(Vector3Int left, Vector3Int right) =>
            new Vector3Int(left.x - right.x, left.y - right.y, left.z - right.z);

        public static implicit operator Vector3(Vector3Int value) => new Vector3(value.x, value.y, value.z);
    }

    public struct Vector4
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public Vector4(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
    }

    public struct Quaternion
    {
        public static Quaternion Inverse(Quaternion rotation) => rotation;
        public static Quaternion operator *(Quaternion left, Quaternion right) => left;
    }

    public struct Matrix4x4
    {
        public Vector4 MultiplyVector(Vector3 vector) => new Vector4(vector.x, vector.y, vector.z, 0);
    }

    public struct Bounds
    {
        public Vector3 center;
        public Vector3 size;

        public Bounds(Vector3 center, Vector3 size)
        {
            this.center = center;
            this.size = size;
        }

        public bool Contains(Vector3 point) => true;
        public void Encapsulate(Vector3 point) { }
    }

    public struct Color
    {
        public float r;
        public float g;
        public float b;
        public float a;

        public Color(float r, float g, float b, float a = 1)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public static Color black => new Color(0, 0, 0);
        public static Color white => new Color(1, 1, 1);
        public static Color green => new Color(0, 1, 0);
        public static Color yellow => new Color(1, 1, 0);
        public static Color cyan => new Color(0, 1, 1);
    }

    public struct Color32
    {
        public byte r;
        public byte g;
        public byte b;
        public byte a;

        public Color32(byte r, byte g, byte b, byte a = 255)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public static implicit operator Color(Color32 value) =>
            new Color(value.r / 255f, value.g / 255f, value.b / 255f, value.a / 255f);

        public static implicit operator Color32(Color value) =>
            new Color32((byte)(value.r * 255), (byte)(value.g * 255), (byte)(value.b * 255), (byte)(value.a * 255));
    }

    public class Transform : Object
    {
        public Vector3 localPosition { get; set; }
        public Vector3 localEulerAngles { get; set; }
        public Vector3 position { get; set; }
        public Quaternion rotation { get; set; }
        public Vector3 localScale { get; set; } = Vector3.one;
        public Matrix4x4 localToWorldMatrix { get; set; }

        public void Rotate(float xAngle, float yAngle, float zAngle, Space relativeTo = Space.Self) { }
        public void Translate(float x, float y, float z) { }
        public Vector3 InverseTransformPoint(Vector3 position) => position;
    }

    public class Texture : Object
    {
    }

    public class Texture2D : Texture
    {
        public int width { get; }
        public int height { get; }

        public Texture2D(int width, int height, TextureFormat format, bool mipChain)
        {
            this.width = width;
            this.height = height;
        }

        public void LoadRawTextureData(IntPtr data, int size) { }
        public void LoadRawTextureData(byte[] data) { }
        public void Apply() { }
    }

    public class Texture3D : Texture
    {
        public int width { get; }
        public int height { get; }
        public int depth { get; }

        public Texture3D(int width, int height, int depth, TextureFormat format, bool mipChain)
        {
            this.width = width;
            this.height = height;
            this.depth = depth;
        }
    }

    public class RenderTexture : Texture
    {
    }

    public class Mesh : Object
    {
    }

    public class MeshFilter : Component
    {
        public Mesh mesh { get; set; }
        public Mesh sharedMesh { get; set; }
    }

    public class Camera : Behaviour
    {
        public static Camera main { get; set; }
    }

    public class ComputeBuffer : Object
    {
        public int count { get; }

        public ComputeBuffer(int count, int stride)
        {
            this.count = count;
        }

        public void SetData<T>(IList<T> data) { }
        public void SetData<T>(IList<T> data, int managedBufferStartIndex, int computeBufferStartIndex, int count) { }
        public void Release() { }
    }

    public class ComputeShader : Object
    {
        public int FindKernel(string name) => 0;
        public void SetBuffer(int kernelIndex, string name, ComputeBuffer buffer) { }
        public void SetBuffer(int kernelIndex, int nameId, ComputeBuffer buffer) { }
        public void SetInt(string name, int value) { }
        public void SetFloat(string name, float value) { }
        public void Dispatch(int kernelIndex, int threadGroupsX, int threadGroupsY, int threadGroupsZ) { }
    }

    public class Material : Object
    {
        public void SetTexture(int nameId, Texture value) { }
        public void SetInt(int nameId, int value) { }
        public void SetFloat(int nameId, float value) { }
        public void SetVector(int nameId, Vector3 value) { }
        public void SetVector(int nameId, Vector4 value) { }
        public void SetVectorArray(int nameId, Vector4[] values) { }
        public void SetColor(int nameId, Color value) { }
        public void SetMatrix(int nameId, Matrix4x4 value) { }
        public void SetBuffer(int nameId, ComputeBuffer value) { }
        public bool SetPass(int pass) => true;
    }

    public class MeshRenderer : Component
    {
        public Material material { get; set; }
    }

    public class Canvas : Behaviour
    {
    }

    public static class Shader
    {
        public static int PropertyToID(string name) => name.GetHashCode();
        public static void WarmupAllShaders() { }
        public static void EnableKeyword(string keyword) { }
        public static void DisableKeyword(string keyword) { }
    }

    public static class Graphics
    {
        public static void CopyTexture(Texture src, int srcElement, int srcMip, int srcX, int srcY, int srcWidth, int srcHeight, Texture dst, int dstElement, int dstMip, int dstX, int dstY) { }
        public static void DrawProceduralNow(MeshTopology topology, int vertexCount) { }
    }

    public static class Mathf
    {
        public static float Floor(float value) => (float)Math.Floor(value);
        public static int FloorToInt(float value) => (int)Math.Floor(value);
        public static int CeilToInt(float value) => (int)Math.Ceiling(value);
        public static int RoundToInt(float value) => (int)Math.Round(value);
        public static int Clamp(int value, int min, int max) => Math.Min(Math.Max(value, min), max);
        public static float Clamp(float value, float min, float max) => Math.Min(Math.Max(value, min), max);
        public static float Sqrt(float value) => (float)Math.Sqrt(value);
    }

    public static class Random
    {
        public static float Range(float minInclusive, float maxInclusive) => minInclusive;
    }

    public static class Time
    {
        public static float deltaTime => 0;
    }

    public static class Application
    {
        public static string dataPath => ".";
        public static bool isEditor => true;
    }

    public static class PlayerPrefs
    {
        public static void SetInt(string key, int value) { }
        public static void SetString(string key, string value) { }
        public static void Save() { }
    }

    public static class Debug
    {
        public static void Assert(bool condition) { }
        public static void Log(object message) { }
        public static void LogWarning(object message) { }
        public static void LogError(object message) { }
    }

    public class UnityEvent
    {
        public void AddListener(Action call) { }
        public void RemoveListener(Action call) { }
        public void Invoke() { }
    }

    public class UnityEvent<T0>
    {
        public void AddListener(Action<T0> call) { }
        public void RemoveListener(Action<T0> call) { }
        public void Invoke(T0 arg0) { }
    }
}

namespace UnityEngine.Events
{
    public class UnityEvent : UnityEngine.UnityEvent
    {
    }

    public class UnityEvent<T0> : UnityEngine.UnityEvent<T0>
    {
    }
}

namespace UnityEngine.UI
{
    public class Slider : UnityEngine.Component
    {
        public float value { get; set; }
    }
}

namespace UnityEngine.Windows.Speech
{
    public enum ConfidenceLevel
    {
        High,
        Medium,
        Low,
        Rejected
    }
}

namespace TMPro
{
    public class TextMeshProUGUI : UnityEngine.Component
    {
        public string text { get; set; }
    }
}

namespace Valve.VR
{
    public class SteamVR
    {
        public static SteamVR instance { get; set; }
        public CVRCompositor compositor { get; } = new CVRCompositor();
    }

    public class CVRCompositor
    {
        public bool GetFrameTiming(ref Compositor_FrameTiming timing, uint framesAgo) => true;
    }

    public struct Compositor_FrameTiming
    {
        public uint m_nSize;
        public double m_flSystemTimeInSeconds;
    }
}

namespace Valve.Newtonsoft.Json
{
    public sealed class JsonPropertyAttribute : Attribute
    {
        public JsonPropertyAttribute(string propertyName)
        {
        }
    }

    public sealed class JsonConverterAttribute : Attribute
    {
        public JsonConverterAttribute(Type converterType)
        {
        }
    }

    public enum Formatting
    {
        None,
        Indented
    }

    public sealed class JsonSerializerSettings
    {
        public EventHandler<Serialization.ErrorEventArgs> Error { get; set; }
    }

    public class JsonReaderException : Exception
    {
    }

    public static class JsonConvert
    {
        public static T DeserializeObject<T>(string value, JsonSerializerSettings settings) => Activator.CreateInstance<T>();
        public static string SerializeObject(object value, Formatting formatting) => string.Empty;
    }
}

namespace Valve.Newtonsoft.Json.Converters
{
    public class StringEnumConverter
    {
    }
}

namespace Valve.Newtonsoft.Json.Serialization
{
    public sealed class ErrorEventArgs : EventArgs
    {
    }
}

namespace CatalogData
{
    public class CatalogDataSetRenderer : UnityEngine.MonoBehaviour
    {
    }
}

namespace LineRenderer
{
    public abstract class LineShape : UnityEngine.Object
    {
        public UnityEngine.Transform Parent { get; set; }
        public UnityEngine.Vector3 Center { get; set; }
        public UnityEngine.Vector3 Bounds { get; set; }
        public UnityEngine.Color Color { get; set; }
        public bool Active { get; private set; }

        public void Activate() => Active = true;
        public void Deactivate() => Active = false;
        public void Destroy() { }
    }

    public class CuboidLine : LineShape
    {
    }

    public class PolyLine : LineShape
    {
        public List<UnityEngine.Vector3> Vertices { get; set; } = new List<UnityEngine.Vector3>();
    }
}

namespace DataFeatures
{
    public class Feature
    {
        public Feature(UnityEngine.Vector3 cornerMin, UnityEngine.Vector3 cornerMax, UnityEngine.Color color, string name, string flag, int listIndex, int id, string[] rawStrings, bool visible)
        {
            CornerMin = cornerMin;
            CornerMax = cornerMax;
            Name = name;
            Id = id;
            Visible = visible;
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public bool Visible { get; set; }
        public UnityEngine.Vector3 CornerMin { get; set; }
        public UnityEngine.Vector3 CornerMax { get; set; }

        public UnityEngine.Vector3 GetMinBounds() => CornerMin;
        public UnityEngine.Vector3 GetMaxBounds() => CornerMax;

        public static void SetCubeColors(LineRenderer.CuboidLine line, UnityEngine.Color color, bool active) { }
    }

    public class FeatureMenuScrollerDataSource
    {
        public void InitData() { }
    }

    public class FeatureSetManager : UnityEngine.MonoBehaviour
    {
        public bool NeedToRespawnMenuList { get; set; }
        public Feature SelectedFeature { get; set; }

        public bool SelectFeature(UnityEngine.Vector3 cursor) => false;
        public void CreateSelectionFeatureSet() { }
        public FeatureSetRenderer CreateMaskFeatureSet() => new FeatureSetRenderer();
        public void AddSelectedFeatureToNewSet() { }
    }

    public class FeatureSetRenderer : UnityEngine.MonoBehaviour
    {
        public List<Feature> FeatureList { get; } = new List<Feature>();
        public UnityEngine.Color FeatureColor { get; set; }
        public FeatureSetManager FeatureManager { get; } = new FeatureSetManager();
        public FeatureMenuScrollerDataSource FeatureMenuScrollerDataSource { get; } = new FeatureMenuScrollerDataSource();

        public void AddFeature(Feature feature) => FeatureList.Add(feature);
        public void SpawnFeaturesFromSourceStats(Dictionary<int, DataAnalysis.SourceStats> sourceStats) { }
    }
}

public class VolumeInputController : UnityEngine.MonoBehaviour
{
    public void Teleport(UnityEngine.Vector3 boundsMin, UnityEngine.Vector3 boundsMax) { }
}

public class MomentMapMenuController : UnityEngine.MonoBehaviour
{
    public enum ThresholdType
    {
        Mask,
        Range
    }

    public enum LimitType
    {
        ZScale,
        Percentile
    }
}

namespace VolumeData
{
    public class VolumeCommandController : UnityEngine.MonoBehaviour
    {
        public MomentMapMenuController momentMapMenuController { get; set; } = new MomentMapMenuController();
    }

    public class MomentMapRenderer : UnityEngine.MonoBehaviour
    {
        public UnityEngine.Texture3D DataCube { get; set; }
        public UnityEngine.Texture3D MaskCube { get; set; }
        public bool Inverted { get; set; }
        public MomentMapMenuController momentMapMenuController { get; set; }

        public void CalculateMomentMaps() { }
    }
}
