using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Abstract_VR_button : MonoBehaviour {
    public KeyboardManager keyboardManager;

    public Material[] materials;    //this is to switch the color of the button while it is aimed by the ray
    public float keepSelected = 0;
    public int materialIndex = 0;
    protected Renderer renderer;

    protected void Start()
    {
        keyboardManager = GameObject.FindGameObjectWithTag("VRKeyboard").GetComponent<KeyboardManager>();
        renderer = GetComponent<Renderer>();
        renderer.enabled = true;
        renderer.sharedMaterial = materials[materialIndex];
    }

    protected void Update()
    {
        if (keepSelected > 0)
        {
            //switch material if it was not done before
            if (materialIndex == 0)
            {
                materialIndex = 1;
                renderer.sharedMaterial = materials[materialIndex];
            }
            keepSelected -= Time.deltaTime;
        }
        else if (materialIndex == 1)
        {
            materialIndex = 0;
            renderer.sharedMaterial = materials[materialIndex];
        }
    }

    abstract public void onPress();

}
