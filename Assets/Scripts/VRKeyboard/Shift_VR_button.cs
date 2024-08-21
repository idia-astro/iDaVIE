/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 Inter-University Institute for Data Intensive Astronomy
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
