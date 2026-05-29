// SPDX-License-Identifier: LGPL-3.0-or-later
// CatalogDataSetRenderer — ST3 Unity ACL MonoBehaviour. Point-cloud catalogue
// rendering (M-18, IR-02). Replaces Assets/Scripts/CatalogData/CatalogDataSetRenderer.cs
// (694 LOC).
//
// Decomposition pattern mirrors FeatureVisualiser: one ComputeBuffer per loaded
// catalogue; vertex data driven by a ST2-owned CatalogDataSet aggregate. The
// catalogue data classes (CatalogDataSet, CatalogDataSetManager, ColumnInfo,
// DataMapping, CatalogInputController) stay with ST2 per global_model.md §1 ST2
// "IPAC catalogue".

using UnityEngine;

namespace iDaVIE.Rendering
{
    internal sealed class CatalogDataSetRenderer : MonoBehaviour
    {
        /// <summary>Mounts a catalogue for rendering — composition root injection
        /// of the ST2-owned catalogue aggregate (declared in iDaVIE.Data).</summary>
        public void Bind(object catalogueAggregate) => throw new System.NotImplementedException();

        private void OnRenderObject()    => throw new System.NotImplementedException();
        private void Update()            => throw new System.NotImplementedException();
        private void OnDestroy()         => throw new System.NotImplementedException();
    }
}
