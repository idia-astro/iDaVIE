// SPDX-License-Identifier: LGPL-3.0-or-later
// FeatureVisualiser — Unity ACL MonoBehaviour. Replaces the GPU-rendering half
// of FeatureSetRenderer (Awake / Update / OnRenderObject / OnDestroy +
// _computeBufferVertices / _vertices / _dirtyFeatures / MakeAxisAlignedCube).
//
// ONE FeatureVisualiser per FeatureSet, bound by IVisualiserBinder at the
// composition root. This is the ONLY class in iDaVIE.Features permitted to
// touch UnityEngine — per ST5_domain_design.md §7 ("Unity ACL").
//
// Refactor delta:
//   - No FeatureList, no SpawnFeaturesFrom*, no SaveAsVoTable, no
//     FeatureIsWithinVolume. The visualiser observes its FeatureSet via the
//     listener port; everything else moved elsewhere.
//   - CartesianCoord → Vector3 conversion happens here and only here.

using System.Collections.Generic;
using UnityEngine;
using iDaVIE.Kernel.Contracts.Types;     // CartesianCoord, FeatureColour

namespace iDaVIE.Features
{
    internal sealed class FeatureVisualiser : MonoBehaviour, IFeatureDirtyListener
    {
        // Constants — copy of FeatureSetRenderer.cs lines 79-82.
        private const int VerticesPerFeature   = 24;
        private const int BytesPerVertex       = 32;
        private const int DefaultFeatureCapacity = 16384;

        // Set by the composition-root IVisualiserBinder before Awake completes.
        internal IFeatureSet Target { get; set; }

        [SerializeField] private Material lineRenderingMaterial;

        private ComputeBuffer _buffer;
        private FeatureVertex[] _vertices;
        private readonly Queue<int> _dirtyQueue = new();
        private bool _allDirty;
        private Material _materialInstance;

        // ── IFeatureDirtyListener ───────────────────────────────────────────

        public void OnFeatureDirty(int originId)
        {
            if (originId < 0) _allDirty = true;
            else              _dirtyQueue.Enqueue(originId);
        }

        // ── Unity lifecycle ─────────────────────────────────────────────────

        private void Awake()
        {
            _buffer           = new ComputeBuffer(DefaultFeatureCapacity * VerticesPerFeature,
                                                  BytesPerVertex, ComputeBufferType.Structured);
            _vertices         = new FeatureVertex[DefaultFeatureCapacity * VerticesPerFeature];
            _materialInstance = Instantiate(lineRenderingMaterial);
        }

        private void Update()
        {
            if (!_allDirty && _dirtyQueue.Count == 0) return;

            EnsureCapacity(Target.Features.Count);

            if (_allDirty)
            {
                for (var i = 0; i < Target.Features.Count; i++)
                    WriteCube(i, Target.Features[i]);
                _allDirty = false;
                _dirtyQueue.Clear();
            }
            else
            {
                // OriginId → buffer-index lookup is owned by FeatureSet (it maintains the
                // dictionary as a side-effect of Add/Remove). We downcast through the
                // internal FeatureSet type — the visualiser sits inside iDaVIE.Features
                // and is the only place IFeatureSet is converted back to FeatureSet.
                var owningSet = (FeatureSet)Target;
                while (_dirtyQueue.Count > 0)
                {
                    var originId = _dirtyQueue.Dequeue();
                    var index    = owningSet.IndexOf(originId);
                    if (index >= 0) WriteCube(index, Target.Features[index]);
                }
            }

            _buffer.SetData(_vertices);
        }

        private void OnRenderObject()
        {
            if (!Target.IsVisible) return;
            _materialInstance.SetMatrix("datasetMatrix", transform.localToWorldMatrix);
            _materialInstance.SetBuffer("inputData", _buffer);
            _materialInstance.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Lines, Target.Features.Count * VerticesPerFeature);
        }

        private void OnDestroy() => _buffer?.Release();

        // ── Helpers ─────────────────────────────────────────────────────────

        private void EnsureCapacity(int requiredFeatureCount)
        {
            var currentCapacity = _buffer.count / VerticesPerFeature;
            if (requiredFeatureCount <= currentCapacity) return;

            _vertices = new FeatureVertex[requiredFeatureCount * VerticesPerFeature];
            _buffer.Release();
            _buffer = new ComputeBuffer(requiredFeatureCount * VerticesPerFeature,
                                        BytesPerVertex, ComputeBufferType.Structured);
        }

        private void WriteCube(int index, IFeature feature)
        {
            // Single Unity-conversion site: CartesianCoord → Vector3 / FeatureColour → Color.
            var center = ToVector3(feature.Center);
            var size   = ToVector3(feature.Size);
            var colour = ToColor(Target.DisplayColour);
            var visibility = feature.IsSelected
                ? FeatureVisibility.Selected
                : (Target.IsVisible ? FeatureVisibility.Visible : FeatureVisibility.Hidden);
            MakeAxisAlignedCube(center, size, colour, visibility,
                                index * VerticesPerFeature, _vertices);
        }

        private static Vector3 ToVector3(CartesianCoord c) => new(c.X, c.Y, c.Z);
        private static Color   ToColor  (FeatureColour c)  => new(c.R, c.G, c.B, c.A);

        // Verbatim from legacy FeatureSetRenderer.MakeAxisAlignedCube (lines 571-610).
        // 24 vertices = 12 edges × 2 endpoints, drawn as Lines via DrawProceduralNow.
        // Pure GPU-vertex construction; no domain logic, intentionally not refactored.
        private static void MakeAxisAlignedCube(Vector3 position, Vector3 size, Color color,
                                                FeatureVisibility visibility, int offset,
                                                FeatureVertex[] list)
        {
            if (offset + VerticesPerFeature > list.Length) return;
            size *= 0.5f;

            list[offset +  0] = new FeatureVertex { Position = position + new Vector3(-size.x,  size.y, -size.z) };
            list[offset +  1] = new FeatureVertex { Position = position + new Vector3( size.x,  size.y, -size.z) };
            list[offset +  2] = new FeatureVertex { Position = position + new Vector3( size.x,  size.y, -size.z) };
            list[offset +  3] = new FeatureVertex { Position = position + new Vector3( size.x,  size.y,  size.z) };
            list[offset +  4] = new FeatureVertex { Position = position + new Vector3( size.x,  size.y,  size.z) };
            list[offset +  5] = new FeatureVertex { Position = position + new Vector3(-size.x,  size.y,  size.z) };
            list[offset +  6] = new FeatureVertex { Position = position + new Vector3(-size.x,  size.y,  size.z) };
            list[offset +  7] = new FeatureVertex { Position = position + new Vector3(-size.x,  size.y, -size.z) };
            list[offset +  8] = new FeatureVertex { Position = position + new Vector3(-size.x, -size.y, -size.z) };
            list[offset +  9] = new FeatureVertex { Position = position + new Vector3(-size.x,  size.y, -size.z) };
            list[offset + 10] = new FeatureVertex { Position = position + new Vector3( size.x, -size.y, -size.z) };
            list[offset + 11] = new FeatureVertex { Position = position + new Vector3( size.x,  size.y, -size.z) };
            list[offset + 12] = new FeatureVertex { Position = position + new Vector3(-size.x, -size.y,  size.z) };
            list[offset + 13] = new FeatureVertex { Position = position + new Vector3(-size.x,  size.y,  size.z) };
            list[offset + 14] = new FeatureVertex { Position = position + new Vector3( size.x, -size.y,  size.z) };
            list[offset + 15] = new FeatureVertex { Position = position + new Vector3( size.x,  size.y,  size.z) };
            list[offset + 16] = new FeatureVertex { Position = position + new Vector3(-size.x, -size.y, -size.z) };
            list[offset + 17] = new FeatureVertex { Position = position + new Vector3( size.x, -size.y, -size.z) };
            list[offset + 18] = new FeatureVertex { Position = position + new Vector3( size.x, -size.y, -size.z) };
            list[offset + 19] = new FeatureVertex { Position = position + new Vector3( size.x, -size.y,  size.z) };
            list[offset + 20] = new FeatureVertex { Position = position + new Vector3( size.x, -size.y,  size.z) };
            list[offset + 21] = new FeatureVertex { Position = position + new Vector3(-size.x, -size.y,  size.z) };
            list[offset + 22] = new FeatureVertex { Position = position + new Vector3(-size.x, -size.y,  size.z) };
            list[offset + 23] = new FeatureVertex { Position = position + new Vector3(-size.x, -size.y, -size.z) };

            for (var i = 0; i < VerticesPerFeature; i++)
            {
                list[offset + i].Color      = color;
                list[offset + i].Visibility = visibility;
            }
        }

        private enum FeatureVisibility { Hidden, Visible, Selected }
        private struct FeatureVertex { public Vector3 Position; public Vector4 Color; public FeatureVisibility Visibility; }
    }
}
