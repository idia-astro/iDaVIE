using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
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

    /// <summary>
    /// This attribute dictates by how much the min and max values change when the user clicks the button.
    /// </summary>
    private float _increment = 0.01f;
    
    /// <summary>
    /// Timer for the increment and decrement buttons for min and max values.
    /// </summary>
    private float _deltaT = 0.00000f;

    /// <summary>
    /// If _deltaT > this, reset _deltaT to 0.
    /// </summary>
    private float _resetTime = 0.1f;

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

                int increments = Config.Instance.histogramIncrementSteps;
                float val = histogramHelper.CurrentMax - histogramHelper.CurrentMin;
                _increment = val / increments;
                _resetTime = 1.0f / Config.Instance.histogramStepsPerSecond;
            }
        }
    }

    void Update()
    {
        // Add previous frame time to timer
        _deltaT += Time.smoothDeltaTime;
        // If timer has not yet reached reset, don't activate any buttons.
        if (_deltaT < _resetTime)
            return;
        // Else, reset timer and activate buttons where appropriate
        _deltaT = 0.00000f;
        if (DecreaseMinScaleButton.gameObject.GetComponent<UI.UserSelectableItem>().isPressed)
        {
           _decreaseMinScale();
        }

        if (DecreaseMaxScaleButton.gameObject.GetComponent<UI.UserSelectableItem>().isPressed)
        {
            _decreaseMaxScale();
        }

        if (IncreaseMinScaleButton.gameObject.GetComponent<UI.UserSelectableItem>().isPressed)
        {
            _increaseMinScale();
        }

        if (IncreaseMaxScaleButton.gameObject.GetComponent<UI.UserSelectableItem>().isPressed)
        {
            _increaseMaxScale();
        }
    }

    /// <summary>
    /// Function that is called when decreasing the minimum value of the histogram.
    /// </summary>
    private void _decreaseMinScale()
    {
        minText.text = Mathf.Clamp(float.Parse(minText.text) - _increment, getFirstActiveDataSet().Data.MinValue* 2, float.Parse(maxText.text)).ToString();
    }

    /// <summary>
    /// Function that is called when increasing the minimum value of the histogram.
    /// </summary>
    private void _increaseMinScale()
    {
        minText.text = Mathf.Clamp(float.Parse(minText.text) + _increment, getFirstActiveDataSet().Data.MinValue * 2, float.Parse(maxText.text)).ToString();
    }

    /// <summary>
    /// Function that is called when decreasing the maximum value of the histogram.
    /// </summary>
    private void _decreaseMaxScale()
    {
        maxText.text = Mathf.Clamp(float.Parse(maxText.text) - _increment, float.Parse(minText.text), getFirstActiveDataSet().Data.MaxValue* 2).ToString();
        }

    /// <summary>
    /// Function that is called when increasing the maximum value of the histogram.
    /// </summary>
    private void _increaseMaxScale()
    {
        maxText.text = Mathf.Clamp(float.Parse(maxText.text) + _increment, float.Parse(minText.text), getFirstActiveDataSet().Data.MaxValue * 2).ToString();
    }

    /// <summary>
    /// Opens a keypad in VR space. Once the user is satisfied with the number, it sends the value back to target.
    /// </summary>
    /// <param name="target">The text field that the keypad number will be sent to.</param>
    public void OpenKeypad(TextMeshProUGUI target)
    {
        // If any keypads are open, destroy them first.
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

    /// <summary>
    /// Function executed when clicking the update button.
    /// Updates the colour mapping bounds and the desktop UI values.
    /// </summary>
    public void UpdateButtonHandler()
    {
        getFirstActiveDataSet().ScaleMin = float.Parse(minText.text);
        getFirstActiveDataSet().ScaleMax = float.Parse(maxText.text);
        VolumeDataSet.UpdateHistogram(getFirstActiveDataSet().Data, float.Parse(minText.text), float.Parse(maxText.text));
        histogramHelper.CreateHistogramImg(getFirstActiveDataSet().Data.Histogram, getFirstActiveDataSet().Data.HistogramBinWidth, float.Parse(minText.text), float.Parse(maxText.text), getFirstActiveDataSet().Data.MeanValue, getFirstActiveDataSet().Data.StanDev);
    }

    /// <summary>
    /// Function executed when clicking the reset button.
    /// Updates the colour mapping bounds and the desktop UI values back to the default values.
    /// </summary>
    public void ResetButtonHandler()
    {
        getFirstActiveDataSet().ScaleMin = getFirstActiveDataSet().Data.MinValue;
        getFirstActiveDataSet().ScaleMax = getFirstActiveDataSet().Data.MaxValue;
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
