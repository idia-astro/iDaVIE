// SPDX-License-Identifier: LGPL-3.0-or-later
// iDaVIE (immersive Data Visualisation Interactive Explorer)
// Copyright (C) 2024 IDIA, INAF-OACT — refactor skeleton, design-only.
//
// Read-only feature contracts exposed to ST3, ST4, ST6, ST7.
// Canonical signatures live in ST5_interface.md §3; this file is a worked
// skeleton evidencing the FeatureSetRenderer SRP split. Concrete classes
// (Feature / FeatureSet / FeatureStatistics) are internal sealed per DD-5.

using System.Collections.Generic;
using iDaVIE.Kernel.Contracts.Types;   // CartesianCoord, FeatureColour

namespace iDaVIE.Features
{
    /// <summary>Per-feature derived statistics; populated for Mask features, null otherwise.</summary>
    public interface IFeatureStatistics
    {
        long           VoxelCount           { get; }
        double         TotalFlux            { get; }
        double         PeakFlux             { get; }
        CartesianCoord FluxWeightedCentroid { get; }
        double         ChannelW20           { get; }
        double         VeloW20              { get; }
        double         ChannelVsys          { get; }
        double         VeloVsys             { get; }
    }

    /// <summary>Read-only view of a single feature. Mutation goes via IFeatureSetQuery.</summary>
    public interface IFeature
    {
        int                       OriginId       { get; }
        string                    Name           { get; }
        string                    Flag           { get; }
        CartesianCoord            Center         { get; }
        CartesianCoord            Size           { get; }
        bool                      IsSelected     { get; }
        IReadOnlyList<string>     RawDataValues  { get; }
        IFeatureStatistics?       Statistics     { get; }
    }

    /// <summary>Read-only view of a homogeneous feature collection. Mutation goes via IFeatureSetQuery.</summary>
    public interface IFeatureSet
    {
        int                       Index          { get; }
        string                    FileName       { get; }
        FeatureSetType            Type           { get; }
        FeatureColour             DisplayColour  { get; }
        bool                      IsVisible      { get; }
        IReadOnlyList<IFeature>   Features       { get; }
        IReadOnlyList<string>     RawDataKeys    { get; }
    }

    public enum FeatureSetType
    {
        Mask,
        Imported,
        UserDefined,
        SelectionBox
    }
}
