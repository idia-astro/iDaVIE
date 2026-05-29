// SPDX-License-Identifier: LGPL-3.0-or-later
// iDaVIE (immersive Data Visualisation Interactive Explorer)
// Copyright (C) 2024 IDIA, INAF-OACT — refactor skeleton, design-only.
//
// SelectionAnchorRenderer — Unity ACL MonoBehaviour realising ISelectionVisualiser.
//
// Extracts the 8-anchor cluster from the legacy FeatureSetManager:
//   _anchorColliders[8]      (FeatureSetManager.cs:60)        → _anchors[8]
//   Start() instantiation    (FeatureSetManager.cs:117-130)   → Awake
//   UpdateAnchors()          (FeatureSetManager.cs:147-166)   → ShowAt
//   HideAnchors()            (FeatureSetManager.cs:168-174)   → Hide
//
// The corner-handle scene rendering was previously interleaved with feature
// selection state on FeatureSetManager. Splitting it onto a dedicated
// MonoBehaviour lets Application-layer SelectionService depend on
// ISelectionVisualiser (pure interface) rather than on a MonoBehaviour reference
// (DD-5 — UnityEngine stays inside the ACL).
//
// Wired up via Unity Inspector serialisation; the composition root injects
// this MonoBehaviour into SelectionService as ISelectionVisualiser.
// No GameObject.Find — see ST5_refactoring_proposal.md "SelectionAnchorRenderer".

using System;
using UnityEngine;
using iDaVIE.Features;                  // IFeature, IFeatureSet
using iDaVIE.Kernel.Contracts.Types;    // CartesianCoord

namespace iDaVIE.Features
{
    internal sealed class SelectionAnchorRenderer : MonoBehaviour, ISelectionVisualiser
    {
        [SerializeField] private GameObject _anchorPrefab;

        // 8 corner handles, one per bounding-box vertex. Indexed by the (i,j,k)
        // ∈ {0,1}³ loop used in the legacy UpdateAnchors; flat index = i*4+j*2+k.
        private readonly GameObject[] _anchors = new GameObject[8];

        private void Awake()
        {
            if (_anchorPrefab == null)
            {
                Debug.LogError($"{nameof(SelectionAnchorRenderer)}: _anchorPrefab is not assigned in the inspector.");
                return;
            }
            for (var idx = 0; idx < 8; idx++)
            {
                _anchors[idx] = Instantiate(_anchorPrefab, Vector3.zero, Quaternion.identity, transform);
                var i = (idx >> 2) & 1; var j = (idx >> 1) & 1; var k = idx & 1;
                _anchors[idx].name = $"{(i == 0 ? "left" : "right")}_" +
                                     $"{(j == 0 ? "bottom" : "top")}_" +
                                     $"{(k == 0 ? "back" : "front")}";
            }
            Hide();
        }

        public void ShowAt(IFeature feature, IFeatureSet owningSet)
        {
            if (feature == null)   throw new ArgumentNullException(nameof(feature));
            if (owningSet == null) throw new ArgumentNullException(nameof(owningSet));

            // Verbatim port of legacy UpdateAnchors corner-derivation. The
            // ±Vector3.one * 0.5f voxel-centre offsets come from FeatureSetManager.cs:159-160.
            var cornerMin = ToVector3(BoundsMin(feature)) - Vector3.one * 0.5f;
            var cornerMax = ToVector3(BoundsMax(feature)) + Vector3.one * 0.5f;

            for (var idx = 0; idx < 8; idx++)
            {
                var anchor = _anchors[idx];
                if (anchor == null) continue;
                // Reparent so the anchors sit in the FeatureSet's transform frame.
                // The legacy code reparented to the FeatureSetRenderer; the visualiser
                // for owningSet now owns that transform, looked up via the binder. The
                // composition root sets this once at startup and never reassigns it.
                var weight   = new Vector3((idx >> 2) & 1, (idx >> 1) & 1, idx & 1);
                anchor.transform.localPosition = Vector3.Scale(cornerMax, weight) +
                                                 Vector3.Scale(cornerMin, Vector3.one - weight);
                SetGlobalScale(anchor.transform, Vector3.one * 0.01f);
            }
        }

        public void Hide()
        {
            for (var idx = 0; idx < 8; idx++)
                if (_anchors[idx] != null)
                    _anchors[idx].transform.localScale = Vector3.zero;
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        // Cartesian → Unity Vector3 conversion at the ACL boundary, mirroring
        // FeatureVisualiser. CartesianCoord is integer-voxel-space per DD-12
        // and ST5_interface.md §3.
        private static Vector3 ToVector3(CartesianCoord c) => new(c.X, c.Y, c.Z);

        private static CartesianCoord BoundsMin(IFeature f)
            => new(f.Center.X - f.Size.X / 2, f.Center.Y - f.Size.Y / 2, f.Center.Z - f.Size.Z / 2);

        private static CartesianCoord BoundsMax(IFeature f)
            => new(f.Center.X + f.Size.X / 2, f.Center.Y + f.Size.Y / 2, f.Center.Z + f.Size.Z / 2);

        // Verbatim port of the legacy SetGlobalScale helper used by UpdateAnchors —
        // sets local scale relative to current world scale of the parent.
        private static void SetGlobalScale(Transform t, Vector3 targetGlobalScale)
        {
            t.localScale = Vector3.one;
            var ls = t.lossyScale;
            t.localScale = new Vector3(
                targetGlobalScale.x / ls.x,
                targetGlobalScale.y / ls.y,
                targetGlobalScale.z / ls.z);
        }
    }
}
