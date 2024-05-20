using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VolumeData;

public class HistogramMenuController : MonoBehaviour
{
    public GameObject volumeDataSetManager;

    private VolumeDataSetRenderer[] dataSets;
    private HistogramHelper histogramHelper;

    [SerializeField] private Image img;

    [SerializeField] private Button DecreaseMinScaleButton;
    [SerializeField] private Button IncreaseMinScaleButton;
    [SerializeField] private TextMeshProUGUI minText;
    [SerializeField] private Button editMinScale;

    [SerializeField] private Button DecreaseMaxScaleButton;
    [SerializeField] private Button IncreaseMaxScaleButton;
    [SerializeField] private TextMeshProUGUI maxText;
    [SerializeField] private Button editMaxScale;
    [SerializeField] private GameObject keypadPrefab = null;

    // Start is called before the first frame update
    void Start()
    {
        histogramHelper = FindObjectOfType<HistogramHelper>();

        if (volumeDataSetManager != null)
        {
            dataSets = volumeDataSetManager.GetComponentsInChildren<VolumeDataSetRenderer>(true);

            if (getFirstActiveDataSet() != null)
            {
                VolumeDataSet dataSet = getFirstActiveDataSet().Data;
             
                minText.text = histogramHelper.CurrentMin.ToString();  
                maxText.text = histogramHelper.CurrentMax.ToString();
            }
        }
    }

    void Update()
    {
        if (DecreaseMinScaleButton.gameObject.GetComponent<UI.UserSelectableItem>().isPressed)
        {
           minText.text = Mathf.Clamp(float.Parse(minText.text) - 0.01f, getFirstActiveDataSet().Data.MinValue* 2, float.Parse(maxText.text)).ToString(); 
        }

        if (DecreaseMaxScaleButton.gameObject.GetComponent<UI.UserSelectableItem>().isPressed)
        {
            maxText.text = Mathf.Clamp(float.Parse(maxText.text) - 0.01f, float.Parse(minText.text), getFirstActiveDataSet().Data.MaxValue* 2).ToString();
        }

        if (IncreaseMinScaleButton.gameObject.GetComponent<UI.UserSelectableItem>().isPressed)
        {
            minText.text = Mathf.Clamp(float.Parse(minText.text) + 0.01f, getFirstActiveDataSet().Data.MinValue * 2, float.Parse(maxText.text)).ToString();
        }

        if (IncreaseMaxScaleButton.gameObject.GetComponent<UI.UserSelectableItem>().isPressed)
        {
            maxText.text = Mathf.Clamp(float.Parse(maxText.text) + 0.01f, float.Parse(minText.text), getFirstActiveDataSet().Data.MaxValue * 2).ToString();
        }
    }

    public void OpenKeypad(TextMeshProUGUI target)
    {
        if (GameObject.FindGameObjectWithTag("keypad") != null)
        {
            GameObject[] keypads = GameObject.FindGameObjectsWithTag("keypad");
            foreach(GameObject kp in keypads)
                Destroy(kp);
        }
        Vector3 pos = new Vector3(this.transform.parent.position.x, this.transform.parent.position.y - 0.4f, this.transform.parent.position.z);
        //instantiate item
        GameObject SpawnedItem = Instantiate(keypadPrefab, pos, this.transform.parent.rotation);
        SpawnedItem.transform.localRotation = this.transform.parent.rotation;
        SpawnedItem.GetComponent<KeypadController>().targetText = target;
    }

    public void MinSliderHandler(float value)
    {
        minText.text = value.ToString();
    }

    public void MaxSliderHandler(float value)
    {
        maxText.text = value.ToString();
    }

    public void UpdateButtonHandler()
    {
      VolumeDataSet.UpdateHistogram(getFirstActiveDataSet().Data, float.Parse(minText.text), float.Parse(maxText.text));
      histogramHelper.CreateHistogramImg(getFirstActiveDataSet().Data.Histogram, getFirstActiveDataSet().Data.HistogramBinWidth, float.Parse(minText.text), float.Parse(maxText.text), getFirstActiveDataSet().Data.MeanValue, getFirstActiveDataSet().Data.StanDev);
    }

    public void ResetButtonHandler()
    {
        VolumeDataSet.UpdateHistogram(getFirstActiveDataSet().Data, getFirstActiveDataSet().Data.MinValue, getFirstActiveDataSet().Data.MaxValue);
        histogramHelper.CreateHistogramImg(getFirstActiveDataSet().Data.Histogram, getFirstActiveDataSet().Data.HistogramBinWidth, getFirstActiveDataSet().Data.MinValue, getFirstActiveDataSet().Data.MaxValue, getFirstActiveDataSet().Data.MeanValue, getFirstActiveDataSet().Data.StanDev);
    }

    public void UpdateUI(float min, float max, Sprite img)
    {
        minText.text = min.ToString();
        maxText.text = max.ToString();
        this.img.sprite = img;
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
