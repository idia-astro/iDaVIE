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
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// This class is used to control the behaviour of a popup menu, with one or more buttons and usually used as confirmation.
/// </summary>
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

    /// <summary>
    /// Called when the popup is enabled.
    /// </summary>
    private void OnEnable()
    {
        if (MessageBody.text == "")
        {
            messageRect.sizeDelta = new Vector2(messageRect.rect.width, 0f);
            buttonRect.sizeDelta = new Vector2(buttonRect.rect.width, 200f);
        }
    }

    /// <summary>
    /// Called when the popup is disabled.
    /// </summary>
    private void OnDisable()
    {
        messageRect.sizeDelta = new Vector2(messageRect.rect.width, 100f);
        buttonRect.sizeDelta = new Vector2(buttonRect.rect.width, 100f);
    }

    /// <summary>
    /// This function sets the body of the popup.
    /// </summary>
    /// <param name="body">Message to display.</param>
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
    /// This function is called to add a button to the popup.
    /// </summary>
    /// <param name="buttonText">The label for the new button.</param>
    /// <param name="hoverText">The text to show when hovering over the button.</param>
    /// <param name="callback">The function to call when the user clicks on the button.</param>
    public void addButton(string buttonText, string hoverText, System.Action callback)
    {
        var button = Instantiate(buttonPrefab, buttonFrame.transform);
        PopUpButtonController controller = button.GetComponent<PopUpButtonController>();
        controller.setButtonText(buttonText);
        controller.hoverText = hoverText;
        controller.HoverText = this.HoverText;
        controller.callback = callback;
        controller.hidemenu = this.buttonClicked();
        controller.init();
    }

    /// <summary>
    /// This function is called when the user clicks on any button to hide the popup.
    /// </summary>
    public IEnumerator buttonClicked()
    {
        this.gameObject.SetActive(false);
        Debug.Log("Popup menu has been hidden.");
        yield return null;
    }
}
