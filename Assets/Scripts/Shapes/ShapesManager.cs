/*
 * ShapeManager class manages state of all shapes in the scene, keeping track of selected and non-selected shapes
 * as actions performed by the user to allow undo functionality
*/

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
    private Vector3 currentShapeScale;
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

    /*
    * States user can be in:
    * idle: not adding a new shape
    * selecting: cycling through available shapes on controller
    * selected: user has chosen a shape from the list, but has not placed it in the scene yet
    */
    public enum ShapeState {idle, selecting, selected};
    public ShapeState state;

    public void StartShapes() {
        state = ShapeState.selecting;
        baseColour = new Color32(42,46,40,255); 
        currentShape = cube;
        currentShapeIndex = 0;
        shapes = new GameObject[] {cube, cuboid, sphere, cylinder};
        shapesCount = new int[]{0,0,0,0}; //Used to have unique names for each shape added into scene
    }

    public GameObject GetCurrentShape() {
        return shapes[currentShapeIndex];
    }

    //called when user transitions from selecting to selected state
    public void SelectShape() {
        if(state != ShapeState.selecting) return;
        state = ShapeState.selected;
        var shapeScript = currentShape.GetComponent<Shape>();
        shapeScript.SetAdditive(true);
        shapeSelected = true;
    }

    //called when user transitions from selected to selecting state
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

    //Generates unique name for shape to be added into the scene
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


    //Increases scale of shapes. If in selected state only increases shape attatched to controller, otherwise increases scale of all selected shapes in the scene
    public void IncreaseScale() {
        if(state == ShapeState.selecting) return;
        if(state == ShapeState.selected) {
           Vector3 scale = currentShape.transform.localScale;
           Vector3 scaleVector = new Vector3(0,0,0);
           if(currentShape.name.Contains("Cuboid")) scaleVector = new Vector3(0.01f,0.01f,0.01f);
           else scaleVector = new Vector3(0.001f,0.001f,0.001f);
           scale = scale + scaleVector;
           currentShape.transform.localScale = scale;
           return;
        }
        foreach(GameObject shape in selectedShapes) {
           Vector3 scale = shape.transform.localScale;
           Vector3 scaleVector = new Vector3(0,0,0);
           if(shape.name.Contains("Cuboid")) scaleVector = new Vector3(0.01f,0.01f,0.01f);
           else scaleVector = new Vector3(0.001f,0.001f,0.001f);
           scale = scale + scaleVector;
           shape.transform.localScale = scale;
        }
    }

    //Same as above but with decreasing scale
    public void DecreaseScale() {
        if(state == ShapeState.selecting) return;
        if(state == ShapeState.selected) {
           Vector3 scale = currentShape.transform.localScale;
           Vector3 scaleVector = new Vector3(0,0,0);
           if(currentShape.name.Contains("Cuboid")) scaleVector = new Vector3(0.01f,0.01f,0.01f);
           else scaleVector = new Vector3(0.001f,0.001f,0.001f);
           scale = scale - scaleVector;
           if(scale.x < 0.002f | scale.y < 0.002f | scale.z < 0.002f) scale = new Vector3(0.001f,0.001f,0.001f);
           currentShape.transform.localScale = scale;
           return;
        }
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

    //Delete all selected shapes in the secene (shapes are only properly deleted upon exiting shape mode)
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

    //If in selected state change only that shape's state, otherwise change all selected shapes in scene to the inverse of their current state
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

    /*
    * Copies all selected shapes in scene, if used after confirming a selection it returns all shapes used for the selection to the scene
    */
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

    //Sets the shape selected by the user in the selected state
    public void SetSelectableShape(GameObject shape) {
        currentShape = shape;
        currentShapeScale = shape.transform.localScale;
    }

    //Returns the shape selected by the user in the selected state, used to make a copy of the shape to be placed into the scene
    public GameObject GetSelectedShape() {
        if(currentShapeIndex == 1) currentShape.transform.localScale = Vector3.Scale(currentShape.transform.localScale, currentShapeScale);
        return currentShape;
    }

    //Ensures only one shape can be moved at a time
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

    /*
     *Applies the selection and generates the mask from the shapes
     *The additive param decides whether the function will be used on additive or subtractive shapes
     *The undo param is used if the user is undoing a selection
    */
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

            //The following block of code does a transformation to get the correct bounding corners of the bounding box
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

            //These loops loop through the dimensions of the bounding box of the shape aligned with the data cubes axes
            for(float i = min.x; i < max.x; i+=xStep/4) {
                for(float j = min.y; j < max.y; j+=yStep/4) {
                    for(float k = min.z; k < max.z; k+=zStep/4)
                     {
                        Vector3 pos = new Vector3(i,j,k);
                        //each position needs to be transformed to match the correct orientation of the shape's bounding box
                        pos = pos - boundingBox.transform.position;
                        pos = boundingBox.transform.rotation * pos;
                        pos = pos + boundingBox.transform.position;
                        if(insideShape(shape,pos,centre))
                        {
                            if(additive)
                            {
                                _activeDataSet.SetCursorPosition(pos,1);
                                _activeDataSet.PaintCursor((short) _volumeInputController.SourceId);
                                
                                 
                            }
                            else{
                                _activeDataSet.SetCursorPosition(pos,1);
                                _activeDataSet.PaintCursor((short) 0); 
                            }
                            
                        }
                    }
                }
            }
            //note: I changed the method called here (UpdateStats) to public in order for the source list to be updated  
            _activeDataSet.Mask.UpdateStats(_volumeInputController.SourceId);
            
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

    /// <summary>
    /// Determines whether a given point is inside a specified shape using a raycast test.
    /// 
    /// The function casts a ray from the point towards the centre of the shape. If the ray hits the shape,
    /// it checks the direction of the surface normal at the hit point to determine if the point is inside.
    /// Returns true if the point is considered inside the shape, otherwise false.
    /// 
    /// Note: This method relies on Unity's Physics.Raycast and assumes the shape has appropriate colliders.
    /// </summary>
    /// <param name="shape">The GameObject representing the shape to test against.</param>
    /// <param name="point">The world-space position to test for inclusion within the shape.</param>
    /// <param name="centre">The world-space centre of the shape (typically the centroid of its mesh).</param>
    /// <returns>True if the point is inside the shape, otherwise false.</returns>
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

    //Find the centre of a shape's mesh of vertices
    public Vector3 findCentre(GameObject shape) {
        Vector3 centre = new Vector3(0,0,0);
        Vector3[] vertices = shape.GetComponent<MeshFilter>().mesh.vertices;
        foreach(Vector3 vertex in vertices) {
            centre += shape.transform.TransformPoint(vertex);
        }
        return centre/vertices.Length;
    }

    //Undo the last action performed by the user
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