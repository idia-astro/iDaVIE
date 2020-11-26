using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum SourceMappingOptions
{
    none, Name, X, Y, Z, Ra, Dec, Freq, Velo, Redshift, Xmin, Xmax, Ymin, Ymax, Zmin, Zmax
}


public class SourceRow : MonoBehaviour
{

    public string SourceName;
    public int SourceIndex;
    public SourceMappingOptions CurrentMapping = SourceMappingOptions.none;
    public CanvassDesktop CanvassDesktopParent;


    // Start is called before the first frame update
    void Start()
    {
        CanvassDesktopParent = GetComponentInParent<CanvassDesktop>();
        //var name = transform.Find("Source_name").gameObject.GetComponent<TextMeshProUGUI>();
        //var name = transform.Find("Source_name").gameObject.GetComponent<TextMeshProUGUI>();
        //SourceName = name.text;
        PopulateDropDown();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void PopulateDropDown()
    {
        string[] optionNames = System.Enum.GetNames(typeof(SourceMappingOptions));
        optionNames[0] = "";
        //List<string> names =
        var dd = transform.Find("Coord_dropdown").gameObject.GetComponent<TMP_Dropdown>();
        dd.ClearOptions();
        dd.AddOptions(new List<string>(optionNames));
        dd.RefreshShownValue();
    }

    public void MapCoordInParent(int coord)
    {
        string[] optionNames = System.Enum.GetNames(typeof(SourceMappingOptions));
        CurrentMapping = (SourceMappingOptions) coord;
        CanvassDesktopParent.ChangeSourceMapping(SourceIndex, CurrentMapping);
        
        //var dd = transform.Find("Coord_dropdown").gameObject.GetComponent<Dropdown>();
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
