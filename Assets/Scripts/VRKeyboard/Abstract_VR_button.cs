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

public abstract class Abstract_VR_button : MonoBehaviour {
    public KeyboardManager keyboardManager;

    public Material[] materials;    //this is to switch the color of the button while it is aimed by the ray
    public float keepSelected = 0;
    public int materialIndex = 0;
    protected Renderer renderer;

    protected void Start()
    {
        keyboardManager = GameObject.FindGameObjectWithTag("VRKeyboard").GetComponent<KeyboardManager>();
        renderer = GetComponent<Renderer>();
        renderer.enabled = true;
        renderer.sharedMaterial = materials[materialIndex];
    }

    protected void Update()
    {
        if (keepSelected > 0)
        {
            //switch material if it was not done before
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

    abstract public void onPress();

}
