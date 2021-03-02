using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KeypadController : MonoBehaviour
{


    public Text previewText;
    public Text targetText = null;

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
        Debug.Log(value);
        targetText.text = previewText.text;
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
