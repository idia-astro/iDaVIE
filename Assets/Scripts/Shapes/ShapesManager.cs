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
    private bool additive = true;
    private GameObject currentShape; 
    private int currentShapeIndex;
    private List<GameObject> activeShapes = new List<GameObject>();
    private GameObject[] shapes;
    private Color additiveColour;
    private Color subtractiveColour;

    public enum ShapeState {selecting, selected};
    public ShapeState state;

    public void StartShapes() {
        state = ShapeState.selecting;
        additiveColour = Color.green;
        subtractiveColour = Color.red;
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
        var renderer = currentShape.GetComponent<Renderer>();
        renderer.material.color = additiveColour;
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