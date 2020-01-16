using System.IO;
using CatalogData;
using UnityEngine;
using UnityEngine.UI;

public class RenderMenu : MonoBehaviour
{
 /*
    public CatalogDataSetManager CatalogDataSetManager;

    // Data Set
    public Button ButtonPrevDataSet;
    public Button ButtonNextDataSet;
    public Text LabelDataSet;

    // Color Map
    public Button ButtonPrevColorMap;
    public Button ButtonNextColorMap;
    public Text LabelColormap;

    // Use this for initialization
    void Start()
    {
        
        ButtonPrevDataSet.onClick.AddListener(OnPrevDataSetClicked);
        ButtonNextDataSet.onClick.AddListener(OnNextDataSetClicked);
        ButtonNextColorMap.onClick.AddListener(OnPrevColorMapClicked);
        ButtonPrevColorMap.onClick.AddListener(OnNextColorMapClicked);
        var activeDataSet = CatalogDataSetManager.ActiveDataSet;
        if (activeDataSet != null)
        {
            LabelColormap.text = activeDataSet.DataMapping.ColorMap.ToString();
            LabelDataSet.text = Path.GetFileName(activeDataSet.TableFileName);
        }

        CatalogDataSetManager.OnActiveDataSetChanged += HandleDataSetChanged;
        CatalogDataSetManager.OnColorMapChanged += HandleColorMapChanged;
        
    }

    private void OnDestroy()
    {
        // Clean up delegates for data set manager
        CatalogDataSetManager.OnActiveDataSetChanged -= HandleDataSetChanged;
        CatalogDataSetManager.OnColorMapChanged -= HandleColorMapChanged;
        // Clean up button events
        ButtonPrevDataSet.onClick.RemoveAllListeners();
        ButtonNextDataSet.onClick.RemoveAllListeners();
        ButtonNextColorMap.onClick.RemoveAllListeners();
        ButtonPrevColorMap.onClick.RemoveAllListeners();
    }

    private void HandleDataSetChanged(CatalogDataSetRenderer dataSet)
    {
        LabelDataSet.text = Path.GetFileName(dataSet.TableFileName);
        LabelColormap.text = dataSet.DataMapping.ColorMap.ToString();
    }

    private void HandleColorMapChanged(ColorMapEnum colorMap)
    {
        LabelColormap.text = colorMap.ToString();
    }

    private void OnPrevDataSetClicked()
    {
        CatalogDataSetManager.SelectNextSet();
    }

    private void OnNextDataSetClicked()
    {
        CatalogDataSetManager.SelectPreviousSet();
    }

    private void OnNextColorMapClicked()
    {
        CatalogDataSetManager.ShiftColorMap(1);
    }

    private void OnPrevColorMapClicked()
    {
        CatalogDataSetManager.ShiftColorMap(-1);
    }
*/
    
}