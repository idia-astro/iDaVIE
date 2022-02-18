using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class HandRaycast : MonoBehaviour
{
    private readonly SteamVR_Action_Boolean _trigger = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("InteractUI");
    private int m_RayLength = 100;

    // Update is called once per frame
    void Update()
    {
        RaycastHit hit;

        if (Physics.Raycast(transform.position, transform.forward, out hit, m_RayLength))
        {
            if (hit.collider.gameObject.GetComponent<Abstract_VR_button>())
            {
                //this is to highlight the pointed button
                hit.collider.gameObject.GetComponent<Abstract_VR_button>().keepSelected = .05f;

                if (_trigger.stateDown)
                {
                    hit.collider.gameObject.GetComponent<Abstract_VR_button>().onPress();
                }
            }
            else if (hit.collider.gameObject.GetComponent<VRKeyboard_Text>() && _trigger.stateUp)
            {
                hit.collider.gameObject.GetComponent<VRKeyboard_Text>().OnSelect();
                hit.collider.gameObject.GetComponent<VRKeyboard_Text>().GoOnLastChar();
            }
        }
    }
}
