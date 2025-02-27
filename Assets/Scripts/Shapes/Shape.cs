using UnityEngine;

public class Shape : MonoBehaviour {
    private bool additive;
    
    public void SetAdditive(bool isAdditive) {
        var renderer = gameObject.GetComponent<Renderer>();
        if(isAdditive) renderer.material.color = Color.green;
        else  renderer.material.color = Color.red;
        additive = isAdditive;
    }

    public bool isAdditive() {
        return additive;
    }


    public void DestroyShape() {
        Destroy(gameObject);
    }
}