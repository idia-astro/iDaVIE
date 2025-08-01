/*
 * Shape class, used to manage the state of individual shapes used in shape selection
*/

using UnityEngine;

/// <summary>
/// Represents a 3D shape that can be selected, highlighted, moved, and toggled between additive and subtractive states.
/// Interaction is managed through trigger collisions and controller ray input.
/// </summary>
public class Shape : MonoBehaviour
{
    /// <summary>
    /// Indicates whether this shape is additive (true) or subtractive (false).
    /// </summary>
    private bool additive;

    /// <summary>
    /// Highlight color when the additive shape is selected.
    /// </summary>
    private Color highlightAdditiveColor = Color.green;

    /// <summary>
    /// Highlight color when the subtractive shape is selected.
    /// </summary>
    private Color highlighSubtractiveColor = Color.red;

    /// <summary>
    /// Base color when the additive shape is not selected.
    /// </summary>
    private Color baseAdditiveColor = new Color(0.6773301f, 0.8490566f, 0.2923638f);

    /// <summary>
    /// Base color when the subtractive shape is not selected.
    /// </summary>
    private Color baseSubtractiveColor = new Color(0.8509804f, 0.4262924f, 0.2941177f);

    /// <summary>
    /// Cached reference to the shape's Renderer component.
    /// </summary>
    private Renderer rend;

    /// <summary>
    /// Reference to the volume input controller used for interaction logic.
    /// </summary>
    private VolumeInputController _volumeInputController;

    /// <summary>
    /// Reference to the shape manager responsible for tracking selected/moveable shapes.
    /// </summary>
    private ShapesManager _shapeManager;

    /// <summary>
    /// Whether this shape is currently selected.
    /// </summary>
    private bool selected;

    /// <summary>
    /// Whether this shape was previously selected (used for toggle logic).
    /// </summary>
    private bool previouslySelected;

    /// <summary>
    /// Called when the object becomes enabled and active.
    /// Initializes references to controllers and marks the shape as selected.
    /// </summary>
    void OnEnable()
    {
        rend = GetComponent<Renderer>();
        selected = true;

        if (_volumeInputController == null)
            _volumeInputController = FindObjectOfType<VolumeInputController>();

        if (_shapeManager == null)
            _shapeManager = FindObjectOfType<ShapesManager>();
    }

    /// <summary>
    /// Called when another collider enters this shape's trigger.
    /// Highlights the shape and makes it moveable if it’s not already selected.
    /// </summary>
    /// <param name="other">The collider that entered the trigger.</param>
    void OnTriggerEnter(Collider other)
    {
        if (_shapeManager.GetMoveableShape() != null) return;
        if (selected) previouslySelected = true;
        if (additive)
        {
            rend.material.color = highlightAdditiveColor;
        }
        else
        {
            rend.material.color = highlighSubtractiveColor;
        }
        selected = true;
        _shapeManager.SetMoveableShape(gameObject);
        if (!previouslySelected) _shapeManager.AddSelectedShape(gameObject);
    }

    /// <summary>
    /// Called when another collider exits this shape's trigger.
    /// Resets the shape’s color and deselects it if appropriate.
    /// </summary>
    /// <param name="other">The collider that exited the trigger.</param>
    void OnTriggerExit(Collider other)
    {
        if (_shapeManager.GetMoveableShape() != gameObject) return;
        if (previouslySelected)
        {
            _shapeManager.SetMoveableShape(null);
            return;
        }
        if (additive)
        {
            rend.material.color = baseAdditiveColor;
        }
        else
        {
            rend.material.color = baseSubtractiveColor;
        }
        selected = false;
        _shapeManager.RemoveSelectedShape(gameObject);
        _shapeManager.SetMoveableShape(null);
    }

    /// <summary>
    /// Sets whether the shape is additive or subtractive,
    /// and updates the material color based on selection and state.
    /// </summary>
    /// <param name="isAdditive">True for additive, false for subtractive.</param>
    public void SetAdditive(bool isAdditive)
    {
        rend = GetComponent<Renderer>();
        if (selected)
        {
            if (isAdditive)
            {
                rend.material.color = highlightAdditiveColor;
            }
            else
            {
                rend.material.color = highlighSubtractiveColor;
            }
        }
        else
        {
            if (isAdditive)
            {
                rend.material.color = baseAdditiveColor;
            }
            else
            {
                rend.material.color = baseSubtractiveColor;
            }
        }
        additive = isAdditive;
    }

    /// <summary>
    /// Toggles selection state of the shape when clicked by a controller ray.
    /// Updates color and notifies the shape manager.
    /// </summary>
    public void ShapeClicked()
    {
        if (!selected)
        {
            if (additive)
            {
                rend.material.color = highlightAdditiveColor;
            }
            else
            {
                rend.material.color = highlighSubtractiveColor;
            }
            selected = true;
            _shapeManager.AddSelectedShape(gameObject);
        }
        else
        {
            if (additive)
            {
                rend.material.color = baseAdditiveColor;
            }
            else
            {
                rend.material.color = baseSubtractiveColor;
            }
            selected = false;
            previouslySelected = false;
            _shapeManager.RemoveSelectedShape(gameObject);
        }

    }

    /// <summary>
    /// Returns whether this shape is currently in the additive state.
    /// </summary>
    /// <returns>True if additive, false if subtractive.</returns>
    public bool isAdditive()
    {
        return additive;
    }

    /// <summary>
    /// Sets whether the shape is selected. Does not trigger color changes.
    /// </summary>
    /// <param name="isSelected">True to mark as selected, false otherwise.</param>
    public void SetSelected(bool isSelected)
    {
        selected = isSelected;
    }

    /// <summary>
    /// Destroys this shape from the scene.
    /// </summary>
    public void DestroyShape()
    {
        Destroy(gameObject);
    }
}