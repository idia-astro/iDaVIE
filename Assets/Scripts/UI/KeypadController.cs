﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Diagnostics;
using System.Linq;

public class KeypadController : MonoBehaviour
{


    public TextMeshProUGUI previewText;
    public TextMeshProUGUI targetText = null;
    public GameObject targetObj = null;

    private bool isNegative = false;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    /// <summary>
    /// This function runs when the user clicks the confirm button.
    /// It sends the input value to the target and closes the keypad.
    /// </summary>
    public void Confirm()
    {
        float value = float.Parse(previewText.text, System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
        targetText.text = previewText.text;
        UnityEngine.Debug.Log("Setting keypad value to " + previewText.text);
        previewText.text = "";
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Runs when the user presses one of the keypad buttons and adds the
    /// value of the key to the preview number.
    /// </summary>
    /// <param name="key_id">The id of the key that was pressed.</param>
    public void KeyPressed(int key_id)
    {

        string text = previewText.text;
        //Backspace
        if (key_id == -1)
            text = text.Remove(text.Length - 1);
        //Dot
        else if (key_id == -2)
            text = text + ".";
        //Toggle negative
        else if (key_id == -10)
        {
            if (this.isNegative)
            {
                text = text.Remove(0, 1);
                isNegative = false;
            }
            else
            {
                text = text.Insert(0, "-");
                isNegative = true;
            }
        }
        else
            text = text + key_id.ToString();
        
        previewText.text = text;
    }
}
