using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

namespace UI
{
    public class UserDraggableMenu : MonoBehaviour
    {
        public bool CanDPadTranslate = true;
        public float TranslateSpeed = 3.0f;
        public bool CanDPadScale = true;
        public float ScaleSpeed = 0.005f;
        public float MinScale = 1e-4f;
        public float MaxScale = 1e-2f;
        private bool _isDragging;
        private SteamVR_Input_Sources _handType;
        [SteamVR_DefaultAction("MenuUp")] public SteamVR_Action_Boolean menuUpAction;
        [SteamVR_DefaultAction("MenuDown")] public SteamVR_Action_Boolean menuDownAction;
        [SteamVR_DefaultAction("MenuLeft")] public SteamVR_Action_Boolean menuLeftAction;
        [SteamVR_DefaultAction("MenuRight")] public SteamVR_Action_Boolean menuRightAction;

        public void OnDragStarted(PointerController parent)
        {
            transform.SetParent(parent.transform, true);
            transform.localRotation = Quaternion.identity;
            transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
            _isDragging = true;
            _handType = parent.Hand.handType;
        }

        private void FixedUpdate()
        {
            if (_isDragging)
            {
                transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);

                // Thumbstick translating back and forward using the vertical axis
                if ((menuUpAction.GetState(_handType) || menuDownAction.GetState(_handType)) && CanDPadTranslate)
                {
                    float dt = Time.fixedDeltaTime;
                    float verticalAxis = (menuUpAction.GetState(_handType) ? 1 : -1);
                    float dx = verticalAxis * TranslateSpeed * dt;
                    float mag = transform.localPosition.magnitude;
                    // Prevent menu from moving through hand
                    if (Mathf.Sign(mag + dx) * Mathf.Sign(mag) > 0)
                    {
                        transform.localPosition = transform.localPosition.normalized * (mag + dx);
                    }
                }
                // Thumbstick scaling using the horizontal axis
                else if ((menuLeftAction.GetState(_handType) || menuRightAction.GetState(_handType)) && CanDPadScale)
                {
                    float dt = Time.fixedDeltaTime;
                    float horizontalAxis = (menuRightAction.GetState(_handType) ? 1 : -1);
                    float dx = horizontalAxis * ScaleSpeed * dt;
                    float mag = transform.localScale.magnitude;
                    float newMag = Mathf.Clamp(mag + dx, MinScale, MaxScale);
                    transform.localScale = transform.localScale.normalized * (newMag);
                }
            }
        }

        public void OnDragEnded()
        {
            var startPosition = transform.position;
            transform.SetParent(null, true);
            transform.position = startPosition;
            
            _isDragging = false;
        }
    }
}