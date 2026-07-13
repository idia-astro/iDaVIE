using Interaction.Interfaces;
using UnityEngine;

namespace Interaction
{
    public sealed class QuickMenuPositioner : IQuickMenuPositioner
    {
        private readonly GameObject _quickMenuCanvas;

        public QuickMenuPositioner(GameObject quickMenuCanvas)
        {
            _quickMenuCanvas = quickMenuCanvas;
        }

        public void Show(int handIndex, Transform handTransform)
        {
            if (_quickMenuCanvas == null || handTransform == null)
            {
                return;
            }

            _quickMenuCanvas.transform.SetParent(handTransform, false);
            _quickMenuCanvas.transform.localPosition = new Vector3(-0.1f, (handIndex == 0 ? 1 : -1) * 0.175f, 0.10f);
            _quickMenuCanvas.transform.localRotation = Quaternion.Euler((handIndex == 0 ? 1 : -1) * -3.25f, 15f, 90f);
            _quickMenuCanvas.transform.localScale = new Vector3(0.0005f, 0.0005f, 0.0005f);
            _quickMenuCanvas.SetActive(true);
        }

        public void Hide()
        {
            if (_quickMenuCanvas != null)
            {
                _quickMenuCanvas.SetActive(false);
            }
        }
    }
}
