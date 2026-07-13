namespace iDaVIE.Persistence.Domain;

/// <summary>
/// Marks a DTO property as part of the persistence contract.
/// Properties without this attribute are ignored by <see cref="Infrastructure.SnapshotReflector"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PersistFieldAttribute : Attribute
{
    /// <summary>
    /// When true, a missing/null value is accepted and no warning is raised.
    /// </summary>
    public bool Optional { get; init; }
}
