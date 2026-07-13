namespace iDaVIE.Persistence.Domain.Serialization;

/// <summary>
/// Plain-C# replacement for UnityEngine.Color for use in persistence DTOs.
/// </summary>
public sealed class SerializableColor
{
    public float R { get; set; }
    public float G { get; set; }
    public float B { get; set; }
    public float A { get; set; } = 1f;

    public SerializableColor() { }

    public SerializableColor(float r, float g, float b, float a = 1f)
    {
        R = r; G = g; B = b; A = a;
    }

    public static SerializableColor White => new(1f, 1f, 1f);
    public static SerializableColor Gray  => new(0.5f, 0.5f, 0.5f, 0.2f);

    public override string ToString() => $"rgba({R:F3}, {G:F3}, {B:F3}, {A:F3})";
}
