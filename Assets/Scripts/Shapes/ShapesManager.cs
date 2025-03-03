using DataFeatures;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VolumeData;

public class ShapesManager : MonoBehaviour {
    public GameObject cube;
    public GameObject cuboid;
    public GameObject sphere;
    public GameObject cylinder;
    private GameObject currentShape; 
    private GameObject movableShape;
    private int currentShapeIndex;
    private List<GameObject> activeShapes = new List<GameObject>();
    private List<GameObject> selectedShapes = new List<GameObject>();
    private GameObject[] shapes;
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
        if(state == ShapeState.selected) return null;
        DestroyCurrentShape();
        currentShapeIndex++;
        if(currentShapeIndex == shapes.Length) currentShapeIndex = 0;
        return shapes[currentShapeIndex];
    }

    public GameObject GetPreviousShape() {
        if(state == ShapeState.selected) return null;
        DestroyCurrentShape();
        currentShapeIndex-=1;
        if(currentShapeIndex < 0) currentShapeIndex = shapes.Length - 1;
        return shapes[currentShapeIndex];
    }

    public void AddShape(GameObject shape) {
         activeShapes.Add(shape);
    }

    public void AddSelectedShape(GameObject shape) {
        selectedShapes.Add(shape);
    }

    public void RemoveSelectedShape(GameObject shape) {
        selectedShapes.Remove(shape);
    }

    public void DeleteSelectedShapes() {
        foreach(GameObject shape in selectedShapes) {
            activeShapes.Remove(shape);
            Shape shapeScript = shape.GetComponent<Shape>();
            shapeScript.DestroyShape();
        }
        selectedShapes = new List<GameObject>();
    }

    public void ChangeModes() {
        foreach(GameObject shape in selectedShapes) {
            Shape shapeScript = shape.GetComponent<Shape>();
            shapeScript.SetAdditive(!shapeScript.isAdditive());
        }
    }

    public void SetSelectableShape(GameObject shape) {
        currentShape = shape;
    }

    public GameObject GetSelectedShape() {
        if(state == ShapeState.selecting) return null;
        return currentShape;
    }

    public void SetMoveableShape(GameObject shape) {
        movableShape = shape;
    }

    public GameObject GetMoveableShape() {
        return movableShape;
    }

    public void ChangeShapeMode() {
        if(state == ShapeState.selecting) return;

        var shapeScript = currentShape.GetComponent<Shape>();
        if(shapeScript.isAdditive()) {
            shapeScript.SetAdditive(false);
        }
        else {
            shapeScript.SetAdditive(true);
        }
    }

    public void applyMask(VolumeDataSetRenderer _activeDataSet, VolumeInputController  _volumeInputController, bool additive)
    {
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
                Debug.Log("Skipped shape: " + additive);
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
            for(float i = min.x; i < max.x; i+=xStep/2) {
                for(float j = min.y; j < max.y; j+=yStep/2) {
                    for(float k = min.z; k < max.z; k+=zStep/2)
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
                                _activeDataSet.PaintCursor((short) 1000);     
                            }
                            else{
                                _activeDataSet.SetCursorPosition(pos,1);
                                _activeDataSet.PaintCursor((short) 0); 
                            }
                            
                        }
                    }
                }
            }
            shape.SetActive(false);
        }

        foreach (GameObject shape in activeShapes)
        {
            shape.SetActive(true);
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

    public void DestroyCurrentShape() {
        Shape shapeScript = currentShape.GetComponent<Shape>();
        shapeScript.DestroyShape();
    }

    public void DestroyShapes() {
        foreach(GameObject shape in activeShapes) {
            Shape shapeScript = shape.GetComponent<Shape>();
            shapeScript.DestroyShape();
        }
        activeShapes = new List<GameObject>();
    }
}