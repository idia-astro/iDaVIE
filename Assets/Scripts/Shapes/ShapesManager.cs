using DataFeatures;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using VolumeData;

public class ShapesManager : MonoBehaviour {
    public ShapeMenuController shapeMenuController;
    public GameObject cube;
    public GameObject cuboid;
    public GameObject sphere;
    public GameObject cylinder;
    private GameObject currentShape; 
    private GameObject movableShape;
    private int currentShapeIndex;
    private List<GameObject> activeShapes = new List<GameObject>();
    private List<GameObject> selectedShapes = new List<GameObject>();
    private List<GameObject> deletedShapes = new List<GameObject>();
    private List<GameObject> paintedShapes = new List<GameObject>();
    private Stack<ShapeAction> actions = new Stack<ShapeAction>();
    private GameObject[] shapes;
    private int[] shapesCount;
    private Color32 baseColour;
    private bool shapeSelected = false;

    public enum ShapeState {idle, selecting, selected};
    public ShapeState state;

    public void StartShapes() {
        state = ShapeState.selecting;
        baseColour = new Color32(42,46,40,255);
        currentShape = cube;
        currentShapeIndex = 0;
        shapes = new GameObject[] {cube, cuboid, sphere, cylinder};
        shapesCount = new int[]{0,0,0,0};
    }

    public GameObject GetCurrentShape() {
        return shapes[currentShapeIndex];
    }

    public void SelectShape() {
        if(state != ShapeState.selecting) return;
        state = ShapeState.selected;
        var shapeScript = currentShape.GetComponent<Shape>();
        shapeScript.SetAdditive(true);
        shapeSelected = true;
    }

    public void DeselectShape() {
        if(state != ShapeState.selected) return;
        state = ShapeState.selecting;
        var renderer = currentShape.GetComponent<Renderer>();
        renderer.material.color = baseColour;
        shapeSelected = false;
    }

    public bool isShapeSelected() {
        return shapeSelected;
    }

    public bool isIdle() {
        if(state == ShapeState.idle) return true;
        return false;
    }

    public void MakeIdle() {
        state = ShapeState.idle;
    }

    public GameObject GetNextShape() {
        if(state != ShapeState.selecting) return null;
        DestroyCurrentShape();
        currentShapeIndex++;
        if(currentShapeIndex == shapes.Length) currentShapeIndex = 0;
        return shapes[currentShapeIndex];
    }

    public GameObject GetPreviousShape() {
        if(state != ShapeState.selecting) return null;
        DestroyCurrentShape();
        currentShapeIndex-=1;
        if(currentShapeIndex < 0) currentShapeIndex = shapes.Length - 1;
        return shapes[currentShapeIndex];
    }

    public void AddShape(GameObject shape) {
         activeShapes.Add(shape);
         actions.Push(new ShapeAction(shape));
    }

    public string GetShapeName(GameObject shape) {
        shapesCount[currentShapeIndex]++;
        return shape.name + $"{shapesCount[currentShapeIndex]}";
    }

    public void AddSelectedShape(GameObject shape) {
        selectedShapes.Add(shape);
    }

    public void RemoveSelectedShape(GameObject shape) {
        selectedShapes.Remove(shape);
    }

    public void IncreaseScale() {
        if(state == ShapeState.selecting || state == ShapeState.selected) return;
        foreach(GameObject shape in selectedShapes) {
           Vector3 scale = shape.transform.localScale;
           Vector3 scaleVector = new Vector3(0,0,0);
           if(shape.name.Contains("Cuboid")) scaleVector = new Vector3(0.01f,0.01f,0.01f);
           else scaleVector = new Vector3(0.001f,0.001f,0.001f);
           scale = scale + scaleVector;
           shape.transform.localScale = scale;
        }
    }

    public void DecreaseScale() {
        if(state == ShapeState.selecting || state == ShapeState.selected) return;
        foreach(GameObject shape in selectedShapes) {
           Vector3 scale = shape.transform.localScale;
           Vector3 scaleVector = new Vector3(0,0,0);
           if(shape.name.Contains("Cuboid")) scaleVector = new Vector3(0.01f,0.01f,0.01f);
           else scaleVector = new Vector3(0.001f,0.001f,0.001f);
           scale = scale - scaleVector;
           if(scale.x < 0.002f | scale.y < 0.002f | scale.z < 0.002f) scale = new Vector3(0.001f,0.001f,0.001f);
           shape.transform.localScale = scale;
        }
    }

    public void DeleteSelectedShapes() {
        List<GameObject> delShapes = new List<GameObject>();
        foreach(GameObject shape in selectedShapes) {
            activeShapes.Remove(shape);
            shape.SetActive(false);
            deletedShapes.Add(shape);
            delShapes.Add(shape);
        }
        actions.Push(new ShapeAction(ShapeAction.ActionType.DeleteShapes, delShapes));
        selectedShapes = new List<GameObject>();
    }

    public void ChangeModes() {
        if(state == ShapeState.selected){
            ChangeShapeMode();
            return;
        } 
        foreach(GameObject shape in selectedShapes) {
            Shape shapeScript = shape.GetComponent<Shape>();
            shapeScript.SetAdditive(!shapeScript.isAdditive());
        }
    }

    public void CopyShapes() {
        if(selectedShapes.Count > 0) {
            paintedShapes = new List<GameObject>();
        }
        if(paintedShapes.Count > 0) {
            foreach(GameObject shape in paintedShapes) {
                activeShapes.Add(shape);
                shape.SetActive(true);
                selectedShapes.Add(shape);
            }
            paintedShapes = new List<GameObject>();
            return;
        }

        List<GameObject> copiedShapes = new List<GameObject>();
        foreach(GameObject shape in selectedShapes) {
            GameObject copiedShape = Instantiate(shape, shape.transform.position, shape.transform.rotation);
            Vector3 pos = copiedShape.transform.position;
            pos.y+=0.1f;
            copiedShape.transform.position = pos;
            copiedShape.transform.SetParent(shape.transform.parent);
            copiedShape.transform.localScale = shape.transform.localScale;
            Shape shapeScript = copiedShape.GetComponent<Shape>();
            shapeScript.SetAdditive(shape.GetComponent<Shape>().isAdditive());
            activeShapes.Add(copiedShape);
            copiedShapes.Add(copiedShape);
        }
        foreach(GameObject shape in copiedShapes) selectedShapes.Add(shape);
        actions.Push(new ShapeAction(ShapeAction.ActionType.CopyShapes, copiedShapes));
    }

    public void SetSelectableShape(GameObject shape) {
        currentShape = shape;
        //print(currentShape.transform.localScale);
    }

    public GameObject GetSelectedShape() {
        //print(currentShape.transform.localScale);
        return currentShape;
    }

    public void SetMoveableShape(GameObject shape) {
        movableShape = shape;
    }

    public GameObject GetMoveableShape() {
        return movableShape;
    }

    public void ChangeShapeMode() {
        if(state != ShapeState.selected) return;

        var shapeScript = currentShape.GetComponent<Shape>();
        if(shapeScript.isAdditive()) {
            shapeScript.SetAdditive(false);
        }
        else {
            shapeScript.SetAdditive(true);
        }
    }
    public void applyMask(VolumeDataSetRenderer _activeDataSet, VolumeInputController  _volumeInputController, bool additive, bool undo)
    {
        List<GameObject> undoShapes = new List<GameObject>();
        foreach (GameObject shape in activeShapes)
        {
            shape.SetActive(false);
        }

        foreach (GameObject shape in activeShapes)
        {
            shape.SetActive(true);
            Shape shapeScript = shape.GetComponent<Shape>();

            if(shapeScript.isAdditive() != additive)
            {
                shape.SetActive(false);
                continue;
            }

            Vector3 centre = findCentre(shape);
            GameObject boundingBox = shape.transform.GetChild(0).gameObject;
            boundingBox.transform.rotation = Quaternion.identity;
            BoxCollider boxCollider = boundingBox.GetComponent<BoxCollider>();
            Vector3 min = boxCollider.center - boxCollider.size * 0.5f; 
            Vector3 max = boxCollider.center + boxCollider.size * 0.5f;
            min = boxCollider.transform.TransformPoint(min);
            max = boxCollider.transform.TransformPoint(max);
            boundingBox.transform.rotation = shape.transform.rotation;
            float xStep = _activeDataSet.transform.localScale.x/_activeDataSet.GetCubeDimensions().x;
            float yStep = _activeDataSet.transform.localScale.y/_activeDataSet.GetCubeDimensions().y;
            float zStep = _activeDataSet.transform.localScale.z/_activeDataSet.GetCubeDimensions().z;
            for(float i = min.x; i < max.x; i+=xStep/4) {
                for(float j = min.y; j < max.y; j+=yStep/4) {
                    for(float k = min.z; k < max.z; k+=zStep/4)
                     {
                        Vector3 pos = new Vector3(i,j,k);
                        pos = pos - boundingBox.transform.position;
                        pos = boundingBox.transform.rotation * pos;
                        pos = pos + boundingBox.transform.position;
                        if(insideShape(shape,pos,centre))
                        {
                            if(additive)
                            {
                                _activeDataSet.SetCursorPosition(pos,1);
                                _activeDataSet.PaintCursor((short) _volumeInputController.SourceId);
                                
                                //note: I changed the method called used here (UpdateStats) to public in order for the source list to be updated 
                                _activeDataSet.Mask.UpdateStats(_volumeInputController.SourceId);  
                            }
                            else{
                                _activeDataSet.SetCursorPosition(pos,1);
                                _activeDataSet.PaintCursor((short) 0); 
                            }
                            
                        }
                    }
                }
            }
            if(!undo) paintedShapes.Add(shape);
            if(additive) {
                GameObject copiedShape = Instantiate(shape, shape.transform.position, shape.transform.rotation);
                copiedShape.transform.SetParent(shape.transform.parent);
                copiedShape.transform.localScale = shape.transform.localScale;
                shapeScript = copiedShape.GetComponent<Shape>();
                shapeScript.SetAdditive(false);
                copiedShape.SetActive(false);
                undoShapes.Add(copiedShape);
            }
            shape.SetActive(false);
        }
        if(additive) {
            actions.Push(new ShapeAction(ShapeAction.ActionType.Paint, undoShapes));
        }

        _activeDataSet.Mask.ConsolidateMaskEntries();
    }

    public bool insideShape(GameObject shape, Vector3 point, Vector3 centre)
    {
        RaycastHit hit;
        Vector3 rayDirection = centre - point;
        Physics.queriesHitBackfaces = true;
        if (Physics.Raycast(point, rayDirection , out hit)) {
            if(hit.transform.name == shape.transform.name)
            {Vector3 norm = hit.normal;
            float dot = Vector3.Dot(rayDirection, norm);
            return dot > 0;}
            return true;
        }
        return true;
    }

    public Vector3 findCentre(GameObject shape) {
        Vector3 centre = new Vector3(0,0,0);
        Vector3[] vertices = shape.GetComponent<MeshFilter>().mesh.vertices;
        foreach(Vector3 vertex in vertices) {
            centre += shape.transform.TransformPoint(vertex);
        }
        return centre/vertices.Length;
    }

    public void Undo() {
        if(actions.Count == 0) return;
        ShapeAction lastAction = actions.Pop();
        switch (lastAction.type) {
            case ShapeAction.ActionType.AddShape:
                activeShapes.Remove(lastAction.addedShape);
                if(selectedShapes.Contains(lastAction.addedShape)) selectedShapes.Remove(lastAction.addedShape);
                lastAction.addedShape.GetComponent<Shape>().DestroyShape();
            break;

            case ShapeAction.ActionType.DeleteShapes:
                foreach(GameObject shape in lastAction.shapeList) {
                    shape.SetActive(true);
                    activeShapes.Add(shape);
                    selectedShapes.Add(shape);
                    shape.GetComponent<Shape>().SetSelected(true);
                    deletedShapes.Remove(shape);
                }
            break;

            case ShapeAction.ActionType.CopyShapes:
                foreach(GameObject shape in lastAction.shapeList) {
                    activeShapes.Remove(shape);
                    if(selectedShapes.Contains(shape)) selectedShapes.Remove(shape);
                    shape.GetComponent<Shape>().DestroyShape();
                }
            break;

            case ShapeAction.ActionType.Paint:
                foreach(GameObject shape in lastAction.shapeList) {
                    shape.SetActive(true);
                    activeShapes.Add(shape);
                }
                applyMask(shapeMenuController._activeDataSet,shapeMenuController._volumeInputController,false,true);
                foreach(GameObject shape in lastAction.shapeList) {
                    shape.SetActive(false);
                }
                activeShapes = new List<GameObject>();
            break;
        }
    }

    public void DestroyCurrentShape() {
        Shape shapeScript = currentShape.GetComponent<Shape>();
        shapeScript.DestroyShape();
    }

    public void ClearShapes() {
        activeShapes = new List<GameObject>();
        selectedShapes = new List<GameObject>();
    }

    public void ClearPaintedShapes() {
        paintedShapes = new List<GameObject>();
    }

    public void DestroyShapes() {
        foreach(GameObject shape in activeShapes) {
            if(shape)
            {
                Shape shapeScript = shape.GetComponent<Shape>();
                shapeScript.DestroyShape();
            }
        }
        foreach(GameObject shape in deletedShapes) {
            if(shape)
            {
                Shape shapeScript = shape.GetComponent<Shape>();
                shapeScript.DestroyShape(); 
            }
            
        }
        actions.Clear();
        ClearShapes();
        deletedShapes = new List<GameObject>();
        ClearPaintedShapes();
        shapesCount = new int[]{0,0,0,0};
    }
}