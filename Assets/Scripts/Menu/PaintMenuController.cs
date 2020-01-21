using DataFeatures;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VolumeData;

public class PaintMenuController : MonoBehaviour
{

    public GameObject volumeDatasetRendererObj = null;
    public GameObject notificationText = null;

    private VolumeDataSetRenderer _activeDataSet;
    private VolumeDataSetRenderer[] _dataSets;

    public GameObject mainMenuCanvas;
    int maskstatus=0;
    int cropstatus = 0;
    int featureStatus = 0;
    // Start is called before the first frame update
    void Start()
    {
       
        if ( volumeDatasetRendererObj!= null )
            _dataSets = volumeDatasetRendererObj.GetComponentsInChildren<VolumeDataSetRenderer>(true);

   

    }

    // Update is called once per frame
    void Update()
    {

        if (_dataSets != null)
        {
           
        }

        var firstActive = getFirstActiveDataSet();
        if (firstActive && _activeDataSet != firstActive)
        {
           // Debug.Log("in foreach --- Update");
            _activeDataSet = firstActive;
        }
       
    }

    private VolumeDataSetRenderer getFirstActiveDataSet()
    {

        foreach (var dataSet in _dataSets)
        {
        
            if (dataSet.isActiveAndEnabled)
            {
                return dataSet;
            }
        }
        return null;

    }



    public void ExitPaintMode()
    {
        this.gameObject.SetActive(false);
    }

}
