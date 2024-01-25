using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;
using System;
using System.Diagnostics;

public class UserConfirmationPopupController : MonoBehaviour
{
    public RectTransform messageRect;

    public RectTransform buttonRect;

    public GameObject buttonFrame;

    public GameObject buttonPrefab;
    public PopUpButtonController[] buttons;

    public TextMeshProUGUI HeaderText;
    public TextMeshProUGUI MessageBody;
    public TextMeshProUGUI HoverText;

    private System.Action applyCallBack;
    private System.Action cancelCallback;


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        return;
    }

    private void onEnable()
    {
        if (MessageBody.text == "")
        {
            messageRect.sizeDelta = new Vector2(messageRect.rect.width, 0f);
            buttonRect.sizeDelta = new Vector2(buttonRect.rect.width, 200f);
        }
    }

    private void OnDisable()
    {
        messageRect.sizeDelta = new Vector2(messageRect.rect.width, 100f);
        buttonRect.sizeDelta = new Vector2(buttonRect.rect.width, 100f);
    }

    /// <summary>
    /// This function sets the body of the popup
    /// </summary>
    /// <param name="body">Message to display</param>
    public void setMessageBody(string body)
    {
        MessageBody.text = body;
    }

    /// <summary>
    /// This function sets the header of the popup
    /// </summary>
    /// <param name="header">The popup label</param>
    public void setHeaderText(string header)
    {
        HeaderText.text = header;
    }

    /// <summary>
    /// This function is called to add a buttonto the popup
    /// </summary>
    /// <param name="buttonText">The label for the new button</param>
    /// <param name="hoverText">The text to show when hovering over the button</param>
    /// <param name="callback">The function to call when the user clicks on the button</param>
    public void addButton(string buttonText, string hoverText, System.Action callback)
    {
        var button = Instantiate(buttonPrefab, buttonFrame.transform);
        PopUpButtonController controller = button.GetComponent<PopUpButtonController>();
        controller.setButtonText(buttonText);
        controller.hoverText = hoverText;
        controller.HoverText = this.HoverText;
        controller.callback = callback;
        controller.hidemenu = this.buttonClicked;
        controller.init();
    }

    /// <summary>
    /// This function is called when the user clicks on any button to hide the popup.
    /// </summary>
    public void buttonClicked()
    {
        this.gameObject.SetActive(false);
        UnityEngine.Debug.Log("Popup menu has been hidden.");
    }
}
