namespace iDaVIE.Persistence.Domain;

/// <summary>
/// Describes which sub-systems are active in the current session.
/// Inferred at save time; drives WorkspaceStubFactory to produce the minimal stub.
/// </summary>
public enum WorkspaceProfile
{
    /// <summary>FITS cube loaded, no mask, no features.</summary>
    DataOnly,

    /// <summary>FITS cube + mask loaded.</summary>
    DataWithMask,

    /// <summary>FITS cube + feature sets (no mask rendering).</summary>
    DataWithFeatures,

    /// <summary>Full session: FITS cube, mask, features, foveation.</summary>
    FullWorkspace,
}
