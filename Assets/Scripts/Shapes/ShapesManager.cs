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
    /// Represents the current state of the shape interaction.
    /// </summary>
    public enum ShapeState {
        /// <summary>Default state when no interaction is occurring.</summary>
        idle,
        /// <summary>Default state when no interaction is occurring.</summary>
        selecting,
        /// <summary>State when a shape has been selected.</summary>
        selected
    };
    public ShapeState state;

    /// <summary>
    /// Initializes the shape system by setting up default values, the shape array, and internal state.
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
    /// Returns the currently selected shape from the shapes array.
    /// </summary>
    /// <returns>The <see cref="GameObject"/> representing the current shape.</returns>
    public GameObject GetCurrentShape()
    {
        return shapes[currentShapeIndex];
    }

    /// <summary>
    /// Transitions the shape state from <c>selecting</c> to <c>selected</c>, 
    /// enables additive mode on the current shape, and flags the shape as selected.
    /// </summary>
    /// <remarks>
    /// This method is called when the user finalizes their shape selection.
    /// It will only execute if the current state is <see cref="ShapeState.selecting"/>.
    /// </remarks>
    public void SelectShape() {
        if(state != ShapeState.selecting) return;
        state = ShapeState.selected;
        var shapeScript = currentShape.GetComponent<Shape>();
        shapeScript.SetAdditive(true);
        shapeSelected = true;
    }

    /// <summary>
    /// Reverts the current shape from the <c>selected</c> state back to <c>selecting</c>,
    /// resets its color to the base color, and marks it as not selected.
    /// </summary>
    /// <remarks>
    /// This method only executes if the current state is <see cref="ShapeState.selected"/>.
    /// </remarks>
    public void DeselectShape() {
        if(state != ShapeState.selected) return;
        state = ShapeState.selecting;
        var renderer = currentShape.GetComponent<Renderer>();
        renderer.material.color = baseColour;
        shapeSelected = false;
    }

    /// <summary>
    /// Checks if a shape is currently selected.
    /// </summary>
    /// <returns><c>true</c> if a shape is selected; otherwise, <c>false</c>.</returns>
    public bool isShapeSelected()
    {
        return shapeSelected;
    }

    /// <summary>
    /// Checks if the shape system is currently in the <c>idle</c> state.
    /// </summary>
    /// <returns><c>true</c> if the state is <see cref="ShapeState.idle"/>; otherwise, <c>false</c>.</returns>
    public bool isIdle()
    {
        if (state == ShapeState.idle) return true;
        return false;
    }

    /// <summary>
    /// Sets the shape system state to <see cref="ShapeState.idle"/>.
    /// </summary>
    public void MakeIdle()
    {
        state = ShapeState.idle;
    }

    /// <summary>
    /// Cycles to the next shape in the shapes array while in the <c>selecting</c> state.
    /// Destroys the current shape before switching.
    /// </summary>
    /// <returns>
    /// The <see cref="GameObject"/> representing the next shape, or <c>null</c> if the state is not <see cref="ShapeState.selecting"/>.
    /// </returns>
    public GameObject GetNextShape()
    {
        if (state != ShapeState.selecting) return null;
        DestroyCurrentShape();
        currentShapeIndex++;
        if (currentShapeIndex == shapes.Length) currentShapeIndex = 0;
        return shapes[currentShapeIndex];
    }

    /// <summary>
    /// Cycles to the previous shape in the shapes array while in the <c>selecting</c> state.
    /// Destroys the current shape before switching.
    /// </summary>
    /// <returns>
    /// The <see cref="GameObject"/> representing the previous shape, or <c>null</c> if the state is not <see cref="ShapeState.selecting"/>.
    /// </returns>
    public GameObject GetPreviousShape()
    {
        if (state != ShapeState.selecting) return null;
        DestroyCurrentShape();
        currentShapeIndex -= 1;
        if (currentShapeIndex < 0) currentShapeIndex = shapes.Length - 1;
        return shapes[currentShapeIndex];
    }

    /// <summary>
    /// Adds the given shape to the list of active shapes and records the action for undo functionality.
    /// </summary>
    /// <param name="shape">The <see cref="GameObject"/> to add.</param>
    public void AddShape(GameObject shape)
    {
        activeShapes.Add(shape);
        actions.Push(new ShapeAction(shape));
    }

    /// <summary>
    /// Generates a unique name for the given shape by incrementing its type count.
    /// </summary>
    /// <param name="shape">The <see cref="GameObject"/> to name.</param>
    /// <returns>A unique string name based on the shape's original name and its type count.</returns>
    public string GetShapeName(GameObject shape) {
        shapesCount[currentShapeIndex]++;
        return shape.name + $"{shapesCount[currentShapeIndex]}";
    }

    /// <summary>
    /// Adds the given shape to the list of selected shapes.
    /// </summary>
    /// <param name="shape">The <see cref="GameObject"/> to mark as selected.</param>
    public void AddSelectedShape(GameObject shape)
    {
        selectedShapes.Add(shape);
    }

    /// <summary>
    /// Removes the given shape from the list of selected shapes.
    /// </summary>
    /// <param name="shape">The <see cref="GameObject"/> to unmark as selected.</param>
    public void RemoveSelectedShape(GameObject shape)
    {
        selectedShapes.Remove(shape);
    }

    /// <summary>
    /// Increases the scale of the current shape or all selected shapes based on their type.
    /// </summary>
    /// <remarks>
    /// - If the state is <see cref="ShapeState.selecting"/>, this method does nothing.
    /// - If the state is <see cref="ShapeState.selected"/>, only the current shape's scale is increased.
    /// - Otherwise, the scale of all shapes in <c>selectedShapes</c> is increased.
    /// - Cuboid shapes are scaled up by 0.01 units on each axis; other shapes are scaled by 0.001 units.
    /// </remarks>
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
    /// Decreases the scale of the current shape or all selected shapes based on their type,
    /// ensuring the scale does not drop below a minimum threshold.
    /// </summary>
    /// <remarks>
    /// - If the state is <see cref="ShapeState.selecting"/>, this method does nothing.
    /// - If the state is <see cref="ShapeState.selected"/>, only the current shape's scale is decreased.
    /// - Otherwise, the scale of all shapes in <c>selectedShapes</c> is decreased.
    /// - Cuboid shapes are scaled down by 0.01 units on each axis; other shapes by 0.001 units.
    /// - The scale is clamped to a minimum of (0.001, 0.001, 0.001) to prevent disappearing or negative scale.
    /// </remarks>
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
    /// Marks all selected shapes for deletion by removing them from the active shapes list,
    /// deactivating their GameObjects, and adding them to the deleted shapes list for later cleanup.
    /// </summary>
    /// <remarks>
    /// Shapes are only fully deleted upon exiting shape mode.
    /// Records the deletion action for undo functionality.
    /// </remarks>
    public void DeleteSelectedShapes()
    {
        List<GameObject> delShapes = new List<GameObject>();
        foreach (GameObject shape in selectedShapes)
        {
            activeShapes.Remove(shape);
            shape.SetActive(false);
            deletedShapes.Add(shape);
            delShapes.Add(shape);
        }
        actions.Push(new ShapeAction(ShapeAction.ActionType.DeleteShapes, delShapes));
        selectedShapes = new List<GameObject>();
    }

    /// <summary>
    /// Changes the mode of the currently selected shape or toggles the mode of all selected shapes.
    /// </summary>
    /// <remarks>
    /// - If the current state is <see cref="ShapeState.selected"/>, only the current shape's mode is changed.
    /// - Otherwise, all shapes in <c>selectedShapes</c> have their additive mode toggled.
    /// </remarks>
    public void ChangeModes()
    {
        if (state == ShapeState.selected)
        {
            ChangeShapeMode();
            return;
        }
        foreach (GameObject shape in selectedShapes)
        {
            Shape shapeScript = shape.GetComponent<Shape>();
            shapeScript.SetAdditive(!shapeScript.isAdditive());
        }
    }

    /// <summary>
    /// Copies the currently selected shapes by instantiating duplicates slightly offset in position,
    /// adding them to active shapes and selection, and recording the copy action.
    /// </summary>
    /// <remarks>
    /// - If <c>paintedShapes</c> contains shapes, they are reactivated and reselected instead of creating new copies.
    /// - Each copied shape inherits the position, rotation, scale, and additive state of the original.
    /// - Copies are offset slightly upward (by 0.1 units on the Y axis) to distinguish them visually.
    /// </remarks>
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

    /// <summary>
    /// Sets the specified shape as the current selectable shape in the <c>selected</c> state.
    /// </summary>
    /// <param name="shape">The <see cref="GameObject"/> to set as the current selectable shape.</param>
    public void SetSelectableShape(GameObject shape) {
        currentShape = shape;
        currentShapeScale = shape.transform.localScale;
    }

    /// <summary>
    /// Returns the currently selected shape, applying a scale adjustment if the current shape index is 1.
    /// </summary>
    /// <returns>The <see cref="GameObject"/> representing the currently selected shape.</returns>
    public GameObject GetSelectedShape()
    {
        if (currentShapeIndex == 1) currentShape.transform.localScale = Vector3.Scale(currentShape.transform.localScale, currentShapeScale);
        return currentShape;
    }

    /// <summary>
    /// Sets the shape that is currently movable, ensuring only one shape can be moved at a time.
    /// </summary>
    /// <param name="shape">The <see cref="GameObject"/> to set as movable.</param>
    public void SetMoveableShape(GameObject shape) {
        movableShape = shape;
    }

    /// <summary>
    /// Gets the shape that is currently movable.
    /// </summary>
    /// <returns>The <see cref="GameObject"/> representing the movable shape.</returns>
    public GameObject GetMoveableShape()
    {
        return movableShape;
    }

    /// <summary>
    /// Toggles the additive mode of the current shape if it is in the <c>selected</c> state.
    /// </summary>
    /// <remarks>
    /// Does nothing if the state is not <see cref="ShapeState.selected"/>.
    /// </remarks>
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
    /// Applies the selection of shapes to generate a mask on the active volume dataset.
    /// </summary>
    /// <param name="_activeDataSet">The volume dataset renderer on which to apply the mask.</param>
    /// <param name="_volumeInputController">Controller providing input context, such as source ID.</param>
    /// <param name="additive">
    /// Determines if the mask should be applied to additive shapes (<c>true</c>) or subtractive shapes (<c>false</c>).
    /// </param>
    /// <param name="undo">
    /// Indicates if this operation is part of an undo action (<c>true</c>) or a new application (<c>false</c>).
    /// </param>
    /// <remarks>
    /// - Deactivates all shapes initially, then selectively activates shapes matching the additive parameter.
    /// - For each relevant shape, calculates its bounding box aligned to the dataset axes and iterates through the volume within this bounding box.
    /// - Checks if each point inside the bounding box lies within the shape volume using <c>insideShape</c>.
    /// - Depending on <c>additive</c>, paints or erases the mask at points inside the shape.
    /// - Calls <c>UpdateStats</c> to refresh dataset statistics based on the source ID.
    /// - If not undoing, adds the shape to <c>paintedShapes</c>.
    /// - For additive shapes, creates a hidden copy marked subtractive to support undo functionality.
    /// - Pushes a <c>Paint</c> action with undo shapes for undo management.
    /// - Consolidates the mask entries after processing all shapes.
    /// </remarks>
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
    /// Calculates the geometric center (centroid) of the given shape's mesh vertices in world space.
    /// </summary>
    /// <param name="shape">The <see cref="GameObject"/> whose mesh center is to be found.</param>
    /// <returns>
    /// The <see cref="Vector3"/> position representing the average (centroid) of all mesh vertices transformed to world coordinates.
    /// </returns>
    public Vector3 findCentre(GameObject shape) {
        Vector3 centre = new Vector3(0,0,0);
        Vector3[] vertices = shape.GetComponent<MeshFilter>().mesh.vertices;
        foreach(Vector3 vertex in vertices) {
            centre += shape.transform.TransformPoint(vertex);
        }
        return centre/vertices.Length;
    }

    /// <summary>
    /// Undoes the last user action by reverting changes based on the action type.
    /// </summary>
    /// <remarks>
    /// Supports undo for the following action types:
    /// <list type="bullet">
    /// <item><description><see cref="ShapeAction.ActionType.AddShape"/>: Removes the added shape from active and selected lists and destroys it.</description></item>
    /// <item><description><see cref="ShapeAction.ActionType.DeleteShapes"/>: Reactivates deleted shapes, adds them back to active and selected lists, and marks them selected.</description></item>
    /// <item><description><see cref="ShapeAction.ActionType.CopyShapes"/>: Removes copied shapes from active and selected lists and destroys them.</description></item>
    /// <item><description><see cref="ShapeAction.ActionType.Paint"/>: Reactivates painted shapes, reapplies mask in undo mode, then deactivates shapes and clears active shapes list.</description></item>
    /// </list>
    /// </remarks>
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
    /// Destroys the current shape by calling its <c>DestroyShape</c> method.
    /// </summary>
    public void DestroyCurrentShape()
    {
        Shape shapeScript = currentShape.GetComponent<Shape>();
        shapeScript.DestroyShape();
    }

    /// <summary>
    /// Clears the lists of active and selected shapes.
    /// </summary>
    public void ClearShapes()
    {
        activeShapes = new List<GameObject>();
        selectedShapes = new List<GameObject>();
    }

    /// <summary>
    /// Clears the list of painted shapes.
    /// </summary>
    public void ClearPaintedShapes()
    {
        paintedShapes = new List<GameObject>();
    }

    /// <summary>
    /// Destroys all shapes in the active and deleted shape lists by calling their <c>DestroyShape</c> methods,
    /// then clears all related lists and resets the shape counters and action stack.
    /// </summary>
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