// SPDX-License-Identifier: LGPL-3.0-or-later
// FeatureStatistics — value object held by Feature.Statistics for Mask features.
// Populated by FeatureFactory from ST2's SourceStats POCO.

using iDaVIE.Kernel.Contracts.Types;   // CartesianCoord

namespace iDaVIE.Features
{
    internal sealed class FeatureStatistics : IFeatureStatistics
    {
        public FeatureStatistics(long voxelCount, double totalFlux, double peakFlux,
                                 CartesianCoord fluxWeightedCentroid,
                                 double channelW20, double veloW20,
                                 double channelVsys, double veloVsys)
        {
            VoxelCount           = voxelCount;
            TotalFlux            = totalFlux;
            PeakFlux             = peakFlux;
            FluxWeightedCentroid = fluxWeightedCentroid;
            ChannelW20           = channelW20;
            VeloW20              = veloW20;
            ChannelVsys          = channelVsys;
            VeloVsys             = veloVsys;
        }

        public long           VoxelCount           { get; }
        public double         TotalFlux            { get; }
        public double         PeakFlux             { get; }
        public CartesianCoord FluxWeightedCentroid { get; }
        public double         ChannelW20           { get; }
        public double         VeloW20              { get; }
        public double         ChannelVsys          { get; }
        public double         VeloVsys             { get; }
    }
}
