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

    public System.Action hidemenu; 
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
    /// This function sets the label on the button
    /// </summary>
    /// <param name="butText">The button label</param>
    public void setButtonText(string butText)
    {
        this.buttonText = butText;
        this.ButtonText.text = buttonText;
    }

    /// <summary>
    /// This function is called by the onSelect event for this button.
    /// </summary>
    public void setHoverText()
    {
        HoverText.text = this.hoverText;
    }

    /// <summary>
    /// This function is called by the onDeselect event for this button.
    /// </summary>
    public void emptyHoverText()
    {
        HoverText.text = "";
    }

    /// <summary>
    /// This function sets the TMPText object that is used
    /// to display the text when the user hovers over the button.
    /// </summary>
    /// <param name="tmpText">The TMPText field</param>
    public void setHoverObject(TextMeshProUGUI tmpText)
    {
        HoverText = tmpText;
    }

    /// <summary>
    /// This function is called when user clicks this button
    /// and invokes the callback attached to this button.
    /// </summary>
    public void invokeCallback()
    {
        UnityEngine.Debug.Log("Calling callback of button labelled " + buttonText + ".");
        callback();
        hidemenu();
    }
}
