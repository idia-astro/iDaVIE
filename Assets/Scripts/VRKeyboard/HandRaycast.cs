/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 *
 * This file is part of the iDaVIE project.
 *
 * iDaVIE is free software: you can redistribute it and/or modify it under the terms 
 * of the GNU Lesser General Public License (LGPL) as published by the Free Software 
 * Foundation, either version 3 of the License, or (at your option) any later version.
 *
 * iDaVIE is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR 
 * PURPOSE. See the GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License along with 
 * iDaVIE in the LICENSE file. If not, see <https://www.gnu.org/licenses/>.
 *
 * Additional information and disclaimers regarding liability and third-party 
 * components can be found in the DISCLAIMER and NOTICE files included with this project.
 *
 */
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
