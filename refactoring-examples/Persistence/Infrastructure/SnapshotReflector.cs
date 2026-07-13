using System.Reflection;
using iDaVIE.Persistence.Domain;

namespace iDaVIE.Persistence.Infrastructure;

/// <summary>
/// Walks DTO types via reflection to collect all [PersistField]-tagged properties
/// and verify none of the required (non-Optional) ones are null.
/// Used for save-time auditing — not in the hot restore path.
/// </summary>
public static class SnapshotReflector
{
    /// <summary>
    /// Returns a list of required [PersistField] properties that are null on the given DTO object.
    /// </summary>
    public static IReadOnlyList<string> FindMissingRequired(object dto, string prefix = "")
    {
        var missing = new List<string>();
        if (dto is null) return missing;

        foreach (var prop in dto.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var attr = prop.GetCustomAttribute<PersistFieldAttribute>();
            if (attr is null) continue;

            string fullName = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
            object? value = prop.GetValue(dto);

            if (value is null && !attr.Optional)
            {
                missing.Add(fullName);
            }
            else if (value is not null && IsComplexType(prop.PropertyType))
            {
                // Recurse into nested DTOs
                missing.AddRange(FindMissingRequired(value, fullName));
            }
        }
        return missing;
    }

    private static bool IsComplexType(Type t)
        => t.IsClass && t != typeof(string) && !t.IsArray && !t.IsGenericType;
}
