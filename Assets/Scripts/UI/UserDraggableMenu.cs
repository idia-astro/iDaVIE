/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 *
 * This file is part of the iDaVIE project.
 *
 * iDaVIE is free software: you can redistribute it and/or modify it under the terms 
 * of the GNU Lesser General Public License (LGPL) as published by the Free Software 
 * Foundation, either version 3 of the License, or (at your option) any later version.
 *
 * iDaVIE is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR 
 * PURPOSE. See the GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License along with 
 * iDaVIE in the LICENSE file. If not, see <https://www.gnu.org/licenses/>.
 *
 * Additional information and disclaimers regarding liability and third-party 
 * components can be found in the DISCLAIMER and NOTICE files included with this project.
 *
 */
using UnityEngine;
using Valve.VR;

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
        public SteamVR_Action_Boolean menuUpAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("MenuUp");
        public SteamVR_Action_Boolean menuDownAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("MenuDown");
        public SteamVR_Action_Boolean menuLeftAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("MenuLeft");
        public SteamVR_Action_Boolean menuRightAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("MenuRight");

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