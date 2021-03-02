using DataFeatures;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;
using VolumeData;

public class OptionController : MonoBehaviour
{
    private VolumeDataSetRenderer _activeDataSet;
    private VolumeDataSetRenderer[] _dataSets;

    // Color Map
    public Text LabelHand;
    public Text LabelStep;
    public GameObject volumeDatasetRendererObj = null;
    int defaultColorIndex = 33;
    int colorIndex = -1;
    int hand = 0;
    
    [SerializeField]
    public GameObject keypadPrefab = null;
    private float default_threadshold_step = 0.00025f;
    public enum Hand
    {
        Right, Left
    }

    void Start()
    {
       
        if (volumeDatasetRendererObj != null)
            _dataSets = volumeDatasetRendererObj.GetComponentsInChildren<VolumeDataSetRenderer>(true);

        LabelHand.gameObject.GetComponent<Text>().text = (Hand)0 + "";
        LabelStep.gameObject.GetComponent<Text>().text = (float)getFirstActiveDataSet().GetMomentMapRenderer().momstep + "";
    }

    // Update is called once per frame
    void Update()
    {
        var firstActive = getFirstActiveDataSet();
        if (firstActive && _activeDataSet != firstActive)
        {
            _activeDataSet = firstActive;
        }

        if (LabelStep.gameObject.GetComponent<Text>().text != getFirstActiveDataSet().GetMomentMapRenderer().momstep.ToString())
        {
            getFirstActiveDataSet().GetMomentMapRenderer().momstep = float.Parse(LabelStep.gameObject.GetComponent<Text>().text, System.Globalization.CultureInfo.InvariantCulture.NumberFormat); 
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


    public void SetPrimaryHand()
    {
        if (hand == 0)
        {
            hand = 1;
            _activeDataSet._volumeInputController.PrimaryHand = SteamVR_Input_Sources.LeftHand;
        }
        else
        {
            hand = 0;
            _activeDataSet._volumeInputController.PrimaryHand = SteamVR_Input_Sources.RightHand;
        }
        LabelHand.gameObject.GetComponent<Text>().text = (Hand)hand + "";
    }

    public void decreaseMomThresholdStep()
    {
        getFirstActiveDataSet().GetMomentMapRenderer().momstep -= default_threadshold_step;
        LabelStep.gameObject.GetComponent<Text>().text = (float)getFirstActiveDataSet().GetMomentMapRenderer().momstep + "";
    }

    public void increaseMomThresholdStep()
    {
        getFirstActiveDataSet().GetMomentMapRenderer().momstep += default_threadshold_step;
        LabelStep.gameObject.GetComponent<Text>().text = (float)getFirstActiveDataSet().GetMomentMapRenderer().momstep + "";
    }

    public void OpenKeypad()
    {
        Vector3 pos = new Vector3(this.transform.position.x+1, this.transform.position.y, this.transform.position.z);
        //instantiate item

        GameObject SpawnedItem = Instantiate(keypadPrefab, pos, this.transform.localRotation);
        SpawnedItem.GetComponent<KeypadController>().targetText = LabelStep;
    }

}
