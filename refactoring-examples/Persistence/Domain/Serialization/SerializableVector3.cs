namespace iDaVIE.Persistence.Domain.Serialization;

/// <summary>
/// Plain-C# replacement for UnityEngine.Vector3 for use in persistence DTOs.
/// </summary>
public sealed class SerializableVector3
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public SerializableVector3() { }

    public SerializableVector3(float x, float y, float z)
    {
        X = x; Y = y; Z = z;
    }

    public static SerializableVector3 One => new(1f, 1f, 1f);
    public static SerializableVector3 Zero => new(0f, 0f, 0f);

    public bool HasNonPositiveComponent() => X <= 0f || Y <= 0f || Z <= 0f;

    public override string ToString() => $"({X}, {Y}, {Z})";
}
