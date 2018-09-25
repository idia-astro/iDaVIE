using UnityEngine;

namespace UI
{
    [RequireComponent(typeof(RectTransform))]
    public class UserSelectableItem : MonoBehaviour
    {
        private BoxCollider _boxCollider;
        private RectTransform _rectTransform;
        public bool IsDragHandle = false;
        public UserDraggableMenu MenuRoot;

        private void OnEnable()
        {
            ValidateCollider();
        }

        private void OnValidate()
        {
            ValidateCollider();
        }

        // TODO: This needs fixing for transforms with different offsets.
        private void ValidateCollider()
        {
            _rectTransform = GetComponent<RectTransform>();

            _boxCollider = GetComponent<BoxCollider>();
            if (_boxCollider == null)
            {
                _boxCollider = gameObject.AddComponent<BoxCollider>();
            }

            _boxCollider.size = _rectTransform.sizeDelta;
        }

        
    }
}