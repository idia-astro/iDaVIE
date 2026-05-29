// SPDX-License-Identifier: LGPL-3.0-or-later
// MomentMapRenderer — ST3 Unity ACL MonoBehaviour. Realises IMomentMapRenderer
// (M-08); wrapped by ST5's MomentMapServiceAdapter (refactored/Features/).
// Replaces Assets/Scripts/VolumeData/MomentMapRenderer.cs (386 LOC) — the
// non-MonoBehaviour boundary is the IMomentMapRenderer interface.

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using iDaVIE.Rendering.Contracts;

namespace iDaVIE.Rendering
{
    internal sealed class MomentMapRenderer : MonoBehaviour, IMomentMapRenderer
    {
        public bool IsRenderInProgress => throw new NotImplementedException();
        public event Action RenderProgressChanged;

        public Task<MomentMapResult> RenderMomentMap(MomentMapRequest request,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
