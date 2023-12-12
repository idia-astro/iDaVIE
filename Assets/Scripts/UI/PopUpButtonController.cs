using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;

public class PopUpButtonController : MonoBehaviour
{
    public Button thisButton;
    public TextMeshProUGUI ButtonText;
    public TextMeshProUGUI HoverText;

    public System.Action callback;
    public string buttonText {get; set;} = "Text displayed on the button";
    public string hoverText {get; set;} = "Text displayed when user hovers over button";

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        return;
    }

    /// <summary>
    /// This function must be called after everything has been set.
    /// </summary>
    void init()
    {
        // var trig = thisButton.GetComponent<EventTrigger>();
        EventTrigger.Entry onSelect = new EventTrigger.Entry();
        onSelect.eventID = EventTriggerType.Select;
        // onSelect.callback.AddListener(setHoverText(this.hoverText));
        
        EventTrigger.Entry onDeSelect = new EventTrigger.Entry();
        onDeSelect.eventID = EventTriggerType.Deselect;
        // onDeSelect.callback.AddListener(setHoverText(""));
        // trig.triggers.Add(onSelect);
        // trig.triggers.Add(onDeSelect);
    }

    void setHoverText(string text)
    {
        HoverText.text = text;
    }

    public void setHoverObject(TextMeshProUGUI tmpText)
    {
        HoverText = tmpText;
    }

    public void invokeCallback()
    {
        callback();
    }
}
