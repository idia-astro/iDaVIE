using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Backspace_VR_button : Abstract_VR_button {

    public override void onPress()
    {
        keyboardManager.backspace();
    }
}
