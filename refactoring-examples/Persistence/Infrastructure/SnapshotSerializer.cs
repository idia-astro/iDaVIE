using System.Security.Cryptography;
using System.Text;
using iDaVIE.Persistence.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace iDaVIE.Persistence.Infrastructure;

/// <summary>
/// Serialises/deserialises WorkspaceSnapshot to/from JSON.
/// Uses Newtonsoft.Json (NuGet) with CamelCase property names and StringEnumConverter,
/// matching the envelope format in the plan spec.
///
/// Checksum protocol:
///   Serialize → compute SHA-256 over body (with "checksum":null placeholder) →
///   inject result into Metadata.Checksum → re-serialize final form.
///   On deserialize: recompute checksum over body-minus-checksum → compare → reject on mismatch.
/// </summary>
public class SnapshotSerializer
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        Formatting            = Formatting.Indented,
        NullValueHandling     = NullValueHandling.Ignore,
        Converters            = { new StringEnumConverter() },
        DateFormatHandling    = DateFormatHandling.IsoDateFormat,
        ContractResolver      = new CamelCasePropertyNamesContractResolver(),
    };

    public string Serialize(WorkspaceSnapshot snapshot)
    {
        // Two-pass: first with null checksum to get stable body, then inject hash.
        var noChecksum = snapshot with
        {
            Metadata = snapshot.Metadata with { Checksum = null }
        };
        string body    = JsonConvert.SerializeObject(noChecksum, Settings);
        string hash    = ComputeChecksum(body);
        var withHash   = snapshot with
        {
            Metadata = snapshot.Metadata with { Checksum = $"sha256:{hash}" }
        };
        return JsonConvert.SerializeObject(withHash, Settings);
    }

    public WorkspaceSnapshot Deserialize(string filePath)
    {
        string json = File.ReadAllText(filePath, Encoding.UTF8);

        var snapshot = JsonConvert.DeserializeObject<WorkspaceSnapshot>(json, Settings)
            ?? throw new InvalidDataException("Deserialised snapshot is null.");

        // Verify checksum
        string? storedChecksum = snapshot.Metadata.Checksum;
        var noChecksum = snapshot with
        {
            Metadata = snapshot.Metadata with { Checksum = null }
        };
        string body     = JsonConvert.SerializeObject(noChecksum, Settings);
        string expected = $"sha256:{ComputeChecksum(body)}";

        if (!string.Equals(storedChecksum, expected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Checksum mismatch in '{filePath}': stored={storedChecksum}, expected={expected}");

        return snapshot;
    }

    private static string ComputeChecksum(string body)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(body);
        byte[] hash  = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
