using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Diagnostics;

public class KeypadController : MonoBehaviour
{


    public TextMeshProUGUI previewText;
    public TextMeshProUGUI targetText = null;
    public GameObject targetObj = null;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Confirm()
    {
        float value = float.Parse(previewText.text, System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
        targetText.text = previewText.text;
        UnityEngine.Debug.Log("Setting keypad value to " + previewText.text);
        previewText.text = "";
        gameObject.SetActive(false);
    }

    public void KeyPressed(int key_id)
    {

        string text = previewText.text;
        if (key_id == -1)
            text = text.Remove(text.Length - 1);
        else if (key_id == -2)
            text = text + ".";
        else
            text = text + key_id.ToString();
        
        previewText.text = text;
    }
}
