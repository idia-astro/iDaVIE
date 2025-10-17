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

   /// <summary>
    /// States user can be in:
    /// idle: not adding a new shape
    /// selecting: cycling through available shapes on controller
    /// selected: user has chosen a shape from the list, but has not placed it in the scene yet
    /// </summary>
    public enum ShapeState {idle, selecting, selected};
    public ShapeState state;

    /// <summary>
    /// Initializes the shape selection process and sets up shape-related state.
    /// 
    /// Sets the manager state to selecting, initializes the base color, sets the current shape to cube,
    /// resets the shape index, and prepares arrays for available shapes and their counts.
    /// This method should be called when starting shape selection mode.
    /// </summary>
    public void StartShapes()
    {
        state = ShapeState.selecting;
        baseColour = new Color32(42, 46, 40, 255);
        currentShape = cube;
        currentShapeIndex = 0;
        shapes = new GameObject[] { cube, cuboid, sphere, cylinder };
        shapesCount = new int[] { 0, 0, 0, 0 }; //Used to have unique names for each shape added into scene
    }

    /// <summary>
    /// Returns the currently selected shape.
    /// 
    /// Retrieves the GameObject representing the shape at the current selection index.
    /// </summary>
    /// <returns>The currently selected shape GameObject.</returns>
    public GameObject GetCurrentShape()
    {
        return shapes[currentShapeIndex];
    }

    /// <summary>
    /// Selects the current shape and transitions to the selected state.
    /// 
    /// Changes the manager state from selecting to selected, sets the current shape to additive mode,
    /// and marks the shape as selected. Should be called when the user confirms their shape selection.
    /// </summary>
    public void SelectShape() {
        if(state != ShapeState.selecting) return;
        state = ShapeState.selected;
        var shapeScript = currentShape.GetComponent<Shape>();
        shapeScript.SetAdditive(true);
        shapeSelected = true;
    }

    /// <summary>
    /// Deselects the current shape and returns to selecting state.
    /// 
    /// Changes the manager state from selected to selecting, resets the shape's color to the base color,
    /// and marks the shape as not selected. Should be called when the user cancels their shape selection.
    /// </summary>
    public void DeselectShape() {
        if(state != ShapeState.selected) return;
        state = ShapeState.selecting;
        var renderer = currentShape.GetComponent<Renderer>();
        renderer.material.color = baseColour;
        shapeSelected = false;
    }

    /// <summary>
    /// Checks if a shape is currently selected.
    /// 
    /// Returns true if a shape has been selected by the user, otherwise false.
    /// </summary>
    /// <returns>True if a shape is selected, false otherwise.</returns>
    public bool isShapeSelected()
    {
        return shapeSelected;
    }

    /// <summary>
    /// Checks if the manager is currently idle.
    /// 
    /// Returns true if the state is idle, otherwise false.
    /// </summary>
    /// <returns>True if in idle state, false otherwise.</returns>
    public bool isIdle()
    {
        if (state == ShapeState.idle) return true;
        return false;
    }

    /// <summary>
    /// Sets the manager state to idle.
    /// 
    /// Changes the current state to idle, indicating that no shape is being added or selected.
    /// Should be called when exiting shape selection or placement mode.
    /// </summary>
    public void MakeIdle()
    {
        state = ShapeState.idle;
    }

    /// <summary>
    /// Cycles to the next available shape in selection mode.
    /// 
    /// If the manager is in selecting state, destroys the current shape, increments the shape index,
    /// wraps around if at the end of the shapes array, and returns the next shape GameObject.
    /// </summary>
    /// <returns>The next shape GameObject in the selection list, or null if not in selecting state.</returns>
    public GameObject GetNextShape()
    {
        if (state != ShapeState.selecting) return null;
        DestroyCurrentShape();
        currentShapeIndex++;
        if (currentShapeIndex == shapes.Length) currentShapeIndex = 0;
        return shapes[currentShapeIndex];
    }

    /// <summary>
    /// Cycles to the previous available shape in selection mode.
    /// 
    /// If the manager is in selecting state, destroys the current shape, decrements the shape index,
    /// wraps around to the end if at the beginning of the shapes array, and returns the previous shape GameObject.
    /// </summary>
    /// <returns>The previous shape GameObject in the selection list, or null if not in selecting state.</returns>
    public GameObject GetPreviousShape()
    {
        if (state != ShapeState.selecting) return null;
        DestroyCurrentShape();
        currentShapeIndex -= 1;
        if (currentShapeIndex < 0) currentShapeIndex = shapes.Length - 1;
        return shapes[currentShapeIndex];
    }

    /// <summary>
    /// Adds a shape to the active shapes list and records the action for undo functionality.
    /// 
    /// Adds the specified shape GameObject to the list of active shapes in the scene and pushes a new ShapeAction
    /// to the actions stack to enable undoing this addition.
    /// </summary>
    /// <param name="shape">The GameObject representing the shape to add.</param>
    public void AddShape(GameObject shape)
    {
        activeShapes.Add(shape);
        actions.Push(new ShapeAction(shape));
    }

    /// <summary>
    /// Generates a unique name for a shape to be added into the scene.
    /// 
    /// Increments the count for the current shape type and returns a unique name
    /// by appending the count to the shape's base name.
    /// </summary>
    /// <param name="shape">The GameObject representing the shape to name.</param>
    /// <returns>A unique string name for the shape.</returns>
    public string GetShapeName(GameObject shape) {
        shapesCount[currentShapeIndex]++;
        return shape.name + $"{shapesCount[currentShapeIndex]}";
    }

    /// <summary>
    /// Adds a shape to the selected shapes list.
    /// 
    /// Adds the specified shape GameObject to the list of selected shapes in the scene.
    /// </summary>
    /// <param name="shape">The GameObject representing the shape to add to the selected list.</param>
    public void AddSelectedShape(GameObject shape)
    {
        selectedShapes.Add(shape);
    }

    /// <summary>
    /// Removes a shape from the selected shapes list.
    /// 
    /// Removes the specified shape GameObject from the list of selected shapes in the scene.
    /// </summary>
    /// <param name="shape">The GameObject representing the shape to remove from the selected list.</param>
    public void RemoveSelectedShape(GameObject shape)
    {
        selectedShapes.Remove(shape);
    }


    /// <summary>
    /// Increases the scale of the selected shape(s).
    /// 
    /// If in selected state, increases the scale of the shape attached to the controller.
    /// Otherwise, increases the scale of all selected shapes in the scene.
    /// Cuboids are scaled by a larger increment than other shapes.
    /// </summary>
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

    /// <summary>
    /// Decreases the scale of the selected shape(s).
    /// 
    /// If in selected state, decreases the scale of the shape attached to the controller.
    /// Otherwise, decreases the scale of all selected shapes in the scene.
    /// Cuboids are scaled by a larger decrement than other shapes.
    /// Ensures that the scale does not go below a minimum threshold.
    /// </summary>
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

    /// <summary>
    /// Deletes all selected shapes from the scene and records the action for undo functionality.
    /// 
    /// Removes each selected shape from the active shapes list, disables it in the scene,
    /// adds it to the deleted shapes list, and records the deletion in the actions stack.
    /// Clears the selected shapes list after deletion.
    /// </summary>
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

    /// <summary>
    /// Toggles the additive/subtractive mode of selected shapes.
    /// 
    /// If in selected state, toggles the mode of the currently selected shape.
    /// Otherwise, toggles the mode of all shapes in the selected shapes list.
    /// The mode determines whether the shape is additive or subtractive in masking operations.
    /// </summary>
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

    /// <summary>
    /// Copies all selected shapes in the scene.
    /// 
    /// If there are painted shapes, returns them to the scene and adds them to the active and selected shapes lists.
    /// Otherwise, creates deep copies of all selected shapes, adds them to the active and selected shapes lists,
    /// and records the copy action for undo functionality.
    /// </summary>
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
            GameObject copiedShape = DeepCopyShape(shape);
            activeShapes.Add(copiedShape);
            copiedShapes.Add(copiedShape);
        }
        foreach(GameObject shape in copiedShapes) selectedShapes.Add(DeepCopyShape(shape));
        actions.Push(new ShapeAction(ShapeAction.ActionType.CopyShapes, copiedShapes));
    }
    
    /// <summary>
    /// Creates a deep copy of the specified shape GameObject.
    /// 
    /// Instantiates a new GameObject with the same position, rotation, parent, and scale as the original.
    /// Deep copies all materials and mesh data to ensure the new shape is independent of the original.
    /// </summary>
    /// <param name="original">The GameObject representing the shape to copy.</param>
    /// <returns>A new GameObject that is a deep copy of the original shape.</returns>
    GameObject DeepCopyShape(GameObject original) {
        GameObject copy = Instantiate(original, original.transform.position, original.transform.rotation);

        copy.transform.SetParent(original.transform.parent);
        copy.transform.localScale = original.transform.localScale;

        Renderer renderer = copy.GetComponent<Renderer>();
        if (renderer != null) {
            Material[] mats = renderer.materials; // this already makes unique instances in Unity
            for (int i = 0; i < mats.Length; i++) {
                mats[i] = new Material(mats[i]);   // full deep copy of each material
            }
            renderer.materials = mats;
        }

        MeshFilter mf = copy.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null) {
            mf.mesh = Instantiate(mf.sharedMesh); // unique mesh instance
        }

        return copy;
    }

    /// <summary>
    /// Sets the currently selectable shape and updates its scale.
    /// 
    /// Assigns the specified GameObject as the current shape and stores its local scale.
    /// Used when the user selects a shape in the selected state.
    /// </summary>
    /// <param name="shape">The GameObject to set as the currently selectable shape.</param>
    public void SetSelectableShape(GameObject shape)
    {
        currentShape = shape;
        currentShapeScale = shape.transform.localScale;
    }

    /// <summary>
    /// Returns the shape selected by the user in the selected state.
    /// 
    /// If the current shape is a cuboid, updates its scale using the stored scale vector.
    /// Returns the currently selected shape GameObject.
    /// </summary>
    /// <returns>The currently selected shape GameObject.</returns>
    public GameObject GetSelectedShape() {
        if(currentShapeIndex == 1) currentShape.transform.localScale = Vector3.Scale(currentShape.transform.localScale, currentShapeScale);
        return currentShape;
    }

    /// <summary>
    /// Sets the shape that can currently be moved by the user.
    /// 
    /// Assigns the specified GameObject as the movable shape, allowing movement operations to be performed on it.
    /// </summary>
    /// <param name="shape">The GameObject to set as the movable shape.</param>
    public void SetMoveableShape(GameObject shape) {
        movableShape = shape;
    }

    /// <summary>
    /// Returns the shape that can currently be moved by the user.
    /// 
    /// Retrieves the GameObject assigned as the movable shape, allowing movement operations to be performed on it.
    /// </summary>
    /// <returns>The GameObject that can currently be moved by the user.</returns>
    public GameObject GetMoveableShape()
    {
        return movableShape;
    }

    /// <summary>
    /// Toggles the additive/subtractive mode of the currently selected shape.
    /// 
    /// If the manager is in the selected state, switches the mode of the current shape between additive and subtractive.
    /// Used to change how the shape interacts in masking operations.
    /// </summary>
    public void ChangeShapeMode()
    {
        if (state != ShapeState.selected) return;

        var shapeScript = currentShape.GetComponent<Shape>();
        if (shapeScript.isAdditive())
        {
            shapeScript.SetAdditive(false);
        }
        else
        {
            shapeScript.SetAdditive(true);
        }
    }

    /// <summary>
    /// Applies the mask to the volume dataset using the active shapes.
    /// 
    /// Iterates through all active shapes and applies either additive or subtractive masking to the volume dataset,
    /// depending on the specified mode. For each shape, disables it, then re-enables and checks if it matches the
    /// desired additive/subtractive mode. Calculates the bounding box and iterates through its volume, transforming
    /// each point to match the shape's orientation. If a point is inside the shape, paints the mask at that position.
    /// Records painted shapes for undo functionality and pushes paint actions to the undo stack if additive mode is used.
    /// Consolidates mask entries after processing.
    /// </summary>
    /// <param name="_activeDataSet">The VolumeDataSetRenderer to apply the mask to.</param>
    /// <param name="_volumeInputController">The VolumeInputController providing source information.</param>
    /// <param name="additive">True to apply additive masking, false for subtractive.</param>
    /// <param name="undo">True if this operation is part of an undo action.</param>
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

    /// <summary>
    /// Calculates the world-space centre of a shape's mesh.
    /// 
    /// Iterates through all vertices of the shape's mesh, transforms each to world space,
    /// and computes the average position to determine the centroid.
    /// </summary>
    /// <param name="shape">The GameObject whose mesh centre is to be calculated.</param>
    /// <returns>The world-space centre (centroid) of the shape's mesh.</returns>
    public Vector3 findCentre(GameObject shape) {
        Vector3 centre = new Vector3(0,0,0);
        Vector3[] vertices = shape.GetComponent<MeshFilter>().mesh.vertices;
        foreach(Vector3 vertex in vertices) {
            centre += shape.transform.TransformPoint(vertex);
        }
        return centre/vertices.Length;
    }

    /// <summary>
    /// Reverses the most recent shape-related action performed by the user.
    /// </summary>
    /// <remarks>
    /// This method implements an undo system for shape manipulation actions.  
    /// It removes the most recent <see cref="ShapeAction"/> from the action stack 
    /// and reverts its effects depending on the action type:
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// <see cref="ShapeAction.ActionType.AddShape"/> — Removes the most recently added shape 
    /// from <c>activeShapes</c> and <c>selectedShapes</c>, then destroys it.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <see cref="ShapeAction.ActionType.DeleteShapes"/> — Restores previously deleted shapes 
    /// by reactivating them, adding them back to <c>activeShapes</c> and <c>selectedShapes</c>,
    /// and marking them as selected.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <see cref="ShapeAction.ActionType.CopyShapes"/> — Removes shapes that were created by a copy action 
    /// and destroys them.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <see cref="ShapeAction.ActionType.Paint"/> — Reverts a paint operation by reapplying the 
    /// previous mask state and resetting <c>activeShapes</c>.
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Undo the last shape manipulation action
    /// shapeController.Undo();
    /// </code>
    /// </example>
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

    /// <summary>
    /// Destroys the currently active shape in the scene.
    /// </summary>
    /// <remarks>
    /// This method retrieves the <see cref="Shape"/> component attached to 
    /// <c>currentShape</c> and calls its <c>DestroyShape()</c> method to 
    /// remove it from the scene.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Destroy the shape the user is currently working on
    /// shapeController.DestroyCurrentShape();
    /// </code>
    /// </example>
    public void DestroyCurrentShape()
    {
        Shape shapeScript = currentShape.GetComponent<Shape>();
        shapeScript.DestroyShape();
    }


    /// <summary>
    /// Clears all active and selected shapes from memory.
    /// </summary>
    /// <remarks>
    /// This method resets the <c>activeShapes</c> and <c>selectedShapes</c> lists 
    /// by creating new empty lists. It does not destroy the shapes in the scene.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Remove all shape references without destroying them
    /// shapeController.ClearShapes();
    /// </code>
    /// </example>
    public void ClearShapes()
    {
        activeShapes = new List<GameObject>();
        selectedShapes = new List<GameObject>();
    }

    /// <summary>
    /// Clears the list of painted shapes.
    /// </summary>
    /// <remarks>
    /// This method resets <c>paintedShapes</c> to a new empty list.  
    /// It does not affect the shapes themselves in the scene.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Clear the painted shape list before starting a new paint operation
    /// shapeController.ClearPaintedShapes();
    /// </code>
    /// </example>
    public void ClearPaintedShapes()
    {
        paintedShapes = new List<GameObject>();
    }

    /// <summary>
    /// Destroys all shapes managed by the controller and resets internal state.
    /// </summary>
    /// <remarks>
    /// This method destroys every <see cref="Shape"/> object in both 
    /// <c>activeShapes</c> and <c>deletedShapes</c>, clears the undo 
    /// <c>actions</c> stack, and resets all shape-related lists and counters.  
    /// It effectively returns the shape system to a clean initial state.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Completely reset the shape controller
    /// shapeController.DestroyShapes();
    /// </code>
    /// </example>
    public void DestroyShapes()
    {
        foreach (GameObject shape in activeShapes)
        {
            if (shape)
            {
                Shape shapeScript = shape.GetComponent<Shape>();
                shapeScript.DestroyShape();
            }
        }
        foreach (GameObject shape in deletedShapes)
        {
            if (shape)
            {
                Shape shapeScript = shape.GetComponent<Shape>();
                shapeScript.DestroyShape();
            }

        }
        actions.Clear();
        ClearShapes();
        deletedShapes = new List<GameObject>();
        ClearPaintedShapes();
        shapesCount = new int[] { 0, 0, 0, 0 };
    }
}