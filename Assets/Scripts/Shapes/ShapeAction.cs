/*
 * This class just stores information about actions performed by the user 
 * in shape mode to allow for undo functionality
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Represents an action performed on one or more shapes in the scene,
/// such as adding, copying, deleting, or painting.
/// Used for tracking user operations (e.g., for undo/redo functionality).
/// </summary>
public class ShapeAction
{
    /// <summary>
    /// The shape that was added. Only used when <see cref="type"/> is <c>AddShape</c>.
    /// </summary>
    public GameObject addedShape;

    /// <summary>
    /// The list of shapes affected by the action (e.g., copied, deleted, or painted).
    /// </summary>
    public List<GameObject> shapeList = new List<GameObject>();

    /// <summary>
    /// The available types of shape actions.
    /// </summary>
    public enum ActionType {
        /// <summary>
        /// The available types of shape actions.
        /// </summary>
        AddShape,
        /// <summary>
        /// One or more shapes were copied.
        /// </summary>
        CopyShapes,
        /// <summary>
        /// One or more shapes were deleted.
        /// </summary>
        DeleteShapes,
        /// <summary>
        /// Shapes were painted or recolored.
        /// </summary>
        Paint
    };

    /// <summary>
    /// The type of action that was performed.
    /// </summary>
    public ActionType type;

    /// <summary>
    /// Constructs a new <see cref="ShapeAction"/> for a single shape addition.
    /// </summary>
    /// <param name="shape">The shape that was added.</param>
    public ShapeAction(GameObject shape)
    {
        type = ActionType.AddShape;
        addedShape = shape;
    }

    /// <summary>
    /// Constructs a new <see cref="ShapeAction"/> for a list-based action,
    /// such as copying, deleting, or painting multiple shapes.
    /// </summary>
    /// <param name="actionType">The type of action being recorded.</param>
    /// <param name="shapes">The shapes affected by the action.</param>
    public ShapeAction(ActionType actionType, List<GameObject> shapes)
    {
        type = actionType;
        shapeList = shapes;
    }

}