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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows.Speech;

public class Audio_VR_button : Abstract_VR_button
{

    private bool clicked = false;

    protected new void Update()
    {
        if (!clicked)
        {
            if (keepSelected > 0)
            {
                if (materialIndex == 0)
                {
                    materialIndex = 1;
                    renderer.sharedMaterial = materials[materialIndex];
                }
                keepSelected -= Time.deltaTime;
            }
            else if (materialIndex == 1)
            {
                materialIndex = 0;
                renderer.sharedMaterial = materials[materialIndex];
            }
        }
    }

    public override void onPress()
    {
        if(keyboardManager.dictationRecognizer.Status == SpeechSystemStatus.Stopped)
        {
            keyboardManager.dictationRecognizer.Start();
            clicked = true;
            renderer.sharedMaterial = materials[1];
            Debug.Log("recognition started");
        }
        else if(keyboardManager.dictationRecognizer.Status == SpeechSystemStatus.Running)
        {
            keyboardManager.dictationRecognizer.Stop();
            clicked = false;
            renderer.sharedMaterial = materials[0];
            Debug.Log("recognition stopped");
        }
    }

    public void OnApplicationQuit()
    {
        keyboardManager.dictationRecognizer.Stop();
        Debug.Log("recognition stopped");
    }
}