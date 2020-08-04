using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VolumeData;

public class ExitController : MonoBehaviour
{
    public GameObject savePopup;


    public VolumeDataSetRenderer _activeDataSet;
    public VolumeInputController _volumeInputController = null;

    public float VibrationDuration;
    public float VibrationFrequency;
    public float VibrationAmplitude;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Exit()
    {
        Application.Quit();
    }

    public void Cancel()
    {
        this.gameObject.SetActive(false);
    }

    public void CancelSavePopup()
    {
        savePopup.gameObject.SetActive(false);
        this.gameObject.SetActive(true);
    }

    public void SaveAndExit()
    {
        savePopup.transform.SetParent(this.transform.parent, false);
        savePopup.transform.localPosition = this.transform.localPosition;
        savePopup.transform.localRotation = this.transform.localRotation;
        savePopup.transform.localScale = this.transform.localScale;


        savePopup.transform.Find("Content").gameObject.transform.Find("FirstRow").gameObject.transform.Find("Cancel").GetComponent<Button>().onClick.RemoveAllListeners();
        savePopup.transform.Find("Content").gameObject.transform.Find("FirstRow").gameObject.transform.Find("Overwrite").GetComponent<Button>().onClick.RemoveAllListeners();
        savePopup.transform.Find("Content").gameObject.transform.Find("FirstRow").gameObject.transform.Find("NewFile").GetComponent<Button>().onClick.RemoveAllListeners();

        savePopup.transform.Find("Content").gameObject.transform.Find("FirstRow").gameObject.transform.Find("Cancel").GetComponent<Button>().onClick.AddListener(CancelSavePopup);
        savePopup.transform.Find("Content").gameObject.transform.Find("FirstRow").gameObject.transform.Find("Overwrite").GetComponent<Button>().onClick.AddListener(SaveOverwriteMask);
        savePopup.transform.Find("Content").gameObject.transform.Find("FirstRow").gameObject.transform.Find("NewFile").GetComponent<Button>().onClick.AddListener(SaveNewMask);


        gameObject.SetActive(false);
        savePopup.SetActive(true);
    }


    public void SaveOverwriteMask()
    {
        _activeDataSet?.SaveMask(true);

        _volumeInputController.VibrateController(_volumeInputController.PrimaryHand, VibrationDuration, VibrationFrequency, VibrationAmplitude);
        Exit();
    }

    public void SaveNewMask()
    {
        _activeDataSet?.SaveMask(false);
        _volumeInputController.VibrateController(_volumeInputController.PrimaryHand, VibrationDuration, VibrationFrequency, VibrationAmplitude);
        Exit();
    }
}
