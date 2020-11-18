using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SourceRow : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void MapCoordInParent(int coord)
    {
        
        var dd = transform.Find("Coord_dropdown").gameObject.GetComponent<Dropdown>();
        /*
        Transform t = transform;
        GameObject canvasDesktop = null;
        while (t.parent != null)
        {
        if (t.parent.name == "CanvassDesktop")
        {
            canvasDesktop = t.parent.gameObject;
            break;
        }
        t = t.parent.transform;
        }
        */
        //if (canvasDesktop != null)
        //GetComponentInParent<CanvassDesktop>().MapCoord(transform.Find("Name").GetComponent<TextMeshProUGUI>().text, dd.options[coord].text);
    }
}
