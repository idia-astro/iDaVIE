using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Symbol_VR_button : Abstract_VR_button {    
    public List<GameObject> buttons_to_switch = new List<GameObject>();

    //IMPORTANT: if the symbol mode of the keyboard is active, then pressing the shift has no effects
    public override void onPress()
    {
        //switch to lower case in case of symbol mode
        if (buttons_to_switch[0].GetComponent<Char_VR_button>().low_cap_sym[2].gameObject.activeSelf)
        {
            foreach (GameObject button in buttons_to_switch)
            {
                button.GetComponent<Char_VR_button>().switchToLow();
            }
        }
        //switch to symbol mode
        else 
        {
            foreach (GameObject button in buttons_to_switch)
            {
                button.GetComponent<Char_VR_button>().switchToSymbol();
            }
        }
    }
}
