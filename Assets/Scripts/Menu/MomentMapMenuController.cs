using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VolumeData;

public class MomentMapMenuController : MonoBehaviour
{
    public GameObject volumeDataSetManager;
    private VolumeDataSetRenderer[] dataSets;

    // Start is called before the first frame update
    void Start()
    {
        if (volumeDataSetManager != null)
        {
            dataSets = volumeDataSetManager.GetComponentsInChildren<VolumeDataSetRenderer>(true);

            if (getFirstActiveDataSet() != null)
            {
                VolumeDataSet dataSet = getFirstActiveDataSet().GetDataSet();

            }
        }
    }


    // Update is called once per frame
    void Update()
    {
        
    }

    public void IncreaseMomentMapThreshold()
    {
        if (getFirstActiveDataSet().GetMomentMapRenderer().MomentMapThreshold <= 0.1)
        {
            getFirstActiveDataSet().GetMomentMapRenderer().MomentMapThreshold += 0.001f;
        }
    }

    public void DecreaseMomentMapThreshold()
    {
        if (getFirstActiveDataSet().GetMomentMapRenderer().MomentMapThreshold >= -0.1)
            getFirstActiveDataSet().GetMomentMapRenderer().MomentMapThreshold -= 0.001f;
    }

    private VolumeDataSetRenderer getFirstActiveDataSet()
    {
        foreach (var dataSet in dataSets)
        {
            if (dataSet.isActiveAndEnabled)
            {
                return dataSet;
            }
        }
        return null;
    }

}
