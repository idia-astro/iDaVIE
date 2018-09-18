using UnityEngine;

namespace UI
{
    public class UserDraggableMenu : MonoBehaviour
    {
        private bool _isDragging;
        public bool CanThumbstickTranslate = true;
        public float TranslateSpeed = 300.0f;
        public bool CanThumbstickScale = true;
        public float ScaleSpeed = 0.5f;

        public void OnDragStarted(PointerController parent)
        {
            transform.SetParent(parent.transform, true);
            transform.localRotation = Quaternion.identity;
            transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
            _isDragging = true;
        }

        private void FixedUpdate()
        {
            if (_isDragging)
            {
                transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
                float verticalAxis = Input.GetAxis("Vertical");
                float horizontalAxis = Input.GetAxis("Horizontal");
                // Thumbstick translating back and forward using the vertical axis
                if (Mathf.Abs(verticalAxis) > Mathf.Max(Mathf.Abs(horizontalAxis), 0.5f) && CanThumbstickTranslate)
                {
                    float dt = Time.fixedDeltaTime;
                    float dx = verticalAxis * TranslateSpeed * dt;
                    float mag = transform.localPosition.magnitude;
                    // Prevent menu from moving through hand
                    if (Mathf.Sign(mag + dx) * Mathf.Sign(mag) > 0)
                    {
                        transform.localPosition = transform.localPosition.normalized * (mag + dx);
                    }
                }
                // Thumbstick scaling using the horizontal axis
                else if (Mathf.Abs(horizontalAxis) > 0.5f && CanThumbstickScale)
                {
                    float dt = Time.fixedDeltaTime;
                    float dx = horizontalAxis * ScaleSpeed * dt;
                    float mag = transform.localScale.magnitude;
                    // Prevent menu from inverting
                    if (Mathf.Sign(mag + dx) * Mathf.Sign(mag) > 0)
                    {
                        transform.localScale = transform.localScale.normalized * (mag + dx);
                    }
                }
            }
        }

        public void OnDragEnded()
        {
            transform.SetParent(null, true);
            _isDragging = false;
        }
    }
}