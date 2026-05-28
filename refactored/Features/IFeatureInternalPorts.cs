// SPDX-License-Identifier: LGPL-3.0-or-later
// ST5-INTERNAL PORTS — not part of the cross-team contract.
// These types are 'internal' inside the iDaVIE.Features assembly per DD-5.
// Cross-team consumers cannot name them and must not take a dependency on them.

using System.Collections.Generic;
using iDaVIE.Kernel.Contracts.Types;   // CartesianCoord, FeatureColour

namespace iDaVIE.Features
{
    /// <summary>Feature → GPU layer notification. Realised by FeatureVisualiser.
    /// One IFeatureDirtyListener per FeatureSet; shared across all Features in
    /// that set (held by FeatureSet.Listener), so FeatureSetService can attach
    /// new Mask features without a side-channel lookup.</summary>
    internal interface IFeatureDirtyListener
    {
        void OnFeatureDirty(int originId);
    }

    /// <summary>Factory-style creation port held by FeatureImportService /
    /// FeatureSetService. Boundary consumers receive IFeatureSetQuery only.</summary>
    internal interface IFeatureSetCatalog
    {
        FeatureSet CreateSet(string fileName, FeatureSetType type,
                             IReadOnlyList<string> rawDataKeys,
                             FeatureColour colour,
                             IFeatureDirtyListener listener);
    }

    /// <summary>Populates a caller-owned FeatureSet. The factory does not create
    /// the set — the caller is responsible for set creation and event ordering
    /// (per ST5_domain_design.md §6.3 construction sequence).</summary>
    internal interface IFeatureFactory
    {
        void PopulateFromTable      (FeatureSet target, FeatureTable table, FeatureImportMapping mapping);
        void PopulateFromSourceStats(FeatureSet target);
    }

    /// <summary>Composition-root bridge so domain code can request a Unity
    /// MonoBehaviour visualiser without referencing UnityEngine. Realised in
    /// the Unity ACL by a class that news a FeatureVisualiser GameObject and
    /// returns its IFeatureDirtyListener handle.</summary>
    internal interface IVisualiserBinder
    {
        IFeatureDirtyListener BindNew(FeatureSet set);
        void Unbind(FeatureSet set);
    }
}
