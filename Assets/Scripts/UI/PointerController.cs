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

    private void OnEnable()
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
              
                UserSelectableItem selectableItem = _hoveredElement.GetComponent<UserSelectableItem>();
                if (selectableItem )
                {
                   
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