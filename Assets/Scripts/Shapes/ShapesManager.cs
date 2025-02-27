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
    private int currentShapeIndex;
    private List<GameObject> activeShapes = new List<GameObject>();
    private GameObject[] shapes;

    private Color32 baseColour;

    public enum ShapeState {selecting, selected};
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
        if(state == ShapeState.selected) return;
        state = ShapeState.selected;
        var shapeScript = currentShape.GetComponent<Shape>();
        shapeScript.SetAdditive(true);
    }

    public void DeselectShape() {
        if(state == ShapeState.selecting) return;
        state = ShapeState.selecting;
        var renderer = currentShape.GetComponent<Renderer>();
        renderer.material.color = baseColour;
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

    public void SetSelectableShape(GameObject shape) {
        currentShape = shape;
    }

    public GameObject GetSelectedShape() {
        if(state == ShapeState.selecting) return null;
        return currentShape;
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