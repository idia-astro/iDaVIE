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
using UnityEngine.Serialization;

public class CustomDragHandler : MonoBehaviour
{
    [FormerlySerializedAs("spawnPoint")] public GameObject anchorPoint;       // The parent object of the objects to be dragged.
                                        // Set this to Content object when using ScrollRect to allow clamps to work properly.
                                        // This requires adjusting the Content size if spawning objects at runtime.
    public int scrollSpeed;
    public int buttonClickMovement;
    private RectTransform anchorPointPosition;
    public float Spawn_initial_y {get; private set;}


    // Use this for initialization
    void Start()
    {
        anchorPointPosition = anchorPoint.gameObject.GetComponent<RectTransform>();
    }


    public void MoveUp()
    {
        anchorPointPosition.localPosition += Vector3.down * scrollSpeed;
    }
    
    public void MoveDown()
    {
        anchorPointPosition.localPosition += Vector3.up * scrollSpeed;
    }
    
    public void MoveUpClick()
    {
        anchorPointPosition.localPosition += Vector3.down * buttonClickMovement;
    }

    public void MoveDownClick()
    {
        anchorPointPosition.localPosition += Vector3.up * buttonClickMovement;
    }
}
