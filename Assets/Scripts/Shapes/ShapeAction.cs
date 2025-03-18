using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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