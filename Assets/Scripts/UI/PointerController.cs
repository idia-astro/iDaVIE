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
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Valve.VR;
using Valve.VR.InteractionSystem;

[RequireComponent(typeof(LaserPointer))]
public class PointerController : MonoBehaviour
{
    private LaserPointer _laserPointer;
    public Hand Hand;
    private Button _hoveredElement;
    private UserDraggableMenu _draggingMenu;
    UserSelectableItem selectableItem = null;

    public void Start()
    {
        _laserPointer = GetComponent<LaserPointer>();
        Hand = GetComponentInParent<Hand>();

        _laserPointer.PointerIn -= OnPointerOverlayBegin;
        _laserPointer.PointerIn += OnPointerOverlayBegin;
        _laserPointer.PointerOut -= OnPointerOverlayEnd;
        _laserPointer.PointerOut += OnPointerOverlayEnd;
        Hand.uiInteractAction.AddOnChangeListener(OnUiInteractionChanged, Hand.handType);
    }

    private void OnUiInteractionChanged(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool newState)
    {
        // Mouse down
        if (newState)
        {
            if (_hoveredElement)
            {
                selectableItem = _hoveredElement.GetComponent<UserSelectableItem>();
                if (selectableItem )
                {
                    selectableItem.isPressed = true;
                    if (selectableItem.IsDragHandle && selectableItem.MenuRoot != null)
                    {
                        _draggingMenu = selectableItem.MenuRoot;
                        _draggingMenu.OnDragStarted(this);
                    }
                    else
                    {
                        // Process clicks
                    }
                }
            }

            if (EventSystem.current.currentSelectedGameObject != null)
            {
                ExecuteEvents.Execute(EventSystem.current.currentSelectedGameObject, new PointerEventData(EventSystem.current), ExecuteEvents.submitHandler);
            }
        }
        // Mouse up
        else
        {
            if (selectableItem)
            {
                selectableItem.isPressed = false;
                selectableItem = null;
            }
            if (_draggingMenu)
            {
                _draggingMenu.OnDragEnded();
                _draggingMenu = null;
            }

            if (_hoveredElement)
            {
                // Process mouse up
            }
        }
    }


    private void OnPointerOverlayBegin(object sender, PointerEventArgs e)
    {
        var overlappedButton = e.Target.GetComponent<Button>();
        if (overlappedButton != null)
        {
            overlappedButton.Select();
            _hoveredElement = overlappedButton;
        }
    }

    private void OnPointerOverlayEnd(object sender, PointerEventArgs e)
    {
        var overlappedButton = e.Target.GetComponent<Button>();
        if (overlappedButton != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            if (_hoveredElement == overlappedButton)
            {
                _hoveredElement = null;
            }
        }
    }
}