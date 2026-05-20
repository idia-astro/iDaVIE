using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    public class Object
    {
        public static implicit operator bool(Object value) => value != null;
        public static void Destroy(Object target) { }
        public static void DestroyImmediate(Object target) { }
        public static T Instantiate<T>(T original) where T : Object => original;
        public static T Instantiate<T>(T original, Transform parent) where T : Object => original;
        public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation) where T : Object => original;
        public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation, Transform parent) where T : Object => original;
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
        public GameObject gameObject => this;
        public string name { get; set; }
        public string tag { get; set; }
        public bool activeSelf { get; set; }
        public bool activeInHierarchy { get; set; }
        public Transform transform { get; } = new Transform();

        public GameObject()
        {
        }

        public GameObject(string name)
        {
            this.name = name;
        }

        public T AddComponent<T>() where T : new() => new T();
        public Object AddComponent(Type componentType) => Activator.CreateInstance(componentType) as Object;
        public T GetComponent<T>() where T : new() => new T();
        public T GetComponentInChildren<T>() where T : new() => new T();
        public T GetComponentInChildren<T>(bool includeInactive) where T : new() => new T();
        public T GetComponentInParent<T>() where T : new() => new T();
        public T[] GetComponents<T>() => Array.Empty<T>();
        public T[] GetComponentsInChildren<T>() => Array.Empty<T>();
        public T[] GetComponentsInChildren<T>(bool includeInactive) => Array.Empty<T>();
        public void SetActive(bool value)
        {
            activeSelf = value;
            activeInHierarchy = value;
        }

        public bool CompareTag(string tag) => string.Equals(this.tag, tag, StringComparison.Ordinal);
        public static GameObject Find(string name) => new GameObject(name);
        public static GameObject FindGameObjectWithTag(string tag) => new GameObject { tag = tag };
        public static GameObject[] FindGameObjectsWithTag(string tag) => Array.Empty<GameObject>();
        public static GameObject CreatePrimitive(PrimitiveType type) => new GameObject(type.ToString());
    }

    public class Component : Object
    {
        public GameObject gameObject { get; } = new GameObject();
        public Transform transform { get; } = new Transform();
        public string name { get; set; }
        public string tag { get; set; }
        public bool enabled { get; set; } = true;
        public bool isActiveAndEnabled { get; set; } = true;

        public T GetComponent<T>() where T : new() => new T();
        public T GetComponentInChildren<T>() where T : new() => new T();
        public T GetComponentInChildren<T>(bool includeInactive) where T : new() => new T();
        public T GetComponentInParent<T>() where T : new() => new T();
        public T[] GetComponents<T>() => Array.Empty<T>();
        public T[] GetComponentsInChildren<T>() => Array.Empty<T>();
        public T[] GetComponentsInChildren<T>(bool includeInactive) => Array.Empty<T>();
        public bool CompareTag(string tag) => string.Equals(this.tag, tag, StringComparison.Ordinal);
    }

    public class Behaviour : Component
    {
    }

    public class MonoBehaviour : Behaviour
    {
        protected Coroutine StartCoroutine(IEnumerator routine) => new Coroutine();
        protected void StopCoroutine(Coroutine routine) { }
        protected void StopCoroutine(IEnumerator routine) { }
        protected void StopAllCoroutines() { }
        protected static T FindObjectOfType<T>() where T : new() => new T();
        protected static T FindObjectOfType<T>(bool includeInactive) where T : new() => new T();
        protected static T Instantiate<T>(T original) where T : Object => original;
        protected static T Instantiate<T>(T original, Transform parent) where T : Object => original;
        protected static T Instantiate<T>(T original, Vector3 position, Quaternion rotation) where T : Object => original;
        protected static T Instantiate<T>(T original, Vector3 position, Quaternion rotation, Transform parent) where T : Object => original;
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

    public sealed class AddComponentMenuAttribute : Attribute
    {
        public AddComponentMenuAttribute(string menuName)
        {
        }
    }

    public enum PrimitiveType
    {
        Cube,
        Sphere,
        Capsule,
        Cylinder,
        Plane,
        Quad
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
        RFloat,
        RGB24,
        RGBA32
    }

    public enum TextureWrapMode
    {
        Clamp,
        Repeat
    }

    public enum RenderTextureFormat
    {
        Default,
        RFloat,
        ARGB32
    }

    public enum RenderTextureReadWrite
    {
        Default,
        Linear,
        sRGB
    }

    public enum MeshTopology
    {
        Points,
        Lines,
        Triangles
    }

    public enum ComputeBufferType
    {
        Default,
        Structured
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
        public float magnitude => (float)Math.Sqrt(x * x + y * y);
        public Vector2 normalized => magnitude > 0 ? this * (1 / magnitude) : zero;

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
        public static Vector3 up => new Vector3(0, 1, 0);
        public static Vector3 down => new Vector3(0, -1, 0);
        public static Vector3 left => new Vector3(-1, 0, 0);
        public static Vector3 right => new Vector3(1, 0, 0);
        public static Vector3 forward => new Vector3(0, 0, 1);
        public static Vector3 back => new Vector3(0, 0, -1);

        public float sqrMagnitude => x * x + y * y + z * z;
        public float magnitude => (float)Math.Sqrt(sqrMagnitude);
        public Vector3 normalized => magnitude > 0 ? this / magnitude : zero;

        public static Vector3 Min(Vector3 left, Vector3 right) =>
            new Vector3(Math.Min(left.x, right.x), Math.Min(left.y, right.y), Math.Min(left.z, right.z));

        public static Vector3 Max(Vector3 left, Vector3 right) =>
            new Vector3(Math.Max(left.x, right.x), Math.Max(left.y, right.y), Math.Max(left.z, right.z));

        public static Vector3 Cross(Vector3 left, Vector3 right) =>
            new Vector3(left.y * right.z - left.z * right.y, left.z * right.x - left.x * right.z, left.x * right.y - left.y * right.x);

        public static float Dot(Vector3 left, Vector3 right) => left.x * right.x + left.y * right.y + left.z * right.z;
        public static Vector3 Scale(Vector3 left, Vector3 right) => new Vector3(left.x * right.x, left.y * right.y, left.z * right.z);
        public void Set(float newX, float newY, float newZ)
        {
            x = newX;
            y = newY;
            z = newZ;
        }

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

        public int this[int index]
        {
            get
            {
                return index switch
                {
                    0 => x,
                    1 => y,
                    2 => z,
                    _ => throw new IndexOutOfRangeException()
                };
            }
            set
            {
                switch (index)
                {
                    case 0:
                        x = value;
                        break;
                    case 1:
                        y = value;
                        break;
                    case 2:
                        z = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }

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

        public static Vector3Int RoundToInt(Vector3 value) =>
            new Vector3Int((int)Math.Round(value.x), (int)Math.Round(value.y), (int)Math.Round(value.z));

        public static Vector3Int operator +(Vector3Int left, Vector3Int right) =>
            new Vector3Int(left.x + right.x, left.y + right.y, left.z + right.z);

        public static Vector3Int operator -(Vector3Int left, Vector3Int right) =>
            new Vector3Int(left.x - right.x, left.y - right.y, left.z - right.z);

        public static bool operator ==(Vector3Int left, Vector3Int right) =>
            left.x == right.x && left.y == right.y && left.z == right.z;

        public static bool operator !=(Vector3Int left, Vector3Int right) => !(left == right);

        public override bool Equals(object obj) => obj is Vector3Int value && this == value;

        public override int GetHashCode() => HashCode.Combine(x, y, z);

        public override string ToString() => $"({x}, {y}, {z})";

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

        public static implicit operator Vector4(Color value) => new Vector4(value.r, value.g, value.b, value.a);
    }

    public struct Quaternion
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public static Quaternion identity => new Quaternion { w = 1 };
        public static Quaternion Inverse(Quaternion rotation) => rotation;
        public static Quaternion Euler(float x, float y, float z) => identity;
        public static Quaternion Euler(Vector3 euler) => identity;
        public static Quaternion LookRotation(Vector3 forward) => identity;
        public static Quaternion operator *(Quaternion left, Quaternion right) => left;
        public static Vector3 operator *(Quaternion rotation, Vector3 point) => point;
        public Vector3 eulerAngles => Vector3.zero;
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

    public struct Rect
    {
        public float x;
        public float y;
        public float width;
        public float height;

        public Rect(float x, float y, float width, float height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }
    }

    public class RectTransform : Transform
    {
        public Rect rect { get; set; }
        public Vector2 pivot { get; set; }
        public Vector2 sizeDelta { get; set; }
        public Vector2 offsetMin { get; set; }
        public Vector3 anchoredPosition3D { get; set; }
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
        public static Color red => new Color(1, 0, 0);
        public static Color green => new Color(0, 1, 0);
        public static Color yellow => new Color(1, 1, 0);
        public static Color cyan => new Color(0, 1, 1);
        public static Color blue => new Color(0, 0, 1);
        public static Color magenta => new Color(1, 0, 1);
        public static Color gray => new Color(0.5f, 0.5f, 0.5f);
        public static Color grey => gray;
        public static Color clear => new Color(0, 0, 0, 0);

        public static bool operator ==(Color left, Color right) =>
            left.r == right.r && left.g == right.g && left.b == right.b && left.a == right.a;

        public static bool operator !=(Color left, Color right) => !(left == right);

        public override bool Equals(object obj) => obj is Color value && this == value;

        public override int GetHashCode() => HashCode.Combine(r, g, b, a);
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
        public GameObject gameObject { get; } = new GameObject();
        public Transform transform => this;
        public Transform parent { get; set; }
        public Vector3 localPosition { get; set; }
        public Vector3 localEulerAngles { get; set; }
        public Vector3 position { get; set; }
        public Quaternion rotation { get; set; }
        public Quaternion localRotation { get; set; }
        public Vector3 localScale { get; set; } = Vector3.one;
        public Vector3 lossyScale { get; set; } = Vector3.one;
        public Matrix4x4 localToWorldMatrix { get; set; }
        public Vector3 forward { get; set; } = Vector3.forward;
        public int childCount { get; set; }

        public void Rotate(float xAngle, float yAngle, float zAngle, Space relativeTo = Space.Self) { }
        public void Rotate(Vector3 axis, float angle, Space relativeTo = Space.Self) { }
        public void RotateAround(Vector3 point, Vector3 axis, float angle) { }
        public void Translate(float x, float y, float z) { }
        public void SetParent(Transform parent, bool worldPositionStays = true) => this.parent = parent;
        public Transform Find(string name) => new Transform();
        public Transform GetChild(int index) => new Transform();
        public T GetComponent<T>() where T : new() => new T();
        public T GetComponentInChildren<T>() where T : new() => new T();
        public T GetComponentInChildren<T>(bool includeInactive) where T : new() => new T();
        public T GetComponentInParent<T>() where T : new() => new T();
        public Vector3 InverseTransformPoint(Vector3 position) => position;
        public Vector3 TransformPoint(Vector3 position) => position;
        public Vector3 TransformVector(Vector3 vector) => vector;
    }

    public class Texture : Object
    {
        public FilterMode filterMode { get; set; }
        public TextureWrapMode wrapMode { get; set; }
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

        public Texture2D(int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        public void LoadRawTextureData(IntPtr data, int size) { }
        public void LoadRawTextureData(byte[] data) { }
        public Unity.Collections.NativeArray<T> GetPixelData<T>(int mipLevel) where T : struct => new Unity.Collections.NativeArray<T>();
        public Unity.Collections.NativeArray<T> GetRawTextureData<T>() where T : struct => new Unity.Collections.NativeArray<T>();
        public void SetPixel(int x, int y, Color color) { }
        public void SetPixels(Color[] colors) { }
        public void ReadPixels(Rect source, int destX, int destY) { }
        public void Apply() { }
        public byte[] EncodeToPNG() => Array.Empty<byte>();
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

        public Unity.Collections.NativeArray<T> GetPixelData<T>(int mipLevel) where T : struct => new Unity.Collections.NativeArray<T>();
    }

    public class RenderTexture : Texture
    {
        public int width { get; }
        public int height { get; }
        public int depth { get; }
        public bool enableRandomWrite { get; set; }
        public bool useMipMap { get; set; }
        public static RenderTexture active { get; set; }

        public RenderTexture()
        {
        }

        public RenderTexture(int width, int height, int depth)
        {
            this.width = width;
            this.height = height;
            this.depth = depth;
        }

        public RenderTexture(int width, int height, int depth, RenderTextureFormat format, RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default)
        {
            this.width = width;
            this.height = height;
            this.depth = depth;
        }

        public void Create() { }
        public void Release() { }
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
        public static Camera[] allCameras => Array.Empty<Camera>();
        public RenderTexture targetTexture { get; set; }
        public DepthTextureMode depthTextureMode { get; set; }
    }

    public enum DepthTextureMode
    {
        None,
        Depth
    }

    public class ComputeBuffer : Object
    {
        public int count { get; }

        public ComputeBuffer(int count, int stride)
        {
            this.count = count;
        }

        public ComputeBuffer(int count, int stride, ComputeBufferType type)
        {
            this.count = count;
        }

        public void SetData<T>(IList<T> data) { }
        public void SetData<T>(T[] data) { }
        public void SetData<T>(IList<T> data, int managedBufferStartIndex, int computeBufferStartIndex, int count) { }
        public void GetData<T>(T[] data) { }
        public void Release() { }
    }

    public class ComputeShader : Object
    {
        public int FindKernel(string name) => 0;
        public void SetBuffer(int kernelIndex, string name, ComputeBuffer buffer) { }
        public void SetBuffer(int kernelIndex, int nameId, ComputeBuffer buffer) { }
        public void SetTexture(int kernelIndex, int nameId, Texture texture) { }
        public void SetTexture(int kernelIndex, string name, Texture texture) { }
        public void SetInt(string name, int value) { }
        public void SetInt(int nameId, int value) { }
        public void SetFloat(string name, float value) { }
        public void SetFloat(int nameId, float value) { }
        public void GetKernelThreadGroupSizes(int kernelIndex, out uint x, out uint y, out uint z)
        {
            x = y = z = 1;
        }

        public void Dispatch(int kernelIndex, int threadGroupsX, int threadGroupsY, int threadGroupsZ) { }
    }

    public class Material : Object
    {
        public Material()
        {
        }

        public Material(Shader shader)
        {
        }

        public static Material Instantiate(Material material) => material;
        public void SetTexture(int nameId, Texture value) { }
        public void SetTexture(string name, Texture value) { }
        public void SetInt(int nameId, int value) { }
        public void SetFloat(int nameId, float value) { }
        public void SetVector(int nameId, Vector3 value) { }
        public void SetVector(int nameId, Vector4 value) { }
        public void SetVectorArray(int nameId, Vector4[] values) { }
        public void SetColor(int nameId, Color value) { }
        public void SetColor(string name, Color value) { }
        public void EnableKeyword(string keyword) { }
        public void DisableKeyword(string keyword) { }
        public void SetMatrix(int nameId, Matrix4x4 value) { }
        public void SetMatrix(string name, Matrix4x4 value) { }
        public void SetBuffer(int nameId, ComputeBuffer value) { }
        public void SetBuffer(string name, ComputeBuffer value) { }
        public bool SetPass(int pass) => true;
    }

    public class MeshRenderer : Component
    {
        public Material material { get; set; }
    }

    public class Renderer : Component
    {
        public Material material { get; set; }
        public Material sharedMaterial { get; set; }
    }

    public class Collider : Component
    {
    }

    public class BoxCollider : Collider
    {
        public Vector3 size { get; set; }
        public Vector3 center { get; set; }
    }

    public class Sprite : Object
    {
        public Texture2D texture { get; set; } = new Texture2D(1, 1);
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot) => new Sprite();
    }

    public struct Ray
    {
        public Vector3 origin;
        public Vector3 direction;

        public Ray(Vector3 origin, Vector3 direction)
        {
            this.origin = origin;
            this.direction = direction;
        }
    }

    public struct RaycastHit
    {
        public Collider collider;
        public Transform transform;
        public Vector3 point;
        public float distance;
    }

    public static class Physics
    {
        public static bool Raycast(Ray ray, out RaycastHit hit)
        {
            hit = default;
            return false;
        }
    }

    public class Canvas : Behaviour
    {
    }

    public class Shader : Object
    {
        public static int PropertyToID(string name) => name.GetHashCode();
        public static Shader Find(string name) => new Shader();
        public static void WarmupAllShaders() { }
        public static void EnableKeyword(string keyword) { }
        public static void DisableKeyword(string keyword) { }
    }

    public static class Graphics
    {
        public static void CopyTexture(Texture src, int srcElement, int srcMip, int srcX, int srcY, int srcWidth, int srcHeight, Texture dst, int dstElement, int dstMip, int dstX, int dstY) { }
        public static void DrawProceduralNow(MeshTopology topology, int vertexCount) { }
    }

    public static class Resources
    {
        public static Object Load(string path) => null;
        public static T Load<T>(string path) where T : Object, new() => new T();
        public static T[] LoadAll<T>(string path) where T : Object => Array.Empty<T>();
    }

    public static class Mathf
    {
        public const float Rad2Deg = 57.29578f;
        public static float Floor(float value) => (float)Math.Floor(value);
        public static int FloorToInt(float value) => (int)Math.Floor(value);
        public static int CeilToInt(float value) => (int)Math.Ceiling(value);
        public static int RoundToInt(float value) => (int)Math.Round(value);
        public static int Clamp(int value, int min, int max) => Math.Min(Math.Max(value, min), max);
        public static float Clamp(float value, float min, float max) => Math.Min(Math.Max(value, min), max);
        public static float Sqrt(float value) => (float)Math.Sqrt(value);
        public static float Abs(float value) => Math.Abs(value);
        public static int Abs(int value) => Math.Abs(value);
        public static float Max(float left, float right) => Math.Max(left, right);
        public static int Max(int left, int right) => Math.Max(left, right);
        public static float Min(float left, float right) => Math.Min(left, right);
        public static int Min(int left, int right) => Math.Min(left, right);
        public static float Sign(float value) => Math.Sign(value);
        public static float Asin(float value) => (float)Math.Asin(value);
    }

    public static class Random
    {
        public static float Range(float minInclusive, float maxInclusive) => minInclusive;
    }

    public static class Time
    {
        public static float deltaTime => 0;
        public static float smoothDeltaTime => 0;
    }

    public static class Application
    {
        public static string dataPath => ".";
        public static bool isEditor => true;
        public static string version => "0.0";
        public static bool isFocused => true;
        public static void Quit() { }
    }

    public static class SystemInfo
    {
        public static int systemMemorySize => 8192;
    }

    public static class PlayerPrefs
    {
        public static string GetString(string key, string defaultValue = "") => defaultValue;
        public static int GetInt(string key, int defaultValue = 0) => defaultValue;
        public static void SetInt(string key, int value) { }
        public static void SetString(string key, string value) { }
        public static void Save() { }
    }

    public enum KeyCode
    {
        Tab,
        LeftShift,
        RightShift,
        RightArrow,
        LeftArrow,
        Space,
        C,
        P,
        R,
        X,
        Y,
        Z
    }

    public static class Input
    {
        public static bool GetKeyDown(KeyCode key) => false;
        public static bool GetKey(KeyCode key) => false;
        public static float GetAxis(string axisName) => 0;
    }

    public static class GUI
    {
        public static Color backgroundColor { get; set; }
        public static Rect Window(int id, Rect clientRect, Action<int> func, string text) => clientRect;
        public static void Label(Rect position, string text) { }
        public static bool Button(Rect position, string text) => false;
    }

    public static class Screen
    {
        public static int width => 1920;
        public static int height => 1080;
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
        public void RemoveAllListeners() { }
        public void Invoke() { }
    }

    public class UnityEvent<T0>
    {
        public void AddListener(Action<T0> call) { }
        public void RemoveListener(Action<T0> call) { }
        public void RemoveAllListeners() { }
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

namespace UnityEngine.EventSystems
{
    public class PointerEventData
    {
        public enum InputButton
        {
            Left,
            Right,
            Middle
        }

        public InputButton button { get; set; }
        public UnityEngine.Vector2 position { get; set; }

        public PointerEventData()
        {
        }

        public PointerEventData(EventSystem eventSystem)
        {
        }
    }

    public class BaseEventData
    {
    }

    public class EventSystem
    {
        public static EventSystem current { get; set; } = new EventSystem();
        public UnityEngine.GameObject currentSelectedGameObject { get; set; }
    }

    public class EventTrigger : UnityEngine.MonoBehaviour
    {
    }

    public interface IPointerEnterHandler
    {
        void OnPointerEnter(PointerEventData eventData);
    }

    public interface IPointerExitHandler
    {
        void OnPointerExit(PointerEventData eventData);
    }

    public interface IPointerDownHandler
    {
        void OnPointerDown(PointerEventData eventData);
    }

    public interface IPointerUpHandler
    {
        void OnPointerUp(PointerEventData eventData);
    }

    public interface IDragHandler
    {
        void OnDrag(PointerEventData eventData);
    }

    public interface ISelectHandler
    {
        void OnSelect(BaseEventData eventData);
    }

    public static class ExecuteEvents
    {
        public static object submitHandler { get; } = new object();
        public static void Execute(UnityEngine.GameObject target, PointerEventData eventData, object functor) { }
    }
}

namespace UnityEngine.UI
{
    public class Selectable : UnityEngine.Component
    {
        public bool interactable { get; set; }
    }

    public class Graphic : UnityEngine.Component
    {
        public UnityEngine.Color color { get; set; }
    }

    public class Text : Graphic
    {
        public string text { get; set; }
    }

    public class Image : Graphic
    {
        public UnityEngine.Sprite sprite { get; set; }
        public bool preserveAspect { get; set; }
    }

    public class RawImage : Graphic
    {
        public UnityEngine.Texture texture { get; set; }
        public UnityEngine.Rect uvRect { get; set; }
        public UnityEngine.RectTransform rectTransform { get; set; } = new UnityEngine.RectTransform();
    }

    public class Button : Selectable
    {
        public UnityEngine.Events.UnityEvent onClick { get; } = new UnityEngine.Events.UnityEvent();
    }

    public class Toggle : Selectable
    {
        public bool isOn { get; set; }
    }

    public class Scrollbar : Selectable
    {
        public float value { get; set; }
    }

    public class Slider : Selectable
    {
        public float value { get; set; }
        public float minValue { get; set; }
        public float maxValue { get; set; }
        public void SetValueWithoutNotify(float input) => value = input;
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

    public struct PhraseRecognizedEventArgs
    {
        public string text;
        public ConfidenceLevel confidence;
        public DateTime phraseStartTime;
        public TimeSpan phraseDuration;
    }

    public sealed class KeywordRecognizer : IDisposable
    {
        public KeywordRecognizer(string[] keywords, ConfidenceLevel minimumConfidence = ConfidenceLevel.Medium)
        {
        }

        public bool IsRunning { get; private set; }
        public event Action<PhraseRecognizedEventArgs> OnPhraseRecognized;
        public void Start() => IsRunning = true;
        public void Stop() => IsRunning = false;
        public void Dispose() { }
    }
}

namespace TMPro
{
    public class TMP_Text : UnityEngine.Component
    {
        public string text { get; set; }
        public UnityEngine.Color color { get; set; }
        public void SetText(string value) => text = value;
    }

    public class TextMeshProUGUI : TMP_Text
    {
    }

    public class TextMeshPro : TMP_Text
    {
    }

    public class TMP_InputField : UnityEngine.Component
    {
        public string text { get; set; }
        public bool interactable { get; set; }
        public UnityEngine.Events.UnityEvent<string> onEndEdit { get; } = new UnityEngine.Events.UnityEvent<string>();
        public void Select() { }
    }

    public class TMP_Dropdown : UnityEngine.Component
    {
        public sealed class OptionData
        {
            public string text { get; set; }

            public OptionData()
            {
            }

            public OptionData(string text)
            {
                this.text = text;
            }
        }

        public List<OptionData> options { get; } = new List<OptionData>();
        public int value { get; set; }
        public bool interactable { get; set; }
        public UnityEngine.Events.UnityEvent<int> onValueChanged { get; } = new UnityEngine.Events.UnityEvent<int>();
        public void ClearOptions() => options.Clear();
        public void AddOptions(List<string> optionNames)
        {
            foreach (var optionName in optionNames)
            {
                options.Add(new OptionData(optionName));
            }
        }

        public void RefreshShownValue() { }
    }
}

namespace Valve.VR
{
    public enum SteamVR_Input_Sources
    {
        Any,
        LeftHand,
        RightHand
    }

    public class SteamVR
    {
        public static bool active { get; set; }
        public static bool usingNativeSupport { get; set; }
        public static SteamVR instance { get; set; }
        public string hmd_ModelNumber { get; set; } = string.Empty;
        public CVRCompositor compositor { get; } = new CVRCompositor();
    }

    public static class OpenVR
    {
        public static void Shutdown() { }
    }

    public static class SteamVR_Input
    {
        public static T GetAction<T>(string actionName) where T : new() => new T();
    }

    public class SteamVR_Action_Boolean
    {
        public bool GetState(SteamVR_Input_Sources source) => false;
        public void AddOnChangeListener(Action<SteamVR_Action_Boolean, SteamVR_Input_Sources, bool> action, SteamVR_Input_Sources source) { }
        public void RemoveOnChangeListener(Action<SteamVR_Action_Boolean, SteamVR_Input_Sources, bool> action, SteamVR_Input_Sources source) { }
        public void AddOnStateDownListener(Action<SteamVR_Action_Boolean, SteamVR_Input_Sources> action, SteamVR_Input_Sources source) { }
        public void RemoveOnStateDownListener(Action<SteamVR_Action_Boolean, SteamVR_Input_Sources> action, SteamVR_Input_Sources source) { }
        public void AddOnStateUpListener(Action<SteamVR_Action_Boolean, SteamVR_Input_Sources> action, SteamVR_Input_Sources source) { }
        public void RemoveOnStateUpListener(Action<SteamVR_Action_Boolean, SteamVR_Input_Sources> action, SteamVR_Input_Sources source) { }
    }

    public class SteamVR_Action_Vibration
    {
        public void Execute(float startDelay, float duration, float frequency, float amplitude, SteamVR_Input_Sources source) { }
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

namespace Valve.VR.InteractionSystem
{
    public class Player : UnityEngine.MonoBehaviour
    {
        public Hand leftHand { get; set; } = new Hand();
        public Hand rightHand { get; set; } = new Hand();
    }

    public class Hand : UnityEngine.MonoBehaviour
    {
        public Valve.VR.SteamVR_Input_Sources handType { get; set; } = Valve.VR.SteamVR_Input_Sources.Any;
        public Valve.VR.SteamVR_Action_Boolean grabGripAction { get; set; } = new Valve.VR.SteamVR_Action_Boolean();
        public Valve.VR.SteamVR_Action_Boolean grabPinchAction { get; set; } = new Valve.VR.SteamVR_Action_Boolean();
        public Valve.VR.SteamVR_Action_Boolean uiInteractAction { get; set; } = new Valve.VR.SteamVR_Action_Boolean();
        public Valve.VR.SteamVR_Action_Vibration hapticAction { get; set; } = new Valve.VR.SteamVR_Action_Vibration();
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
        public static T DeserializeObject<T>(string value) => Activator.CreateInstance<T>();
        public static T DeserializeObject<T>(string value, JsonSerializerSettings settings) => Activator.CreateInstance<T>();
        public static string SerializeObject(object value, Formatting formatting) => string.Empty;
    }
}

namespace Valve.Newtonsoft.Json.Linq
{
    public class JObject
    {
        public static JObject FromObject(object value) => new JObject();
        public override string ToString() => "{}";
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
        public void SetColor(UnityEngine.Color color, int index) => Color = color;
    }

    public class PolyLine : LineShape
    {
        public List<UnityEngine.Vector3> Vertices { get; set; } = new List<UnityEngine.Vector3>();
    }
}
