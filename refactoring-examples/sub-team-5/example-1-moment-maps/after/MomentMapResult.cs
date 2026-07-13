/*
 * Refactoring example 1: moment-map generation
 * Sub-team 5: Feature System and Domain Model
 *
 * MomentMapResult.cs (after state, new file, design-level example)
 *
 * An immutable value object describing the output of
 * IMomentMapService.GenerateMomentMaps(), in iDaVIE.Application.Feature (ADR-008).
 *
 * The old MomentMapRenderer exposed its output as Unity RenderTexture properties
 * (Moment0Map, Moment1Map, ImageOutput) that only worked inside a running scene,
 * and kept bounds in private Vector2 fields with no accessor. This class packages
 * the same outputs as plain C# arrays and scalar bounds, so they can be read
 * without UnityEngine. The infrastructure adapter (MomentMapRendererAdapter) takes
 * the result and uploads it to GPU textures and UI elements, and a unit test can
 * assert on it with ordinary float-array comparisons.
 */

using System;

namespace iDaVIE.Application.Feature
{
    /// <summary>
    /// Immutable value object containing the computed moment-0 and moment-1
    /// pixel arrays and their display bounds.
    /// All data is plain C# arrays, with no Unity types.
    /// </summary>
    public sealed class MomentMapResult
    {
        /// <summary>
        /// Flat float array of moment-0 (integrated intensity) pixel values,
        /// ordered [y, x]. Length = <see cref="Width"/> × <see cref="Height"/>.
        /// </summary>
        public float[] Moment0Pixels { get; }

        /// <summary>
        /// Flat float array of moment-1 (flux-weighted mean velocity) pixel values,
        /// ordered [y, x]. Length = <see cref="Width"/> × <see cref="Height"/>.
        /// Pixels with zero integrated flux are <see cref="float.NaN"/>.
        /// </summary>
        public float[] Moment1Pixels { get; }

        /// <summary>Map width in pixels.</summary>
        public int Width { get; }

        /// <summary>Map height in pixels.</summary>
        public int Height { get; }

        /// <summary>Minimum finite value in <see cref="Moment0Pixels"/> (NaN excluded).</summary>
        public float Moment0Min { get; }

        /// <summary>Maximum finite value in <see cref="Moment0Pixels"/> (NaN excluded).</summary>
        public float Moment0Max { get; }

        /// <summary>Minimum finite value in <see cref="Moment1Pixels"/> (NaN excluded).</summary>
        public float Moment1Min { get; }

        /// <summary>Maximum finite value in <see cref="Moment1Pixels"/> (NaN excluded).</summary>
        public float Moment1Max { get; }

        /// <summary>
        /// Constructs a new moment-map result.
        /// </summary>
        public MomentMapResult(
            float[] moment0Pixels,
            float[] moment1Pixels,
            int     width,
            int     height,
            float   moment0Min,
            float   moment0Max,
            float   moment1Min,
            float   moment1Max)
        {
            Moment0Pixels = moment0Pixels ?? throw new ArgumentNullException(nameof(moment0Pixels));
            Moment1Pixels = moment1Pixels ?? throw new ArgumentNullException(nameof(moment1Pixels));
            if (width  <= 0) throw new ArgumentOutOfRangeException(nameof(width),  "Must be > 0.");
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Must be > 0.");

            Width      = width;
            Height     = height;
            Moment0Min = moment0Min;
            Moment0Max = moment0Max;
            Moment1Min = moment1Min;
            Moment1Max = moment1Max;
        }
    }
}
