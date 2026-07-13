/*
 * Refactoring example 2: VOTable export
 * Sub-team 5: Feature System and Domain Model
 *
 * IAstFrame.cs (after state, new file, design-level example)
 *
 * A domain-level handle that stands in for the native AST frame pointer
 * (ADR-002, anti-corruption layer).
 *
 * Previously ICoordinateTransformer.Transform() and .Normalise() both took an
 * IntPtr as their first parameter, the raw P/Invoke handle to the native
 * DataAnalysis.dll AST frame. IntPtr is an unmanaged-memory pointer, so exposing
 * it in a domain interface pulled the native-plugin boundary type into domain
 * code, forced unit tests to fake an unsafe pointer, and broke the ACL rule of no
 * native types above infrastructure.
 *
 * IAstFrame replaces it with an opaque marker interface that domain and
 * application code can hold without knowing anything about unmanaged memory. The
 * interface is defined here in iDaVIE.Domain.Feature; the concrete AstFrameHandle
 * (which holds the IntPtr) lives in iDaVIE.Infrastructure.NativePlugins. Tests
 * pass a stub: public sealed class NullAstFrame : IAstFrame { }.
 */

// Documentation copy, not compiled as part of the Unity project.
// The authoritative production file is Assets/Scripts/FeatureData/IAstFrame.cs.
// This copy only exists to make example 2 self-contained for readers.
// If the production IAstFrame changes, update this file to match.

namespace iDaVIE.Domain.Feature
{
    /// <summary>
    /// Opaque domain handle for an AST World Coordinate System frame.
    /// <para>
    /// Replaces the raw <c>IntPtr</c> that previously leaked the
    /// <c>DataAnalysis.dll</c> P/Invoke boundary into domain-layer interfaces
    /// (an ADR-002 anti-corruption-layer violation).
    /// </para>
    /// <para>
    /// This is an intentionally empty marker interface. Domain and application
    /// code holds an <see cref="IAstFrame"/> reference and passes it to
    /// <see cref="ICoordinateTransformer"/>, and neither layer ever sees an
    /// <c>IntPtr</c> or unsafe code.
    /// </para>
    /// <para>
    /// The production implementation (<c>AstFrameHandle</c>) lives in
    /// <c>iDaVIE.Infrastructure.NativePlugins</c> and wraps the real
    /// <c>IntPtr</c> from <c>VolumeDataSetRenderer.AstFrame</c>.
    /// </para>
    /// </summary>
    public interface IAstFrame
    {
        // Intentionally empty: this is the opaque handle pattern.
        // Concrete types own the IntPtr; the domain layer never touches it.
    }
}
