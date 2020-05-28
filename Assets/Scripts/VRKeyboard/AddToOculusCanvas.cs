using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddToOculusCanvas : MonoBehaviour {

	// Use this for initialization
	void Start () {
        StartCoroutine("addToOculusCanvas");
	}
	

    private IEnumerator addToOculusCanvas()
    {
        while (GameObject.Find("Canvas_Oculus") == null)
        {
            yield return new WaitForEndOfFrame();
        }
            transform.parent = GameObject.Find("Canvas_Oculus").transform;
            transform.localPosition = new Vector3(-25, -350, -175);
            transform.rotation = Quaternion.Euler(0, 0, 0);
            transform.localScale = new Vector3(40, 40, 1);
    }
	
}
