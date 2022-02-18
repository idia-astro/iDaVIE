using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum SourceMappingOptions
{
    none, ID, X, Y, Z, Ra, Dec, Freq, Velo, Redshift, Xmin, Xmax, Ymin, Ymax, Zmin, Zmax
}

public class SourceRow : MonoBehaviour
{

    public string SourceName;
    public int SourceIndex;
    public SourceMappingOptions CurrentMapping = SourceMappingOptions.none;
    public CanvassDesktop CanvassDesktopParent;

    void Start()
    {
        CanvassDesktopParent = GetComponentInParent<CanvassDesktop>();
        PopulateDropDown();
    }

    void PopulateDropDown()
    {
        string[] optionNames = System.Enum.GetNames(typeof(SourceMappingOptions));
        optionNames[0] = "";
        var dropdown = transform.Find("Coord_dropdown").gameObject.GetComponent<TMP_Dropdown>();
        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string>(optionNames));
        dropdown.RefreshShownValue();
    }

    public void MapCoordInParent(int coord)
    {
        string[] optionNames = System.Enum.GetNames(typeof(SourceMappingOptions));
        CurrentMapping = (SourceMappingOptions) coord;
        CanvassDesktopParent.ChangeSourceMapping(SourceIndex, CurrentMapping);        
    }
}
