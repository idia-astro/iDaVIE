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
