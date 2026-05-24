using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VolumeData;

public class TestScript : MonoBehaviour
{
    public VolumeDataSetRenderer _activeDataSet = null;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void executeTestButton()
    {
        if (connectLinks())
        {
            Debug.LogError("Issue with connecting items to testing script.");
        }
        Debug.Log("Successfully linked VolumeDataSetRenderer object.");

        if (doTest())
        {
            Debug.LogError("Issue with performing test in testing script.");
        }
        Debug.Log("Successfully performed test.");
    }

    bool connectLinks()
    {
        Debug.Log("Attempting to link VolumeDataSetRenderer object.");
        _activeDataSet = getFirstActiveDataSet();
        
        if (_activeDataSet == null)
            return true;
        return false;
    }

    bool doTest()
    {
        Debug.Log("Attempting to save subcube as a whole, which should trigger correct function call.");
        _activeDataSet.SaveSubCube();
        return false;
    }

    

    private VolumeDataSetRenderer getFirstActiveDataSet()
    {
        VolumeDataSetRenderer[] _dataSets = null;
        GameObject VolumeDatasetRendererObj = GameObject.Find("VolumeDataSetManager");
        if (VolumeDatasetRendererObj != null)
            _dataSets = VolumeDatasetRendererObj.GetComponentsInChildren<VolumeDataSetRenderer>(true);
        if (_dataSets != null)
        {
            foreach (VolumeDataSetRenderer dataSet in _dataSets)
            {
                if (dataSet.gameObject.activeSelf)
                {
                    return dataSet;
                }
            }
        }
        return null;
    }
}
