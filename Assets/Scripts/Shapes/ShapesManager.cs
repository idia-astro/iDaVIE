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

    public enum ShapeState {selecting, selected};
    public ShapeState state;

    public void StartShapes() {
        state = ShapeState.selecting;
        currentShape = cube;
    }

    public GameObject GetCurrentShape() {
        return currentShape;
    }
}