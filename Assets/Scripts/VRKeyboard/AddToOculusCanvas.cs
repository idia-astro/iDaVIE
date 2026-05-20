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
using UnityEngine;

public class AddToOculusCanvas : MonoBehaviour {

	// Use this for initialization
	void Start () {
        StartCoroutine("addToOculusCanvas");
	}
	

    private IEnumerator addToOculusCanvas()
    {
        while (GameObject.Find("Canvas_Oculus") == null)
        {
            yield return new WaitForEndOfFrame();
        }
            transform.parent = GameObject.Find("Canvas_Oculus").transform;
            transform.localPosition = new Vector3(-25, -350, -175);
            transform.rotation = Quaternion.Euler(0, 0, 0);
            transform.localScale = new Vector3(40, 40, 1);
    }
	
}
