using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VolumeData;

public class QuickMenuController : MonoBehaviour
{

    public GameObject volumeDatasetRendererObj = null;
    public GameObject notificationText = null;

    private VolumeDataSetRenderer _activeDataSet;
    private VolumeDataSetRenderer[] _dataSets;

    public GameObject mainMenuCanvas;
    int maskstatus=0;
    // Start is called before the first frame update
    void Start()
    {
       
        if ( volumeDatasetRendererObj!= null )
            _dataSets = volumeDatasetRendererObj.GetComponentsInChildren<VolumeDataSetRenderer>(true);

   

    }

    // Update is called once per frame
    void Update()
    {

        if (_dataSets!= null)
            Debug.Log("_dataSets size " + _dataSets.Length);
       

        var firstActive = getFirstActiveDataSet();
        if (firstActive && _activeDataSet != firstActive)
        {
            Debug.Log("in foreach --- Update");
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



    public void Exit()
    {
        Application.Quit();
    }

    public void OpenMainMenu()
    {
        mainMenuCanvas.SetActive(!mainMenuCanvas.activeSelf);
    }

    public void ToggleMask()
    {
        if (maskstatus == 2)
            maskstatus = -1;
        maskstatus++;
        
        switch (maskstatus)
        {
            case 0:
                setMask(MaskMode.Disabled);
                notificationText.GetComponent<Text>().text = "Mask disabled";
                break;
            case 1: 
                setMask(MaskMode.Enabled);
                notificationText.GetComponent<Text>().text = "Mask enabled";
                break;
            case 2:
                setMask(MaskMode.Inverted);
                notificationText.GetComponent<Text>().text = "Mask inverted";
                break;
        }
        
    }

    private void setMask(MaskMode mode)
    {
        Debug.Log("set");
        if (_activeDataSet)
        {
            Debug.Log("attive");
            _activeDataSet.MaskMode = mode;
        }
    }
}
