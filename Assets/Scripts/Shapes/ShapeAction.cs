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

/// <summary>
/// This class just stores information about actions performed by the user
///  in shape mode to allow for undo functionality
/// </summary>
public class ShapeAction {
    public GameObject addedShape;
    public List<GameObject> shapeList = new List<GameObject>();
    public enum ActionType {AddShape,CopyShapes,DeleteShapes,Paint};
    public ActionType type;
    public ShapeAction(GameObject shape) {
        type = ActionType.AddShape;
        addedShape = shape;
    }

    public ShapeAction(ActionType actionType, List<GameObject> shapes) {
        type = actionType;
        shapeList = shapes;
    }

}