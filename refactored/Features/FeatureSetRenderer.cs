// SPDX-License-Identifier: LGPL-3.0-or-later
//
// FeatureSetRenderer — DELETED in the post-refactor layout.
//
// The legacy class (Assets/Scripts/FeatureData/FeatureSetRenderer.cs, 616 LOC,
// WMC 90 / CBO 26 / LCOM 91% — CRITICAL per T2 Baseline Report §3) carried at
// least six unrelated responsibilities. The split:
//
//   ┌──────────────────────────────────────┬──────────────────────────────────┐
//   │ Legacy responsibility                │ Post-refactor home               │
//   ├──────────────────────────────────────┼──────────────────────────────────┤
//   │ GPU ComputeBuffer + Draw             │ FeatureVisualiser.cs (Unity ACL) │
//   │ List<Feature> mutation, visibility,  │ FeatureSet.cs (internal sealed)  │
//   │ DisplayColour                        │   via IFeatureSetQuery surface   │
//   │ SpawnFeaturesFromTable (262 lines)   │ FeatureFactory.cs                │
//   │ SpawnFeaturesFromSourceStats         │ FeatureFactory.cs                │
//   │ SaveAsVoTable                        │ VoTableSaver.cs                  │
//   │ SelectFeature                        │ SelectionService.cs (ST5)        │
//   │ FeatureIsWithinVolume (predicate bug)│ Removed; IVolumeDataSet.Extents  │
//   │                                      │ + Feature.Center membership test │
//   └──────────────────────────────────────┴──────────────────────────────────┘
//
// Cross-team consumers (ST3 / ST4 / ST6 / ST7) bind to IFeatureSet,
// IFeatureSetQuery, and IFeatureSelectionService — not to the concrete renderer.
// This file is retained as a navigation breadcrumb only; it intentionally
// declares no type.
