using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(SteamVR_LaserPointer))]
public class PointerController : MonoBehaviour
{
    private SteamVR_LaserPointer _laserPointer;
    private SteamVR_TrackedController _trackedController;
    private Button _button;
    private UserDraggableMenu _draggingMenu;

    private void OnEnable()
    {
        _laserPointer = GetComponent<SteamVR_LaserPointer>();
        _trackedController = GetComponentInParent<SteamVR_TrackedController>();

        _laserPointer.PointerIn -= OnPointerOverlayBegin;
        _laserPointer.PointerIn += OnPointerOverlayBegin;
        _laserPointer.PointerOut -= OnPointerOverlayEnd;
        _laserPointer.PointerOut += OnPointerOverlayEnd;
        _trackedController.TriggerClicked += OnPointerTriggerDown;
        _trackedController.TriggerUnclicked += OnPointerTriggerUp;
    }

    private void OnPointerTriggerDown(object sender, ClickedEventArgs e)
    {
        if (_button)
        {
            UserSelectableItem selectableItem = _button.GetComponent<UserSelectableItem>();
            if (selectableItem)
            {
                if (selectableItem.IsDragHandle && selectableItem.MenuRoot != null)
                {
                    _draggingMenu = selectableItem.MenuRoot;
                    _draggingMenu.OnDragStarted(this);
                    _laserPointer.pointer.SetActive(false);
                }
                else
                {
                    // Process clicks
                }
            }
        }
    }

    private void OnPointerTriggerUp(object sender, ClickedEventArgs e)
    {
        if (_draggingMenu)
        {
            _draggingMenu.OnDragEnded();
            _draggingMenu = null;
            _laserPointer.pointer.SetActive(true);
        }

        if (_button)
        {
            // Process mouse up
        }
    }

    private void OnPointerOverlayBegin(object sender, PointerEventArgs e)
    {
        var overlappedButton = e.target.GetComponent<Button>();
        if (overlappedButton != null)
        {
            overlappedButton.Select();
            _button = overlappedButton;
        }
    }

    private void OnPointerOverlayEnd(object sender, PointerEventArgs e)
    {
        var overlappedButton = e.target.GetComponent<Button>();
        if (overlappedButton != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            if (_button == overlappedButton)
            {
                _button = null;
            }
        }
    }

    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
    }
}