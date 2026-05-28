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

        /// <summary>Raises FeatureSetChanged after bulk-population is complete.
        /// Implements §6.3 step 5 — the second event that signals consumers to
        /// re-query the populated set. Called by FeatureImportService after
        /// factory.PopulateFromTable, and by FeatureSetService's Mask bootstrap
        /// after factory.PopulateFromSourceStats.</summary>
        void NotifySetPopulated();

        /// <summary>Records the source-file path and column mapping that produced an
        /// Imported set so IFeatureStateCapture.Capture can store provenance rather
        /// than inlining the features. Called by FeatureImportService after CreateSet
        /// on the Imported flow.</summary>
        void RecordImportProvenance(IFeatureSet set, string filePath, FeatureImportMapping mapping);
    }

    /// <summary>Setter-injected hook that lets FeatureSetService.Restore replay
    /// Imported-set entries through FeatureImportService without forming a
    /// FeatureSetService ↔ FeatureImportService construction cycle. The composition
    /// root wires this after both services are built.</summary>
    internal interface IImportRestorer
    {
        void RestoreImported(string filePath, FeatureImportMapping mapping,
                             FeatureColour displayColour, bool isVisible);
    }

    /// <summary>Populates a caller-owned FeatureSet. The factory does not create
    /// the set — the caller is responsible for set creation and event ordering
    /// (per ST5_domain_design.md §6.3 construction sequence).</summary>
    internal interface IFeatureFactory
    {
        void PopulateFromTable      (FeatureSet target, FeatureTable table, FeatureImportMapping mapping);
        void PopulateFromSourceStats(FeatureSet target);
    }

    /// <summary>Exposes which FeatureSetType the user is currently working with.
    /// Realised by FeatureMenuController (ST5-owned per brief §6.5 "source-list
    /// statistics"). Both producer and consumer (SelectionService) are ST5-internal,
    /// so this interface is not part of the cross-team contract.
    ///
    /// SelectionService uses ActiveType to prioritise the active type's sets during
    /// spatial search; null means no source-list panel is open, so it scans every
    /// loaded set in FeatureSetType-declaration order.</summary>
    internal interface IActiveFeatureSetTypeProvider
    {
        FeatureSetType? ActiveType { get; }
    }

    /// <summary>Scene-side anchor renderer for bounding-box corner handles.
    /// Extracts the 8 _anchorColliders managed by the legacy FeatureSetManager into
    /// a dedicated Unity ACL MonoBehaviour (SelectionAnchorRenderer). Application-
    /// layer SelectionService depends on this interface, not on the MonoBehaviour.</summary>
    internal interface ISelectionVisualiser
    {
        void ShowAt(IFeature feature, IFeatureSet owningSet);
        void Hide();
    }

    /// <summary>Composition-root bridge so domain code can request a Unity
    /// MonoBehaviour visualiser without referencing UnityEngine. Realised in
    /// the Unity ACL by a class that instantiates FeatureVisualiser GameObjects.
    ///
    /// Two-step wiring (§6.3 construction sequence in ST5_domain_design.md):
    ///   1. CreateListener() — instantiates a FeatureVisualiser with no set yet;
    ///      dirty events received before AttachToSet are buffered internally.
    ///   2. The returned listener is passed to IFeatureSetCatalog.CreateSet so the
    ///      FeatureSet is constructed with its listener already in place.
    ///   3. AttachToSet(listener, set) — hands the set reference to the visualiser
    ///      so it can drain its buffered dirty queue and begin uploading vertices.</summary>
    internal interface IVisualiserBinder
    {
        /// <summary>Instantiates a FeatureVisualiser MonoBehaviour with no set reference.
        /// Call before IFeatureSetCatalog.CreateSet so the listener can be passed to
        /// the FeatureSet constructor.</summary>
        IFeatureDirtyListener CreateListener();

        /// <summary>Hands the set reference to a previously-created listener.
        /// After this call the visualiser drains its buffered dirty queue.</summary>
        void AttachToSet(IFeatureDirtyListener listener, IFeatureSet set);
    }
}
