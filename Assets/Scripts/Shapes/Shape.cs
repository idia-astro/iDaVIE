/*
 * Shape class, used to manage the state of individual shapes used in shape selection
*/

using UnityEngine;

public class Shape : MonoBehaviour {
    private bool additive;
    private Color highlightAdditiveColor = Color.green; 
    private Color highlighSubtractiveColor = Color.red;
    private Color baseAdditiveColor = new Color(0.6773301f, 0.8490566f, 0.2923638f);
    private Color baseSubtractiveColor = new Color(0.8509804f, 0.4262924f, 0.2941177f);
    private Renderer rend;
    private VolumeInputController _volumeInputController;
    private ShapesManager _shapeManager;
    private bool selected;
    private bool previouslySelected;


    void OnEnable()
    {
        rend = GetComponent<Renderer>();
        selected = true;
        
        if (_volumeInputController == null)
            _volumeInputController = FindObjectOfType<VolumeInputController>(); 

        if(_shapeManager == null)
            _shapeManager = FindObjectOfType<ShapesManager>(); 
    }

    //The following two functions check for collisions of the users hand entering the shape to allow the user to move the shape when inside
    void OnTriggerEnter(Collider other)
    {
        if(_shapeManager.GetMoveableShape() != null) return;
        if(selected) previouslySelected = true;
        if(additive)
        {
            rend.material.color = highlightAdditiveColor;
        }
        else{
            rend.material.color = highlighSubtractiveColor;
        }
        selected = true;
        _shapeManager.SetMoveableShape(gameObject);
        if(!previouslySelected) _shapeManager.AddSelectedShape(gameObject);
    }

    void OnTriggerExit(Collider other)
    {
        if(_shapeManager.GetMoveableShape() != gameObject) return;
        if(previouslySelected) {
            _shapeManager.SetMoveableShape(null);
            return;
        }
        if(additive)
        {
            rend.material.color = baseAdditiveColor; 
        }
        else{
            rend.material.color = baseSubtractiveColor;
        }
        selected = false;
        _shapeManager.RemoveSelectedShape(gameObject);
        _shapeManager.SetMoveableShape(null); 
    }
    
    //Change the state of a shape and the relevant colour of the shape
    public void SetAdditive(bool isAdditive) {
        rend = GetComponent<Renderer>();
        if(selected){
            if(isAdditive)
            {
                rend.material.color = highlightAdditiveColor;
            }
            else{
                rend.material.color = highlighSubtractiveColor;
            }
        }
        else {
            if(isAdditive)
            {
                rend.material.color = baseAdditiveColor;
            }
            else{
                rend.material.color = baseSubtractiveColor;
            }
        }
        additive = isAdditive;
    }

    //This function is called when a shape is selected with the ray from a controller
    public void ShapeClicked() {
        if(!selected){
            if(additive)
            {
                rend.material.color = highlightAdditiveColor;
            }
            else{
                rend.material.color = highlighSubtractiveColor;
            }
            selected = true;
            _shapeManager.AddSelectedShape(gameObject);
        }
        else {
            if(additive)
            {
                rend.material.color = baseAdditiveColor;
            }
            else{
                rend.material.color = baseSubtractiveColor;
            }
            selected = false;
            previouslySelected = false;
            _shapeManager.RemoveSelectedShape(gameObject);
        }
        
    }

    public bool isAdditive() {
        return additive;
    }

    public void SetSelected(bool isSelected) {
        selected = isSelected;
    }


    public void DestroyShape() {
        Destroy(gameObject);
    }
}