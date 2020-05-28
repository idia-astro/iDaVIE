using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shift_VR_button : Abstract_VR_button {
    public List<GameObject> buttons_to_switch = new List<GameObject>();
    public bool doubleClick = false;

    //IMPORTANT: if the symbol mode of the keyboard is active, then pressing the shift has no effects
    public override void onPress()
    {
        //switch to upper case
        if (buttons_to_switch[0].GetComponent<Char_VR_button>().low_cap_sym[0].gameObject.activeSelf)
        {
            foreach (GameObject button in buttons_to_switch)
            {
                button.GetComponent<Char_VR_button>().switchToCapital();
            }
        }
        //switch to lower case
        else if (buttons_to_switch[0].GetComponent<Char_VR_button>().low_cap_sym[1].gameObject.activeSelf)
        {
            foreach (GameObject button in buttons_to_switch)
            {
                button.GetComponent<Char_VR_button>().switchToLow();
            }
        }
        StartCoroutine("startDoubleClickTimer");
    }
    
    private IEnumerator startDoubleClickTimer()
    {
        doubleClick = true;
        yield return new WaitForSeconds(2);
        doubleClick = false;
    }
}
