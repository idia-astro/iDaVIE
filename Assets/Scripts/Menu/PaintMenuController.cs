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

    private VolumeInputController _volumeInputController;

    // Start is called before the first frame update
    void Start()
    {
        _volumeInputController = FindObjectOfType<VolumeInputController>();
       /*
        if ( volumeDatasetRendererObj!= null )
            _dataSets = volumeDatasetRendererObj.GetComponentsInChildren<VolumeDataSetRenderer>(true);
            */
        _volumeInputController.SetInteractionState(VolumeInputController.InteractionState.PaintMode);
   
    

    }

    // Update is called once per frame
    void Update()
    {
        /*
        if (_dataSets != null)
        {
           
        }

        var firstActive = getFirstActiveDataSet();
        if (firstActive && _activeDataSet != firstActive)
        {
           // Debug.Log("in foreach --- Update");
            _activeDataSet = firstActive;
        }
        */
       
    }
    /*
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
    */


    public void ExitPaintMode()
    {
        _volumeInputController.SetInteractionState(VolumeInputController.InteractionState.SelectionMode);
        this.gameObject.SetActive(false);
    }

}
