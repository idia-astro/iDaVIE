// SPDX-License-Identifier: LGPL-3.0-or-later
// DesktopPaintController — refactored thin shell. Replaces the 1558 LOC class
// in Assets/Scripts/UI/DesktopPaintController.cs. Holds PaintTabViewModel +
// DesktopPaintRasteriser; wires Unity pointer events (IPointerDownHandler /
// IDragHandler / IPointerUpHandler) to ViewModel commands.
//
// Drawing the canvas overlay (zoom / pan, slice texture, polygon preview)
// stays here; the mask-mutation work and rasterisation are delegated.

using UnityEngine;
using UnityEngine.EventSystems;

namespace iDaVIE.UI
{
    internal sealed class DesktopPaintController : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private PaintTabViewModel        _viewModel;
        private DesktopPaintRasteriser   _rasteriser;

        public void Inject(PaintTabViewModel viewModel, DesktopPaintRasteriser rasteriser)
            => throw new System.NotImplementedException();

        public void OnPointerDown(PointerEventData eventData) => throw new System.NotImplementedException();
        public void OnDrag       (PointerEventData eventData) => throw new System.NotImplementedException();
        public void OnPointerUp  (PointerEventData eventData) => throw new System.NotImplementedException();
    }
}
