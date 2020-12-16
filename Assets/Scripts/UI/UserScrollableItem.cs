using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UserScrollableItem : EventTrigger
    {
      
        private BoxCollider _boxCollider;
        private RectTransform _rectTransform;
        private VolumeInputController _volumeInputController;



        private void Start()
        {

        _volumeInputController = FindObjectOfType<VolumeInputController>();
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
            gameObject.AddComponent<Button>();

    }
      
        public override void OnDeselect(BaseEventData data)
        {
            _volumeInputController.scrollSelected = false;
            _volumeInputController.ScrollObject = null;
            Debug.Log("OnDeselect called.");
        }

      


        public override void OnSelect(BaseEventData data)
        {
            _volumeInputController.scrollSelected = true;
            _volumeInputController.ScrollObject = gameObject;
            Debug.Log("OnSelect called.");
        }


}
