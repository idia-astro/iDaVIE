namespace iDaVIE.Persistence.Domain.Serialization;

/// <summary>
/// Plain-C# replacement for UnityEngine.Quaternion for use in persistence DTOs.
/// </summary>
public sealed class SerializableQuaternion
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float W { get; set; }

    public SerializableQuaternion() { W = 1f; }

    public SerializableQuaternion(float x, float y, float z, float w)
    {
        X = x; Y = y; Z = z; W = w;
    }

    public static SerializableQuaternion Identity => new(0f, 0f, 0f, 1f);

    /// <summary>Returns true if the quaternion magnitude is not approximately 1.</summary>
    public bool IsNotNormalised()
    {
        float mag = MathF.Sqrt(X * X + Y * Y + Z * Z + W * W);
        return MathF.Abs(mag - 1f) > 0.001f;
    }

    /// <summary>Returns a normalised copy, or Identity if the quaternion is zero-length.</summary>
    public SerializableQuaternion Normalised()
    {
        float mag = MathF.Sqrt(X * X + Y * Y + Z * Z + W * W);
        if (mag < 1e-6f) return Identity;
        return new(X / mag, Y / mag, Z / mag, W / mag);
    }

    public override string ToString() => $"({X}, {Y}, {Z}, {W})";
}
