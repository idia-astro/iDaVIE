using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Char_VR_button : Abstract_VR_button {
    public char char_key = (char)0;
    public List<TextMesh> low_cap_sym = new List<TextMesh>();

    protected new void Start()
    {
        base.Start();
        if (char_key == (char)0)
        {
            if (low_cap_sym.Count == 0)
                low_cap_sym.Add(GetComponentInChildren<TextMesh>());
            char_key = low_cap_sym[0].text.ToCharArray()[0];
        }
        //this is only to manage the return char, which apparently is not assignable from the unity GUI
        else if (char_key == '⏎')
        {
            char_key = '\n';
        }
    }

    public override void onPress()
    {
        //input to the textBox
        keyboardManager.type_char(char_key);
    }

    public void switchToLow()
    {
        if (low_cap_sym == null || low_cap_sym.Count < 3)
            return;
        low_cap_sym[1].gameObject.SetActive(false);
        low_cap_sym[2].gameObject.SetActive(false);
        low_cap_sym[0].gameObject.SetActive(true);
        char_key = low_cap_sym[0].text.ToCharArray()[0];
    }

    public void switchToCapital()
    {
        if (low_cap_sym == null || low_cap_sym.Count < 3)
            return;
        low_cap_sym[0].gameObject.SetActive(false);
        low_cap_sym[2].gameObject.SetActive(false);
        low_cap_sym[1].gameObject.SetActive(true);
        char_key = low_cap_sym[1].text.ToCharArray()[0];
    }

    public void switchToSymbol()
    {
        if (low_cap_sym == null || low_cap_sym.Count < 3)
            return;
        low_cap_sym[0].gameObject.SetActive(false);
        low_cap_sym[1].gameObject.SetActive(false);
        low_cap_sym[2].gameObject.SetActive(true);
        char_key = low_cap_sym[2].text.ToCharArray()[0];
    }
}
