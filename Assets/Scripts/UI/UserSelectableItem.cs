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
        public bool isPressed;


        private void Start()
        {
            if (MenuRoot == null)
            {
                MenuRoot = GetComponentInParent<UserDraggableMenu>();
            }

            _rectTransform = GetComponent<RectTransform>();
            _boxCollider = GetComponent<BoxCollider>();
            if (_boxCollider == null)
            {
                _boxCollider = gameObject.AddComponent<BoxCollider>();
            }

            var rect = _rectTransform.rect;
            Vector2 pivot = _rectTransform.pivot;
            _boxCollider.size = new Vector3(rect.width, rect.height, 1f);
            _boxCollider.center = new Vector3(rect.width / 2 - rect.width * pivot.x, rect.height / 2 - rect.height * pivot.y, _rectTransform.anchoredPosition3D.z);
        }
    }
}