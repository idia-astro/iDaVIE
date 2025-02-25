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
    private List<GameObject> shapes = new List<GameObject>();

    public enum ShapeState {selecting, selected};
    public ShapeState state;

    public void StartShapes() {
        state = ShapeState.selecting;
        currentShape = cube;
    }

    public GameObject GetCurrentShape() {
        return currentShape;
    }

    public void AddShape(GameObject shape) {
         shapes.Add(shape);
    }

    public void DestroyShapes() {
        foreach(GameObject shape in shapes) {
            Shape shapeScript = shape.GetComponent<Shape>();
            shapeScript.DestroyShape();
        }
    }
}