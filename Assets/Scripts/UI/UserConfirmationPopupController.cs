using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;

public class UserConfirmationPopupController : MonoBehaviour
{
    public GameObject Popup;
    public PopUpButtonController[] buttons;
    public TextMeshProUGUI HeaderText;
    public TextMeshProUGUI MessageBody;
    public TextMeshProUGUI HoverText;

    private System.Action applyCallBack;
    private System.Action cancelCallback;


    // Start is called before the first frame update
    void Start()
    {
        Popup.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        return;
    }

    void setMessageBody(string body)
    {
        MessageBody.text = body;
    }

    void setHeaderText(string header)
    {
        HeaderText.text = header;
    }

    void setApplyHoverText(string applyHover)
    {

    }

    void setCancelHoverText(string cancelHover)
    {
        
    }
    
    void ApplyOnClick()
    {
        Popup.SetActive(false);
        applyCallBack();
    }

    void CancelOnClick()
    {
        Popup.SetActive(false);
        cancelCallback();
    }
}
