using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonHoverBehaviour : MonoBehaviour
{

    public Image ButtonImage;
    
    
    public void OnHoverEnter()
    {
        ButtonImage.color = Color.cyan;
    }

    public void OnHoverExit()
    {
        ButtonImage.color = Color.white;
    }
    
}
