/*
 * Refactoring example 1: moment-map generation
 * Sub-team 5: Feature System and Domain Model
 *
 * MomentMapCalculator.cs (after state, new file, design-level example)
 *
 * The moment-map post-processing maths, in iDaVIE.Domain.Feature (ADR-008). It is
 * only stateless operations on float arrays (bounds extraction, moment-0,
 * moment-1): no orchestration, no I/O, no framework dependency. Maths like this
 * belongs in the domain layer as a shared utility that application services call.
 * In GRASP terms the domain layer is the expert on what moment maps mean, and the
 * application layer is the expert on when to compute them.
 *
 * The old MomentMapRenderer.GetBounds() did the min/max extraction by reading
 * pixels back from a RenderTexture into a Texture2D and walking the raw data, so
 * it was tied to Unity GPU types. This class does the same computation on plain
 * float[] arrays: in production the infrastructure adapter reads back from the GPU
 * and passes a float[] in; in tests you pass any float[] directly with no Unity
 * runtime.
 *
 * ComputeMoment0 and ComputeMoment1 are CPU versions of the algorithms the GPU
 * runs in MomentMapRendererAdapter. They give the tests a ground truth to compare
 * the GPU output against, and they let moment maps be generated in CI without a
 * GPU or a Unity process.
 */

using System;

namespace iDaVIE.Domain.Feature
{
    /// <summary>
    /// Moment-map calculation helpers, with no Unity or framework dependencies so
    /// they run in headless CI.
    /// </summary>
    public static class MomentMapCalculator
    {
        // Bounds extraction

        /// <summary>
        /// Returns the (min, max) finite value pair for the given pixel array,
        /// ignoring <see cref="float.NaN"/> entries.
        /// </summary>
        /// <param name="pixels">
        ///   Flat float array of moment-map pixel values. Must not be null or empty.
        /// </param>
        /// <returns>
        ///   (min, max) tuple. If all pixels are NaN, returns (0f, 0f).
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   Thrown when <paramref name="pixels"/> is null or empty.
        /// </exception>
        public static (float min, float max) ComputeMinMaxBounds(float[] pixels)
        {
            if (pixels == null || pixels.Length == 0)
                throw new ArgumentException("Pixel array must not be null or empty.", nameof(pixels));

            float min = float.MaxValue;
            float max = float.MinValue;

            foreach (float v in pixels)
            {
                if (float.IsNaN(v)) continue;
                if (v < min) min = v;
                if (v > max) max = v;
            }

            // If every pixel is NaN (e.g. masked-out region) return safe defaults.
            if (min > max) return (0f, 0f);

            return (min, max);
        }

        // CPU reference implementations, used as test ground truth

        /// <summary>
        /// CPU-side moment-0 (integrated intensity) computation.
        /// Each output pixel is the sum of voxel values along the spectral (Z) axis
        /// that exceed <paramref name="threshold"/>.
        /// </summary>
        /// <param name="dataVoxels">Flat voxel array ordered [z, y, x].</param>
        /// <param name="width">Cube width in pixels.</param>
        /// <param name="height">Cube height in pixels.</param>
        /// <param name="depth">Number of spectral channels.</param>
        /// <param name="threshold">Voxels at or below this value are excluded.</param>
        /// <returns>
        ///   Flat float array of length <c>width * height</c>, ordered [y, x].
        /// </returns>
        public static float[] ComputeMoment0(
            float[] dataVoxels,
            int width, int height, int depth,
            float threshold)
        {
            if (dataVoxels == null) throw new ArgumentNullException(nameof(dataVoxels));

            float[] moment0 = new float[width * height];

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width;  x++)
            {
                float sum = 0f;
                for (int z = 0; z < depth; z++)
                {
                    float v = dataVoxels[z * width * height + y * width + x];
                    if (!float.IsNaN(v) && v > threshold)
                        sum += v;
                }
                moment0[y * width + x] = sum;
            }

            return moment0;
        }

        /// <summary>
        /// CPU-side moment-1 (flux-weighted mean velocity) computation.
        /// Each output pixel is the flux-weighted mean of the <paramref name="spectrumZ"/>
        /// values along the Z axis, restricted to voxels above <paramref name="threshold"/>.
        /// Pixels with zero integrated flux are set to <see cref="float.NaN"/>.
        /// </summary>
        /// <param name="dataVoxels">Flat voxel array ordered [z, y, x].</param>
        /// <param name="spectrumZ">
        ///   Physical Z-axis values (velocity / frequency) per channel.
        ///   Length must equal <paramref name="depth"/>.
        /// </param>
        /// <param name="width">Cube width in pixels.</param>
        /// <param name="height">Cube height in pixels.</param>
        /// <param name="depth">Number of spectral channels.</param>
        /// <param name="threshold">Voxels at or below this value are excluded.</param>
        /// <returns>
        ///   Flat float array of length <c>width * height</c>, ordered [y, x].
        ///   NaN where integrated flux is zero.
        /// </returns>
        public static float[] ComputeMoment1(
            float[] dataVoxels,
            float[] spectrumZ,
            int width, int height, int depth,
            float threshold)
        {
            if (dataVoxels == null) throw new ArgumentNullException(nameof(dataVoxels));
            if (spectrumZ  == null) throw new ArgumentNullException(nameof(spectrumZ));
            if (spectrumZ.Length != depth)
                throw new ArgumentException(
                    $"spectrumZ.Length ({spectrumZ.Length}) must equal depth ({depth}).",
                    nameof(spectrumZ));

            float[] moment1 = new float[width * height];

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width;  x++)
            {
                float weightedSum = 0f;
                float totalFlux   = 0f;

                for (int z = 0; z < depth; z++)
                {
                    float v = dataVoxels[z * width * height + y * width + x];
                    if (!float.IsNaN(v) && v > threshold)
                    {
                        weightedSum += v * spectrumZ[z];
                        totalFlux   += v;
                    }
                }

                moment1[y * width + x] = totalFlux > 0f
                    ? weightedSum / totalFlux
                    : float.NaN;
            }

            return moment1;
        }
    }
}
