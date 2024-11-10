/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 *
 * This file is part of the iDaVIE project.
 *
 * iDaVIE is free software: you can redistribute it and/or modify it under the terms 
 * of the GNU Lesser General Public License (LGPL) as published by the Free Software 
 * Foundation, either version 3 of the License, or (at your option) any later version.
 *
 * iDaVIE is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR 
 * PURPOSE. See the GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License along with 
 * iDaVIE in the LICENSE file. If not, see <https://www.gnu.org/licenses/>.
 *
 * Additional information and disclaimers regarding liability and third-party 
 * components can be found in the DISCLAIMER and NOTICE files included with this project.
 *
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TMPro;
using Valve.Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
using VolumeData;
using Valve;
using Valve.VR;
using DataFeatures;
using VoTableReader;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using SFB;

public class CanvassDesktop : MonoBehaviour
{
    private VolumeDataSetRenderer[] _volumeDataSetRenderers;
    private GameObject _volumeDataSetManager;
    private GameObject[] _sourceRowObjects;

    public GameObject cubeprefab;
    public GameObject informationPanelContent;
    public GameObject renderingPanelContent;
    public GameObject statsPanelContent;
    public GameObject sourcesPanelContent;
    public GameObject mainCanvassDesktop;
    public GameObject fileLoadCanvassDesktop;
    public GameObject VolumePlayer;
    public GameObject SourceRowPrefab;

    public GameObject WelcomeMenu;
    public GameObject LoadingText;
    public TextMeshProUGUI loadTextLabel;

    public GameObject progressBar;

    private HistogramHelper _histogramHelper;

    private bool _showPopUp = false;
    private string _textPopUp = "";
    private VolumeInputController _volumeInputController;
    private VolumeCommandController _volumeCommandController;
    private string _imagePath = "";
    private string _maskPath = "";
    private string _sourcesPath = "";
    
    
    private int _hduSelectionIndex = 0;     //This is the index of the HDU selected in the dropdown. 0 often means HDU 1

    private double _imageNAxis = 0;
    private double _imageSize = 1;
    private double _maskNAxis = 0;
    private double _maskSize = 1;

    private int _subsetMin = 1;
    private int _subsetMax_X = 2;
    private int _subsetMax_Y = 2;
    private int _subsetMax_Z = 2;
    private int[] _subset;

    private int[] _trueBounds;
    
    private Dictionary<double, double> _axisSize = null;
    private Dictionary<double, double> _maskAxisSize = null;

    private int _ratioDropdownIndex = 0;

    private ColorMapEnum _activeColorMap = ColorMapEnum.None;

    private Slider _minThreshold;
    private TextMeshProUGUI _minThresholdLabel;

    private Slider _maxThreshold;
    private TextMeshProUGUI _maxThresholdLabel;

    private float _restFrequency;
    private FeatureMapping _featureMapping;

    private Toggle _subsetToggle;
    private TMP_InputField _subset_XMin_input;
    private TMP_InputField _subset_XMax_input;
    private TMP_InputField _subset_YMin_input;
    private TMP_InputField _subset_YMax_input;
    private TMP_InputField _subset_ZMin_input;
    private TMP_InputField _subset_ZMax_input;
    private TMP_Dropdown _zAxisDropdown;

    public List<TMP_InputField> inputFields;

    private int _inputIndex;

    protected Coroutine _loadCubeCoroutine;
    protected Coroutine _showLoadDialogCoroutine;

    public MenuBarBehaviour MenuBarBehaviour;

    private void Awake()
    {
        // Change the culture to invariant to avoid issues with parsing floats
        System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
        var firstActiveRenderer = GetFirstActiveRenderer();
        if (firstActiveRenderer != null)
        {
            firstActiveRenderer.RestFrequencyGHzListIndexChanged += OnRestFrequencyIndexOfDatasetChanged;
            firstActiveRenderer.RestFrequencyGHzChanged += OnRestFrequencyOfDatasetChanged;
        }
    }

    private void OnDestroy()
    {
        var firstActiveRenderer = GetFirstActiveRenderer();
        if (firstActiveRenderer != null)
        {
            firstActiveRenderer.RestFrequencyGHzListIndexChanged -= OnRestFrequencyIndexOfDatasetChanged;
            firstActiveRenderer.RestFrequencyGHzChanged -= OnRestFrequencyOfDatasetChanged;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        _volumeInputController = FindObjectOfType<VolumeInputController>();
        _volumeCommandController = FindObjectOfType<VolumeCommandController>();
        _histogramHelper = FindObjectOfType<HistogramHelper>();

        CheckCubesDataSet();

        _minThreshold = renderingPanelContent.gameObject.transform.Find("Rendering_container").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject
            .transform.Find("Settings").gameObject.transform.Find("Threshold_container").gameObject.transform.Find("Threshold_min").gameObject.transform.Find("Slider")
            .GetComponent<Slider>();
        _minThresholdLabel = renderingPanelContent.gameObject.transform.Find("Rendering_container").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content")
            .gameObject.transform.Find("Settings").gameObject.transform.Find("Threshold_container").gameObject.transform.Find("Threshold_min").gameObject.transform
            .Find("Min_label").GetComponent<TextMeshProUGUI>();

        _maxThreshold = renderingPanelContent.gameObject.transform.Find("Rendering_container").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject
            .transform.Find("Settings").gameObject.transform.Find("Threshold_container").gameObject.transform.Find("Threshold_max").gameObject.transform.Find("Slider")
            .GetComponent<Slider>();
        _maxThresholdLabel = renderingPanelContent.gameObject.transform.Find("Rendering_container").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content")
            .gameObject.transform.Find("Settings").gameObject.transform.Find("Threshold_container").gameObject.transform.Find("Threshold_max").gameObject.transform
            .Find("Max_label").GetComponent<TextMeshProUGUI>();

        _subsetToggle = informationPanelContent.gameObject.transform.Find("SubsetSelection_container").gameObject.transform.Find("LoadSubset_Toggle").GetComponent<Toggle>();
        _subset_XMin_input = informationPanelContent.gameObject.transform.Find("SubsetMin_container").gameObject.transform.Find("SubsetX_min").GetComponent<TMP_InputField>();
        _subset_XMin_input.onEndEdit.AddListener(checkSubsetBounds);
        _subset_YMin_input = informationPanelContent.gameObject.transform.Find("SubsetMin_container").gameObject.transform.Find("SubsetY_min").GetComponent<TMP_InputField>();
        _subset_YMin_input.onEndEdit.AddListener(checkSubsetBounds);
        _subset_ZMin_input = informationPanelContent.gameObject.transform.Find("SubsetMin_container").gameObject.transform.Find("SubsetZ_min").GetComponent<TMP_InputField>();
        _subset_ZMin_input.onEndEdit.AddListener(checkSubsetBounds);
        _subset_XMax_input = informationPanelContent.gameObject.transform.Find("SubsetMax_container").gameObject.transform.Find("SubsetX_max").GetComponent<TMP_InputField>();
        _subset_XMax_input.onEndEdit.AddListener(checkSubsetBounds);
        _subset_YMax_input = informationPanelContent.gameObject.transform.Find("SubsetMax_container").gameObject.transform.Find("SubsetY_max").GetComponent<TMP_InputField>();
        _subset_YMax_input.onEndEdit.AddListener(checkSubsetBounds);
        _subset_ZMax_input = informationPanelContent.gameObject.transform.Find("SubsetMax_container").gameObject.transform.Find("SubsetZ_max").GetComponent<TMP_InputField>();
        _subset_ZMax_input.onEndEdit.AddListener(checkSubsetBounds);
        _zAxisDropdown = informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("Z_Dropdown").GetComponent<TMP_Dropdown>();
        _zAxisDropdown.onValueChanged.AddListener(updateSubsetZMax);
        _inputIndex = 0;

        _subset_XMin_input.text = _subsetMin.ToString();
        _subset_XMax_input.text = _subsetMax_X.ToString();
        _subset_YMin_input.text = _subsetMin.ToString();
        _subset_YMax_input.text = _subsetMax_Y.ToString();
        _subset_ZMin_input.text = _subsetMin.ToString();
        _subset_ZMax_input.text = _subsetMax_Z.ToString();
        _subset = new int[6];
        _trueBounds = new int[6];
        _subset[0] = _subset[2] = _subset[4] = _subsetMin;
        _subset[1] = _subsetMax_X;
        _subset[3] = _subsetMax_Y;
        _subset[5] = _subsetMax_Z;
    }

    private void PopulateRestfreqencyDropdown()
    {
        var renderingFreqsDropdown = renderingPanelContent.transform.Find("Rendering_container/Viewport/Content/Settings/RestFreq_container/RestFreq_dropdown").GetComponent<TMP_Dropdown>();
        renderingFreqsDropdown.ClearOptions();
        foreach (var freq in GetFirstActiveRenderer().RestFrequencyGHzList.Keys)
        {
            renderingFreqsDropdown.options.Add(new TMP_Dropdown.OptionData(freq));
        }
        
    }

    private void CheckCubesDataSet()
    {
        _volumeDataSetManager = GameObject.Find("VolumeDataSetManager");
        if (_volumeDataSetManager)
        {
            _volumeDataSetRenderers = _volumeDataSetManager.GetComponentsInChildren<VolumeDataSetRenderer>(true);
        }
        else
        {
            _volumeDataSetRenderers = new VolumeDataSetRenderer[0];
        }
    }

    // Update is called once per frame
    private void Update()
    {
        var firstActiveRenderer = GetFirstActiveRenderer();
        if (firstActiveRenderer != null)
        {
            if (_minThreshold.value > _maxThreshold.value)
            {
                _minThreshold.value = _maxThreshold.value;
            }

            var effectiveMin = firstActiveRenderer.ScaleMin + firstActiveRenderer.ThresholdMin 
                * (firstActiveRenderer.ScaleMax - firstActiveRenderer.ScaleMin);
            var effectiveMax = firstActiveRenderer.ScaleMin + firstActiveRenderer.ThresholdMax 
                * (firstActiveRenderer.ScaleMax - firstActiveRenderer.ScaleMin);
            _minThresholdLabel.text = effectiveMin.ToString();
            _maxThresholdLabel.text = effectiveMax.ToString();

            if (firstActiveRenderer.ThresholdMin != _minThreshold.value)
            {
                _minThreshold.value = firstActiveRenderer.ThresholdMin;
            }

            if (firstActiveRenderer.ThresholdMax != _maxThreshold.value)
            {
                _maxThreshold.value = firstActiveRenderer.ThresholdMax;
            }


            if (firstActiveRenderer.ColorMap != _activeColorMap)
            {
                renderingPanelContent.gameObject.transform.Find("Rendering_container").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content")
                    .gameObject.transform.Find("Settings").gameObject.transform.Find("Colormap_container")
                    .gameObject.transform.Find("Dropdown_colormap").GetComponent<TMP_Dropdown>().value = (int)firstActiveRenderer.ColorMap;
            }
        }

        // Check if the tab key is being pressed and if there are more than one input fields in the list
		if (Input.GetKeyDown(KeyCode.Tab) && inputFields.Count > 1) 
		{
			// If there are, check if either shift key is being pressed
			if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) 
			{
				// If shift is pressed, move up on the list - or, if at the top of the list, move to the bottom
				if (_inputIndex <= 0)
				{
					_inputIndex = inputFields.Count;
				}
				_inputIndex--;
				inputFields[_inputIndex].Select();
			}
			else
			{
    			// If shift is not pressed, move down on the list - or, if at the bottom, move to the top
                if (inputFields.Count <= _inputIndex + 1)
                {
                    _inputIndex = -1;
                }
                _inputIndex++;
                inputFields[_inputIndex].Select();
			}
		}
    }

    public void BrowseImageFile()
    {
        LoadingText.SetActive(false);
        string lastPath = PlayerPrefs.GetString("LastPath");
        if (!Directory.Exists(lastPath))
            lastPath = "";
        var extensions = new[]
        {
            new ExtensionFilter("Fits Files", "fits", "fit"),
            new ExtensionFilter("All Files", "*"),
        };
        StandaloneFileBrowser.OpenFilePanelAsync("Open File", lastPath, extensions, false, (string[] paths) =>
        {
            if (paths.Length == 1)
            {
                PlayerPrefs.SetString("LastPath", Path.GetDirectoryName(paths[0]));
                PlayerPrefs.Save();

                _browseImageFile(paths[0]);
            }
        });
    }

    private void _browseImageFile(string path)
    {
        if (path != null)
        {
            _imageSize = 1;

            _imagePath = path;

            //each time you select a fits image, reset the mask and disable loading button
            _maskPath = "";
            informationPanelContent.gameObject.transform.Find("MaskFile_container").gameObject.transform.Find("Button")
                .GetComponent<Button>().interactable = false;
            informationPanelContent.gameObject.transform.Find("MaskFile_container").gameObject.transform
                .Find("MaskFilePath_text").GetComponent<TextMeshProUGUI>().text = "...";
            informationPanelContent.gameObject.transform.Find("Loading_container").gameObject.transform.Find("Button")
                .GetComponent<Button>().interactable = false;

            IntPtr fptr;
            int status = 0;

            if (FitsReader.FitsOpenFile(out fptr, _imagePath, out status, true) != 0)
            {
                Debug.Log("Fits open failure... code #" + status.ToString());
            }

            _axisSize = new Dictionary<double, double>();
            

            //if there are more than 1 HDUs in the fits file, enable the dropdown and populate it with names
            FitsReader.FitsGetHduCount(fptr, out int hduNum, out status);
            var hduNames = new List<string>();
            var hduName = new StringBuilder(80);
            for (var i = 0; i < hduNum; i++)
            {
                FitsReader.FitsMovabsHdu(fptr, i + 1, out _, out status);
                hduName.Clear();
                if (FitsReader.FitsReadKey(fptr, (int)FitsReader.DataType.TSTRING, "EXTNAME", hduName,
                        IntPtr.Zero, out status) != 0)
                {
                    status = 0;
                    if (FitsReader.FitsReadKey(fptr, (int)FitsReader.DataType.TSTRING, "HDUNAME", hduName,
                            IntPtr.Zero, out status) != 0)
                    {
                        Debug.Log("Could not find EXTNAME or HDUNAME in HDU " + (i + 1) +
                                  "! Using default name.");
                        hduName.Append("HDU " + (i + 1));
                        status = 0;
                    }
                }
                hduNames.Add(hduName.ToString());
            }
            _hduSelectionIndex = 0;
            FitsReader.FitsMovabsHdu(fptr, _hduSelectionIndex + 1, out _, out status);
            var hduContainer = informationPanelContent.gameObject.transform.Find("HeaderTitle_container").transform
                .Find("Hdu_container").gameObject;
            hduContainer.transform.Find("Hdu_dropdown").GetComponent<TMP_Dropdown>().ClearOptions();   
            hduContainer.transform.Find("Hdu_dropdown").GetComponent<TMP_Dropdown>().value = 0;
            if (hduNames.Count > 1)
            {
                hduContainer.SetActive(true);
                for (int i = 0; i < hduNames.Count; i++)
                {
                    hduContainer.transform.Find("Hdu_dropdown").GetComponent<TMP_Dropdown>().options.Add(
                        new TMP_Dropdown.OptionData() { text = (i + 1) + ": " + hduNames[i] });
                }
                hduContainer.transform.Find("Hdu_dropdown").GetComponent<TMP_Dropdown>().RefreshShownValue();
            }
            else
            {
                hduContainer.SetActive(false);
            }

            //set the path of selected file to the ui
            informationPanelContent.gameObject.transform.Find("ImageFile_container").gameObject.transform.Find("ImageFilePath_text").GetComponent<TextMeshProUGUI>().text =
                System.IO.Path.GetFileName(_imagePath);

            UpdateHeaderFromFits(fptr);

            FitsReader.FitsCloseFile(fptr, out status);
            
            //if it is valid enable loading button and subset selector
            if (IsLoadable())
            {
                informationPanelContent.gameObject.transform.Find("MaskFile_container").gameObject.transform.Find("Button").GetComponent<Button>().interactable = true;
                informationPanelContent.gameObject.transform.Find("Loading_container").gameObject.transform.Find("Button").GetComponent<Button>().interactable = true;
                informationPanelContent.gameObject.transform.Find("SubsetSelection_container").gameObject.SetActive(true);
                setSubsetBounds();
            }
            else
            {
                informationPanelContent.gameObject.transform.Find("MaskFile_container").gameObject.transform.Find("Button").GetComponent<Button>().interactable = false;
                informationPanelContent.gameObject.transform.Find("Loading_container").gameObject.transform.Find("Button").GetComponent<Button>().interactable = false;
                informationPanelContent.gameObject.transform.Find("SubsetSelection_container").gameObject.SetActive(false);
                loadTextLabel.text = "Not enough dimensions in selected image";
                LoadingText.SetActive(true);
            }
        }

        if (_showLoadDialogCoroutine != null)
            StopCoroutine(_showLoadDialogCoroutine);
    }

    private bool IsLoadable()
    {
        //check if it is a valid fits cube
        List<double> list = new List<double>();
        bool loadable = false;
        string localMsg = "";
        if (_imageNAxis > 2)
        {
            if (_imageNAxis == 3)
            {
                //check if all 3 axis dim are > 1
                //foreach (var axes in axisSize)
                foreach (KeyValuePair<double, double> axes in _axisSize)
                {
                    localMsg += "Axis[" + axes.Key + "]: " + axes.Value + "\n";
                    if (axes.Value > 1)
                    {
                        list.Add(axes.Key);
                        _imageSize *= axes.Value;
                    }
                }

                //if the cube have just 3 axis with n element > 3 is valid
                if (list.Count == 3)
                {
                    loadable = true;
                    _subsetMax_X = (int) _axisSize[list[0]];
                    _subsetMax_Y = (int) _axisSize[list[1]];
                    _subsetMax_Z = (int) _axisSize[list[2]];
                }
            }
            //more than 3 axis
            else
            {
                // more than 3 axis, check if axis dim are > 1
                foreach (KeyValuePair<double, double> axes in _axisSize)
                {
                    localMsg += "Axis[" + axes.Key + "]: " + axes.Value + "\n";
                    if (axes.Value > 1)
                    {
                        list.Add(axes.Key);
                        _imageSize *= axes.Value;
                    }
                }

                //more than 3 axis but just 3 axis have nelement > 1
                if (list.Count == 3)
                {
                    loadable = true;
                    _subsetMax_X = (int) _axisSize[list[0]];
                    _subsetMax_Y = (int) _axisSize[list[1]];
                    _subsetMax_Z = (int) _axisSize[list[2]];
                }
                else
                    informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.SetActive(true);
            }

            //update dropdown
            _zAxisDropdown.interactable = false;
            _zAxisDropdown.ClearOptions();

            foreach (KeyValuePair<double, double> axes in _axisSize)
            {
                if (axes.Value > 1 && axes.Key > 2)
                {
                    _zAxisDropdown.options.Add((new TMP_Dropdown.OptionData() { text = axes.Key.ToString() }));
                }
            }

            _zAxisDropdown.RefreshShownValue();
            _zAxisDropdown.value = 0;
            //end update dropdown

            //Cube is not loadable with valid axis < 3
            if (!loadable && list.Count < 3)
            {
                _showPopUp = true;
                _textPopUp = "NAxis_ " + _imageNAxis + "\n" + localMsg;
            }
            //cube is not loadable with more than 3 axis with nelement
            else if (!loadable && list.Count > 3)
            {
                _zAxisDropdown.interactable = true;

                loadable = true;
                _subsetMax_X = (int) _axisSize[list[0]];
                _subsetMax_Y = (int) _axisSize[list[1]];
                int zAxisIdx;
                Int32.TryParse(_zAxisDropdown.options[_zAxisDropdown.value].text, out zAxisIdx);
                zAxisIdx -= 1;
                Debug.Log("The list has " + list.Count + " items, and the dropdown points to index " + zAxisIdx + "!");
                _subsetMax_Z = (int) _axisSize[list[zAxisIdx]];
            }
        }
        else
        {
            loadable = false;
            localMsg = "Please select a valid cube!";
        }

        return loadable;
    }

    private void UpdateHeaderFromFits(IntPtr fptr)
    {
        int status;
        //visualize the header into the scroll view
        string _header = "";
        _axisSize.Clear();
        IDictionary<string, string> _headerDictionary = FitsReader.ExtractHeaders(fptr, out status);

        foreach (KeyValuePair<string, string> entry in _headerDictionary)
        {
            //switch (entry.Key)
            if (entry.Key.Length > 4)
                switch (entry.Key.Substring(0, 5))
                {
                    case "NAXIS":
                        string sub = entry.Key.Substring(5);

                        if (sub == "")
                            _imageNAxis = Convert.ToDouble(entry.Value, CultureInfo.InvariantCulture);
                        else
                            _axisSize.Add(Convert.ToDouble(sub, CultureInfo.InvariantCulture), Convert.ToDouble(entry.Value, CultureInfo.InvariantCulture));
                        break;
                }

            _header += entry.Key + "\t\t " + entry.Value + "\n";
        }

           

            
        informationPanelContent.gameObject.transform.Find("Header_container").gameObject.transform.Find("Scroll View").gameObject.transform.Find("Viewport").gameObject
            .transform.Find("Content").gameObject.transform.Find("Header").GetComponent<TextMeshProUGUI>().text = _header;
        informationPanelContent.gameObject.transform.Find("Header_container").gameObject.transform.Find("Scroll View").gameObject.transform.Find("Scrollbar Vertical")
            .GetComponent<Scrollbar>().value = 1;
    }

    public void onSubsetToggleSelected(bool val)
    {
        if (_subsetToggle.isOn)
        {
            informationPanelContent.gameObject.transform.Find("SubsetLabel_container").gameObject.SetActive(true);
            informationPanelContent.gameObject.transform.Find("SubsetMin_container").gameObject.SetActive(true);
            informationPanelContent.gameObject.transform.Find("SubsetMax_container").gameObject.SetActive(true);
            inputFields[_inputIndex].Select();
        }
        else
        {
            informationPanelContent.gameObject.transform.Find("SubsetLabel_container").gameObject.SetActive(false);
            informationPanelContent.gameObject.transform.Find("SubsetMin_container").gameObject.SetActive(false);
            informationPanelContent.gameObject.transform.Find("SubsetMax_container").gameObject.SetActive(false);
        }
    }

    public void setSubsetBounds()
    {
        _subset_XMin_input.text = _subsetMin.ToString();
        _subset_YMin_input.text = _subsetMin.ToString();
        _subset_ZMin_input.text = _subsetMin.ToString();
        _subset_XMax_input.text = _subsetMax_X.ToString();
        _subset_YMax_input.text = _subsetMax_Y.ToString();
        _subset_ZMax_input.text = _subsetMax_Z.ToString();
        
        _subset[0] = _subset[2] = _subset[4] = _trueBounds[0] = _trueBounds[2] = _trueBounds[4] = _subsetMin;
        _subset[1] = _trueBounds[1] = _subsetMax_X;
        _subset[3] = _trueBounds[3] = _subsetMax_Y;
        _subset[5] = _trueBounds[5] = _subsetMax_Z;
    }

    public void updateSubsetZMax(int val = 0)
    {
        int i2;
        int.TryParse(_zAxisDropdown.options[_zAxisDropdown.value].text, out i2);
        i2 -= 1;
        int oldMaxZ = _subsetMax_Z;
        _subsetMax_Z = (int) _axisSize[i2 + 1];
        string val1 = _subset_ZMax_input.text;
        int valInt = 0;
        if (Int32.TryParse(val1, out valInt)){
            if (valInt < _subsetMin)
                _subset_ZMax_input.text = (_subsetMin).ToString();
            else if (valInt > _subsetMax_Z || valInt == oldMaxZ)
                _subset_ZMax_input.text = _subsetMax_Z.ToString();
        }
        
        _subset[0] = _subset[2] = _subset[4] = _subsetMin;
        _subset[1] = _subsetMax_X;
        _subset[3] = _subsetMax_Y;
        _subset[5] = _subsetMax_Z;
    }
    
    public void checkSubsetBounds(string val1 = "")
    {
        string val = _subset_XMax_input.text;
        int valInt = 0;
        if (Int32.TryParse(val, out valInt)){
            if (valInt < _subsetMin){
                Debug.Log(val + " is less than the minimum which is " + (_subsetMin).ToString() + "!");
                _subset_XMax_input.text = (_subset[0]).ToString();
            }
            else if (valInt > _subsetMax_X){
                Debug.Log(val + " is more than the maximum which is " + _subsetMax_X.ToString() + "!");
                _subset_XMax_input.text = _subsetMax_X.ToString();
            }
            else if (valInt < _subset[0]){
                Debug.Log(val + " is less than the current chosen lower bound which is " + (_subset[0]).ToString() + "!");
                _subset_XMax_input.text = (_subset[0]).ToString();
            }
        }
        else{
            Debug.Log(val + " is not a number!");
            _subset_XMax_input.text = _subsetMax_X.ToString();
        }

        val = _subset_YMax_input.text;
        valInt = 0;
        if (Int32.TryParse(val, out valInt)){
            if (valInt < _subsetMin){
                Debug.Log(val + " is less than the minimum which is " + (_subsetMin).ToString() + "!");
                _subset_YMax_input.text = (_subset[2]).ToString();
            }
            else if (valInt > _subsetMax_Y){
                Debug.Log(val + " is more than the maximum which is " + _subsetMax_Y.ToString() + "!");
                _subset_YMax_input.text = _subsetMax_Y.ToString();
            }
            else if (valInt < _subset[2]){
                Debug.Log(val + " is less than the current chosen lower bound which is " + (_subset[2]).ToString() + "!");
                _subset_YMax_input.text = (_subset[2]).ToString();
            }
        }
        else{
            Debug.Log(val + " is not a number!");
            _subset_YMax_input.text = _subsetMax_Y.ToString();
        }

        val = _subset_ZMax_input.text;
        valInt = 0;
        if (Int32.TryParse(val, out valInt)){
            if (valInt < _subsetMin){
                Debug.Log(val + " is less than the minimum which is " + (_subsetMin).ToString() + "!");
                _subset_ZMax_input.text = (_subset[4]).ToString();
            }
            else if (valInt > _subsetMax_Z){
                Debug.Log(val + " is more than the maximum which is " + _subsetMax_Z.ToString() + "!");
                _subset_ZMax_input.text = _subsetMax_Z.ToString();
            }
            else if (valInt < _subset[4]){
                Debug.Log(val + " is less than the current chosen lower bound which is " + (_subset[4]).ToString() + "!");
                _subset_ZMax_input.text = (_subset[4]).ToString();
            }
        }
        else{
            Debug.Log(val + " is not a number!");
            _subset_ZMax_input.text = _subsetMax_Z.ToString();
        }

        val = _subset_XMin_input.text;
        valInt = 0;
        if (Int32.TryParse(val, out valInt)){
            if (valInt < _subsetMin){
                Debug.Log(val + " is less than the minimum which is " + _subsetMin.ToString() + "!");
                _subset_XMin_input.text = _subsetMin.ToString();
            }
            else if (valInt > _subsetMax_X){
                Debug.Log(val + " is more than the maximum which is " + (_subsetMax_X).ToString() + "!");
                _subset_XMin_input.text = (_subset[1]).ToString();
            }
            else if (valInt > _subset[1]){
                Debug.Log(val + " is more than the current chosen upper bound which is " + (_subset[1]).ToString() + "!");
                _subset_XMin_input.text = (_subset[1]).ToString();
            }
        }
        else{
            Debug.Log(val + " is not a number!");
            _subset_XMin_input.text = _subsetMin.ToString();
        }

        val = _subset_YMin_input.text;
        valInt = 0;
        if (Int32.TryParse(val, out valInt)){
            if (valInt < _subsetMin){
                Debug.Log(val + " is less than the minimum which is " + _subsetMin.ToString() + "!");
                _subset_YMin_input.text = _subsetMin.ToString();
            }
            else if (valInt > _subsetMax_Y){
                Debug.Log(val + " is more than the maximum which is " + (_subsetMax_Y).ToString() + "!");
                _subset_YMin_input.text = (_subset[3]).ToString();
            }
            else if (valInt > _subset[3]){
                Debug.Log(val + " is more than the current chosen upper bound which is " + (_subset[3]).ToString() + "!");
                _subset_YMin_input.text = (_subset[3]).ToString();
            }
        }
        else{
            Debug.Log(val + " is not a number!");
            _subset_YMin_input.text = _subsetMin.ToString();
        }

        val = _subset_ZMin_input.text;
        valInt = 0;
        if (Int32.TryParse(val, out valInt)){
            if (valInt < _subsetMin){
                Debug.Log(val + " is less than the minimum which is " + _subsetMin.ToString() + "!");
                _subset_ZMin_input.text = _subsetMin.ToString();
            }
            else if (valInt > _subsetMax_Z){
                Debug.Log(val + " is more than the maximum which is " + (_subsetMax_Z).ToString() + "!");
                _subset_ZMin_input.text = (_subset[5]).ToString();
            }
            else if (valInt > _subset[5]){
                Debug.Log(val + " is more than the current chosen upper bound which is " + (_subset[5]).ToString() + "!");
                _subset_ZMin_input.text = (_subset[5]).ToString();
            }
        }
        else{
            Debug.Log(val + " is not a number!");
            _subset_ZMin_input.text = _subsetMin.ToString();
        }
        
        _subset[0] = Int32.Parse(_subset_XMin_input.text);
        _subset[1] = Int32.Parse(_subset_XMax_input.text);
        _subset[2] = Int32.Parse(_subset_YMin_input.text);
        _subset[3] = Int32.Parse(_subset_YMax_input.text);
        _subset[4] = Int32.Parse(_subset_ZMin_input.text);
        _subset[5] = Int32.Parse(_subset_ZMax_input.text);
    }

    public void BrowseMaskFile()
    {
        string lastPath = PlayerPrefs.GetString("LastPath");
        if (!Directory.Exists(lastPath))
            lastPath = "";
        var extensions = new[]
        {
            new ExtensionFilter("Fits Files", "fits", "fit"),
            new ExtensionFilter("All Files", "*"),
        };
        StandaloneFileBrowser.OpenFilePanelAsync("Open File", lastPath, extensions, false, (string[] paths) =>
        {
            if (paths.Length == 1)
            {
                PlayerPrefs.SetString("LastPath", Path.GetDirectoryName(paths[0]));
                PlayerPrefs.Save();

                _browseMaskFile(paths[0]);
            }
        });
    }

    private void _browseMaskFile(string path)
    {
        bool loadable = false;

        if (_maskPath != null)
        {
            informationPanelContent.gameObject.transform.Find("Loading_container").gameObject.transform.Find("Button").GetComponent<Button>().interactable = false;
            _maskSize = 1;
            _maskPath = path;

            IntPtr fptr;
            int status = 0;

            if (FitsReader.FitsOpenFile(out fptr, _maskPath, out status, true) != 0)
            {
                Debug.Log("Fits open failure... code #" + status.ToString());
            }

            informationPanelContent.gameObject.transform.Find("MaskFile_container").gameObject.transform.Find("MaskFilePath_text").GetComponent<TextMeshProUGUI>().text =
                System.IO.Path.GetFileName(_maskPath);

            _maskAxisSize = new Dictionary<double, double>();
            List<double> list = new List<double>();


            //visualize the header into the scroll view
            IDictionary<string, string> _headerDictionary = FitsReader.ExtractHeaders(fptr, out status);
            FitsReader.FitsCloseFile(fptr, out status);

            foreach (KeyValuePair<string, string> entry in _headerDictionary)
            {
                if (entry.Key.Length > 4)
                    switch (entry.Key.Substring(0, 5))
                    {
                        case "NAXIS":
                            string sub = entry.Key.Substring(5);

                            if (sub == "")
                                _maskNAxis = Convert.ToDouble(entry.Value, CultureInfo.InvariantCulture);
                            else
                            {
                                _maskAxisSize.Add(Convert.ToDouble(sub, CultureInfo.InvariantCulture), Convert.ToDouble(entry.Value, CultureInfo.InvariantCulture));
                            }

                            break;
                    }
            }

            if (_maskNAxis > 2)
            {
                //Get Axis size from Image Cube
                int i2 = int.Parse(_zAxisDropdown.options[_zAxisDropdown.value].text) - 1;
                if (_axisSize[1] == _maskAxisSize[1] && _axisSize[2] == _maskAxisSize[2] && _axisSize[i2 + 1] == _maskAxisSize[3])
                {
                    loadable = true;
                    informationPanelContent.gameObject.transform.Find("Loading_container").gameObject.transform.Find("Button").GetComponent<Button>().interactable = true;
                    informationPanelContent.gameObject.transform.Find("SubsetSelection_container").gameObject.SetActive(true);
                }
                else
                    loadable = false;
            }

            if (!loadable)
            {
                //mask is not valid
                informationPanelContent.gameObject.transform.Find("MaskFile_container").gameObject.transform.Find("MaskFilePath_text").GetComponent<TextMeshProUGUI>().text = "...";
                _maskPath = "";
                _showPopUp = true;
                _textPopUp = "Selected Mask\ndoesn't match image file";
            }
        }

        if (_showLoadDialogCoroutine != null)
            StopCoroutine(_showLoadDialogCoroutine);
    }

    public void CheckImgMaskAxisSize()
    {
        if (_maskPath != "")
        {
            //Get Axis size from Image Cube
            int i2 = int.Parse(_zAxisDropdown.options[_zAxisDropdown.value].text) - 1;

            if (_axisSize[1] != _maskAxisSize[1] || _axisSize[2] != _maskAxisSize[2] || _axisSize[i2 + 1] != _maskAxisSize[3])
            {
                informationPanelContent.gameObject.transform.Find("MaskFile_container").gameObject.transform.Find("MaskFilePath_text").GetComponent<TextMeshProUGUI>().text = "...";
                _showPopUp = true;
                _textPopUp = "Selected axis size \ndoesn't match mask axis size";
                informationPanelContent.gameObject.transform.Find("Loading_container").gameObject.transform.Find("Button").GetComponent<Button>().interactable = false;
            }
            else
            {
                informationPanelContent.gameObject.transform.Find("Loading_container").gameObject.transform.Find("Button").GetComponent<Button>().interactable = true;
                informationPanelContent.gameObject.transform.Find("SubsetSelection_container").gameObject.SetActive(true);
            }
        }
    }
    
    public void setMaskPath(string mPath)
    {
        _maskPath = mPath;
    }

    /// <summary>
    /// This function should be called from the software if the cube needs to be reloaded for whatever reason.
    /// For example, reloading after user confirmation when entering paint mode without a loaded mask if a subcube is loaded.
    /// </summary>
    public void reload()
    {
        LoadFileFromFileSystem();
    }

    public void LoadFileFromFileSystem()
    {
        StartCoroutine(LoadCubeCoroutine(_imagePath, _maskPath, _hduSelectionIndex + 1));
    }

    /// <summary>
    /// Method for all the necessary post loading actions after loading a cube from the file system
    /// </summary>
    private void postLoadFileFileSystem()
    {
        if (_loadCubeCoroutine != null)
            StopCoroutine(_loadCubeCoroutine);

        VolumePlayer.SetActive(false);
        VolumePlayer.SetActive(true);

        var firstActiveRenderer = GetFirstActiveRenderer();
        
        renderingPanelContent.gameObject.transform.Find("Rendering_container").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject.transform
                .Find("Settings").gameObject.transform.Find("Mask_container").gameObject.transform.Find("Dropdown_mask").GetComponent<TMP_Dropdown>().interactable =
            firstActiveRenderer.MaskFileName != "";

        populateColorMapDropdown();
        populateStatsValue();
        
        // Populate the rest frequency dropdown in the Rendering tab from config file
        PopulateRestfreqencyDropdown();
        SetRestFrequencyInputInteractable(false);
        SetRestFrequencyInputField((float)firstActiveRenderer.RestFrequencyGHz);

        //subscribe rest frequency change behanvior to changes in new loaded cube
        firstActiveRenderer.RestFrequencyGHzListIndexChanged += OnRestFrequencyIndexOfDatasetChanged;
        firstActiveRenderer.RestFrequencyGHzChanged += OnRestFrequencyOfDatasetChanged;
        
        
        LoadingText.gameObject.SetActive(false);
        progressBar.gameObject.SetActive(false);
        WelcomeMenu.gameObject.SetActive(false);

        mainCanvassDesktop.gameObject.transform.Find("RightPanel").gameObject.transform.Find("Tabs_ container").gameObject.transform.Find("Rendering_Button").GetComponent<Button>()
            .interactable = true;
        mainCanvassDesktop.gameObject.transform.Find("RightPanel").gameObject.transform.Find("Tabs_ container").gameObject.transform.Find("Stats_Button").GetComponent<Button>()
            .interactable = true;
        mainCanvassDesktop.gameObject.transform.Find("RightPanel").gameObject.transform.Find("Tabs_ container").gameObject.transform.Find("Sources_Button").GetComponent<Button>()
            .interactable = true;

        mainCanvassDesktop.gameObject.transform.Find("RightPanel").gameObject.transform.Find("Tabs_ container").gameObject.transform.Find("Stats_Button").GetComponent<Button>()
            .onClick.Invoke();
        
        // Enable the desktop GUI VR View as default left panel content to display
        if (MenuBarBehaviour.AboutSection.activeSelf)
        {
            MenuBarBehaviour.ToggleAboutSection();
        }
        if (!MenuBarBehaviour.VRViewDisplay.activeSelf)
        {
            MenuBarBehaviour.ToggleVRViewDisplay();
        }
    }

    public bool CheckMemSpaceForCubes(string _imagePath, string _maskPath)
    {
        int ramSizeMB = SystemInfo.systemMemorySize;
        float imgSize = new FileInfo(_imagePath).Length / 1024f / 1024f;
        float maskSize = (String.IsNullOrEmpty(_maskPath)) ? 0 : new FileInfo(_maskPath).Length / 1024f / 1024f;
        float sumSizeMB = imgSize + maskSize;
        if (sumSizeMB >= ramSizeMB){
            Debug.LogWarning("Cube and mask size (" + sumSizeMB.ToString("F2") + " MB) exceed RAM size (" + ramSizeMB.ToString("F2") + " MB)!");
            return true;
        }
        Debug.Log("Loading cube and mask of size " + sumSizeMB.ToString("F2") + " MB with RAM size " + ramSizeMB.ToString("F2") + " MB.");
        return false;
    }

    public IEnumerator LoadCubeCoroutine(string _imagePath, string _maskPath, int hduSelection = 1)
    {
        LoadingText.gameObject.SetActive(true);
        progressBar.gameObject.SetActive(true);
        if (CheckMemSpaceForCubes(_imagePath, _maskPath)){
            loadTextLabel.text = "Cube too large to fit into RAM! Using virtual memory!";
            yield return new WaitForSeconds(5.0f);
        }
        loadTextLabel.text = "Loading...";
        Debug.Log("Loading image " + _imagePath + " and mask " + _maskPath + ".");
        progressBar.GetComponent<Slider>().value = 0;
        yield return new WaitForSeconds(0.001f);

        float zScale = 1f;
        if (_ratioDropdownIndex == 1)
        {
            // case X=Y, calculate z scale from NAXIS1 and NAXIS3
            int i2 = int.Parse(_zAxisDropdown.options[_zAxisDropdown.value].text) - 1;

            double x, z;
            if (_axisSize.TryGetValue(1, out x) && _axisSize.TryGetValue(i2 + 1, out z))
            {
                zScale = (float)(z / x);
            }
        }

        var firstActiveRenderer = GetFirstActiveRenderer();
        loadTextLabel.text = "Replacing old cube...";
        progressBar.GetComponent<Slider>().value = 1;
        yield return new WaitForSeconds(0.001f);
        if (firstActiveRenderer != null)
        {
            Debug.Log("Replacing data cube...");

            firstActiveRenderer.transform.gameObject.SetActive(false);
            _volumeCommandController.RemoveDataSet(firstActiveRenderer);
            // Reset UI to default
            try
            {
                _volumeInputController = FindObjectOfType<VolumeInputController>();
                _volumeInputController.gameObject.SetActive(false);
                _volumeInputController.gameObject.SetActive(true);

                _volumeCommandController.DisablePaintMode();
                _volumeCommandController.endThresholdEditing();
                _volumeCommandController.endZAxisEditing();
            }
            catch (Exception)
            {
                // ignored
            }

            // Manually clean up
            firstActiveRenderer.Data.CleanUp(firstActiveRenderer.RandomVolume);
            firstActiveRenderer.Mask?.CleanUp(false);
            Destroy(firstActiveRenderer);
        }

        loadTextLabel.text = "Building new cube...";
        progressBar.GetComponent<Slider>().value = 2;
        Debug.Log("Instantiating new cube prefab.");
        yield return new WaitForSeconds(0.001f);

        GameObject newCube = Instantiate(cubeprefab, new Vector3(0, 0f, 0), Quaternion.identity);
        newCube.transform.localScale = new Vector3(1, 1, zScale);
        newCube.SetActive(true);

        newCube.transform.SetParent(_volumeDataSetManager.transform, false);

        // Set data to be loaded
        var volDSRender = newCube.GetComponent<VolumeDataSetRenderer>();
        volDSRender.subsetBounds = _subset;
        volDSRender.trueBounds = _trueBounds;
        volDSRender.FileName = _imagePath; //_dataSet.FileName.ToString();
        volDSRender.MaskFileName = _maskPath; // _maskDataSet.FileName.ToString();
        volDSRender.SelectedHdu = hduSelection;
        volDSRender.loadText = this.loadTextLabel;
        volDSRender.progressBar = this.progressBar.GetComponent<Slider>();
        volDSRender.CubeDepthAxis = int.Parse(_zAxisDropdown.options[_zAxisDropdown.value].text) - 1;
        volDSRender.FileChanged = false;
        _zAxisDropdown.interactable = false;

        CheckCubesDataSet();

        // Toggle VolumeInputController activation to update VolumeInputController's list of datasets
        _volumeInputController.gameObject.SetActive(false);
        yield return new WaitForSeconds(0.001f);
        _volumeInputController.gameObject.SetActive(true);

        // Toggle FeatureMenuController activation to reload source list
        var featureMenu = FindObjectOfType<FeatureMenuController>();
        if (featureMenu?.gameObject?.activeSelf == true)
        {
            featureMenu.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.001f);
            featureMenu.gameObject.SetActive(true);
        }
        
        _volumeCommandController.AddDataSet(newCube.GetComponent<VolumeDataSetRenderer>());
        StartCoroutine(newCube.GetComponent<VolumeDataSetRenderer>()._startFunc());

        while (!newCube.GetComponent<VolumeDataSetRenderer>().started)
        {
            yield return new WaitForSeconds(.1f);
        }

        loadTextLabel.text = "Loading complete!";
        string completeMessage = "";
        completeMessage += "Loading image " + _imagePath;
        if (_maskPath != "")
            completeMessage += " and mask " + _maskPath;
        completeMessage += " complete!";
        
        Debug.Log(completeMessage);
        progressBar.GetComponent<Slider>().value = 6;
        yield return new WaitForSeconds(0.001f);
        postLoadFileFileSystem();
    }

    public void OnRatioDropdownValueChanged(int optionIndex)
    {
        _ratioDropdownIndex = optionIndex;
        var firstActiveRenderer = GetFirstActiveRenderer();
        if (firstActiveRenderer != null)
        {
            if (optionIndex == 0)
            {
                // X=Y=Z
                firstActiveRenderer.ZScale = firstActiveRenderer.XScale;
            }
            else
            {
                // X=Y
                firstActiveRenderer.ZScale = firstActiveRenderer.XScale * firstActiveRenderer.GetCubeDimensions().z / firstActiveRenderer.GetCubeDimensions().x;
            }
        }
    }

    /// <summary>
    /// When the rest frequency index of the dataset changes, update the dropdown selection
    /// </summary>
    private void OnRestFrequencyIndexOfDatasetChanged()
    {
        SetRestFrequencyDropdown(GetFirstActiveRenderer().RestFrequencyGHzListIndex);
    }

    /// <summary>
    /// When the rest frequency of the dataset changes, update the input field
    /// </summary>
    private void OnRestFrequencyOfDatasetChanged()
    {
        SetRestFrequencyInputField(GetFirstActiveRenderer().RestFrequencyGHz);
    }
    
    /// <summary>
    /// When the dropdown value changes, update the rest frequency of the dataset
    /// </summary>
    /// <param name="optionIndex"></param>
    public void OnRestFrequencyDropdownValueChanged(int optionIndex)
    {
        GetFirstActiveRenderer().RestFrequencyGHzListIndex = optionIndex;
    }

    /// <summary>
    /// When the rest frequency input field changes, update the rest frequency of the dataset
    /// </summary>
    /// <param name="val"></param>
    public void OnRestFrequencyValueChanged(String val)
    {
        var newRestFrequencyGHz = double.Parse(val);
        var firstActiveRenderer = GetFirstActiveRenderer();
        firstActiveRenderer.RestFrequencyGHzList["Custom"] = newRestFrequencyGHz;
        if (firstActiveRenderer.OverrideRestFrequency)
            firstActiveRenderer.RestFrequencyGHz = newRestFrequencyGHz;
    }
    
    /// <summary>
    /// Sets the rest frequency input field to be interactable or not
    /// </summary>
    /// <param name="isInteractable"></param>
    private void SetRestFrequencyInputInteractable(bool isInteractable)
    {
        renderingPanelContent.transform.Find("Rendering_container/Viewport/Content/Settings/RestFreq_container/RestFreq_input")
            .GetComponent<TMP_InputField>().interactable = isInteractable;
    }
    
    /// <summary>
    /// Sets the rest frequency input field to the given value
    /// </summary>
    /// <param name="restFrequency"></param>
    private void SetRestFrequencyInputField(double restFrequency)
    {
        renderingPanelContent.transform.Find("Rendering_container/Viewport/Content/Settings/RestFreq_container/RestFreq_input")
            .GetComponent<TMP_InputField>().text = restFrequency.ToString();
    }
    
    /// <summary>
    /// Sets the rest frequency dropdown to the given index
    /// </summary>
    /// <param name="index"></param>
    private void SetRestFrequencyDropdown(int index)
    {
        renderingPanelContent.transform.Find("Rendering_container/Viewport/Content/Settings/RestFreq_container/RestFreq_dropdown")
            .GetComponent<TMP_Dropdown>().value = index;
        //if Default is selected, disable the input field and use the cube's default rest frequency
        if (index == 0)
        {
            SetRestFrequencyInputInteractable(false);
        }
        //if "Custom" is selected, enable the input field
        else if (index == GetFirstActiveRenderer().RestFrequencyGHzList.Count - 1)
        {
            SetRestFrequencyInputInteractable(true);
        }
        //otherwise, use the selected rest frequency from the config file
        else
        {
            SetRestFrequencyInputInteractable(false);
        }
    }

    public void BrowseSourcesFile()
    {
        string lastPath = PlayerPrefs.GetString("LastPath");
        if (!Directory.Exists(lastPath))
            lastPath = "";
        var extensions = new[]
        {
            new ExtensionFilter("Source Tables", "xml", "fits", "fit"),
            new ExtensionFilter("All Files", "*"),
        };
        StandaloneFileBrowser.OpenFilePanelAsync("Open File", lastPath, extensions, false, (string[] paths) =>
        {
            if (paths.Length == 1)
            {
                PlayerPrefs.SetString("LastPath", Path.GetDirectoryName(paths[0]));
                PlayerPrefs.Save();

                _browseSourcesFile(paths[0]);
            }
        });
    }

    private void _browseSourcesFile(string path)
    {
        var firstActiveRenderer = GetFirstActiveRenderer();
        var featureDataSet = firstActiveRenderer.GetComponentInChildren<FeatureSetManager>();
        _sourcesPath = path;
        featureDataSet.FeatureFileToLoad = path;
        sourcesPanelContent.gameObject.transform.Find("Lower_container").gameObject.transform.Find("MappingSave_container").gameObject.transform.Find("Button").GetComponent<Button>().interactable = true;
        sourcesPanelContent.gameObject.transform.Find("Lower_container").gameObject.transform.Find("SourcesLoad_container").gameObject.transform.Find("Button").GetComponent<Button>().interactable = true;
        //activate load features button
        sourcesPanelContent.gameObject.transform.Find("SourcesFile_container").gameObject.transform.Find("SourcesFilePath_text").GetComponent<TextMeshProUGUI>().text =
            System.IO.Path.GetFileName(path);

        var featureTable = FeatureTable.GetFeatureTableFromFile(path);
        
        Transform sourceBody = sourcesPanelContent.gameObject.transform.Find("SourcesInfo_container").gameObject.transform.Find("Scroll View").gameObject.transform.Find("Viewport")
            .gameObject.transform.Find("Content").gameObject.transform;
        if (_sourceRowObjects != null)
        {
            foreach (var row in _sourceRowObjects)
                Destroy(row);
            _sourceRowObjects = null;
        }

        _sourceRowObjects = new GameObject[featureTable.Columns.Count];
        for (var i = 0; i < featureTable.Columns.Count; i++)
        {
            var row = Instantiate(SourceRowPrefab, sourceBody);
            row.transform.Find("Source_number").GetComponent<TextMeshProUGUI>().text = i.ToString();
            string colName = featureTable.Columns.ElementAt(i).Key;
            // Hard coded 17 (*shivers*) matching the length available in the UI as of coding this. Do better!
            if (colName.Length > 17)
                colName = colName.Substring(0, 14) + "...";
            row.transform.Find("Source_name").GetComponent<TextMeshProUGUI>().text = colName;
            var rowScript = row.GetComponentInParent<SourceRow>();
            rowScript.SourceName = featureTable.Columns.ElementAt(i).Key;
            rowScript.SourceIndex = i;
            _sourceRowObjects[i] = row;
        }

        sourcesPanelContent.gameObject.transform.Find("MappingFile_container").gameObject.transform.Find("Button").GetComponent<Button>().interactable = true;
    }

    public void BrowseMappingFile()
    {
        string lastPath = PlayerPrefs.GetString("LastPath");
        if (!Directory.Exists(lastPath))
            lastPath = "";
        var extensions = new[]
        {
            new ExtensionFilter("JSON", "json"),
            new ExtensionFilter("All Files", "*"),
        };
        StandaloneFileBrowser.OpenFilePanelAsync("Open File", lastPath, extensions, false, (string[] paths) =>
        {
            if (paths.Length == 1)
            {
                PlayerPrefs.SetString("LastPath", Path.GetDirectoryName(paths[0]));
                PlayerPrefs.Save();

                _browseMappingFile(paths[0]);
            }
        });
    }

    private void _browseMappingFile(string path)
    {
        sourcesPanelContent.gameObject.transform.Find("MappingFile_container").gameObject.transform.Find("MappingFilePath_text").GetComponent<TextMeshProUGUI>().text =
            System.IO.Path.GetFileName(path);
        _featureMapping = FeatureMapping.GetMappingFromFile(path);
        foreach (var sourceRowObject in _sourceRowObjects)
        {
            var dropdown = sourceRowObject.transform.Find("Coord_dropdown").gameObject.GetComponent<TMP_Dropdown>();
            dropdown.value = 0;
            sourceRowObject.transform.Find("Import_toggle").gameObject.GetComponent<Toggle>().isOn = false;
        }

        foreach (var sourceRowObject in _sourceRowObjects)
        {
            try
            {
                var sourceRow = sourceRowObject.GetComponent<SourceRow>();
                var dropdown = sourceRowObject.transform.Find("Coord_dropdown").gameObject.GetComponent<TMP_Dropdown>();
                if (_featureMapping.Mapping.ImportedColumns.Contains(sourceRow.SourceName))
                    sourceRowObject.transform.Find("Import_toggle").gameObject.GetComponent<Toggle>().isOn = true;
                if (sourceRow.SourceName == _featureMapping.Mapping.ID.Source)
                {
                    sourceRow.CurrentMapping = SourceMappingOptions.ID;
                    dropdown.value = (int)SourceMappingOptions.ID;
                }
                else if (sourceRow.SourceName == _featureMapping.Mapping.X.Source)
                {
                    sourceRow.CurrentMapping = SourceMappingOptions.X;
                    dropdown.value = (int)SourceMappingOptions.X;
                }
                else if (sourceRow.SourceName == _featureMapping.Mapping.Y.Source)
                {
                    sourceRow.CurrentMapping = SourceMappingOptions.Y;
                    dropdown.value = (int)SourceMappingOptions.Y;
                }
                else if (sourceRow.SourceName == _featureMapping.Mapping.Z.Source)
                {
                    sourceRow.CurrentMapping = SourceMappingOptions.Z;
                    dropdown.value = (int)SourceMappingOptions.Z;
                }
                else if (sourceRow.SourceName == _featureMapping.Mapping.XMin.Source)
                {
                    sourceRow.CurrentMapping = SourceMappingOptions.Xmin;
                    dropdown.value = (int)SourceMappingOptions.Xmin;
                }
                else if (sourceRow.SourceName == _featureMapping.Mapping.XMax.Source)
                {
                    sourceRow.CurrentMapping = SourceMappingOptions.Xmax;
                    dropdown.value = (int)SourceMappingOptions.Xmax;
                }
                else if (sourceRow.SourceName == _featureMapping.Mapping.YMin.Source)
                {
                    sourceRow.CurrentMapping = SourceMappingOptions.Ymin;
                    dropdown.value = (int)SourceMappingOptions.Ymin;
                }
                else if (sourceRow.SourceName == _featureMapping.Mapping.YMax.Source)
                {
                    sourceRow.CurrentMapping = SourceMappingOptions.Ymax;
                    dropdown.value = (int)SourceMappingOptions.Ymax;
                }
                else if (sourceRow.SourceName == _featureMapping.Mapping.ZMin.Source)
                {
                    sourceRow.CurrentMapping = SourceMappingOptions.Zmin;
                    dropdown.value = (int)SourceMappingOptions.Zmin;
                }
                else if (sourceRow.SourceName == _featureMapping.Mapping.ZMax.Source)
                {
                    sourceRow.CurrentMapping = SourceMappingOptions.Zmax;
                    dropdown.value = (int)SourceMappingOptions.Zmax;
                }
                else if (sourceRow.SourceName == _featureMapping.Mapping.RA.Source)
                {
                    sourceRow.CurrentMapping = SourceMappingOptions.Ra;
                    dropdown.value = (int)SourceMappingOptions.Ra;
                }
                else if (sourceRow.SourceName == _featureMapping.Mapping.Dec.Source)
                {
                    sourceRow.CurrentMapping = SourceMappingOptions.Dec;
                    dropdown.value = (int)SourceMappingOptions.Dec;
                }
                else if (sourceRow.SourceName == _featureMapping.Mapping.Vel.Source)
                {
                    sourceRow.CurrentMapping = SourceMappingOptions.Velo;
                    dropdown.value = (int)SourceMappingOptions.Velo;
                }
                else if (sourceRow.SourceName == _featureMapping.Mapping.Freq.Source)
                {
                    sourceRow.CurrentMapping = SourceMappingOptions.Freq;
                    dropdown.value = (int)SourceMappingOptions.Freq;
                }
                else if (sourceRow.SourceName == _featureMapping.Mapping.Redshift.Source)
                {
                    sourceRow.CurrentMapping = SourceMappingOptions.Redshift;
                    dropdown.value = (int)SourceMappingOptions.Redshift;
                }
                else if (sourceRow.SourceName == _featureMapping.Mapping.Flag.Source)
                {
                    sourceRow.CurrentMapping = SourceMappingOptions.Flag;
                    dropdown.value = (int)SourceMappingOptions.Flag;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Error while loading mapping file. Check that all mappings are included: " + ex.Message);
            }
        }
    }

    public void ChangeHduSelection(TMP_Dropdown dropdown)
    {
        LoadingText.SetActive(false);
        IntPtr fptr;
        int status = 0;
        _hduSelectionIndex = dropdown.value;
        if (FitsReader.FitsOpenFile(out fptr, _imagePath, out status, true) != 0)
        {
            Debug.Log("Fits open failure... code #" + status.ToString());
        }
        FitsReader.FitsMovabsHdu(fptr, _hduSelectionIndex + 1, out int hdutype, out status);
        UpdateHeaderFromFits(fptr);
        FitsReader.FitsCloseFile(fptr, out status);
        //if it is valid enable loading button and subset selector
        if (IsLoadable())
        {
            informationPanelContent.gameObject.transform.Find("MaskFile_container").gameObject.transform.Find("Button").GetComponent<Button>().interactable = true;
            informationPanelContent.gameObject.transform.Find("Loading_container").gameObject.transform.Find("Button").GetComponent<Button>().interactable = true;
            informationPanelContent.gameObject.transform.Find("SubsetSelection_container").gameObject.SetActive(true);
            setSubsetBounds();
        }
        else
        {
            informationPanelContent.gameObject.transform.Find("MaskFile_container").gameObject.transform.Find("Button").GetComponent<Button>().interactable = false;
            informationPanelContent.gameObject.transform.Find("Loading_container").gameObject.transform.Find("Button").GetComponent<Button>().interactable = false;
            informationPanelContent.gameObject.transform.Find("SubsetSelection_container").gameObject.SetActive(false);
            loadTextLabel.text = "Not enough dimensions in selected image";
            LoadingText.SetActive(true);
        }
    }
    
    public void SaveMappingFile()
    {
        string lastPath = PlayerPrefs.GetString("LastPath");
        if (!Directory.Exists(lastPath))
            lastPath = null;

        var extensionList = new[]
        {
            new ExtensionFilter("JSON", "json"),
        };

        StandaloneFileBrowser.SaveFilePanelAsync("Save File", lastPath, "", extensionList, (string path) =>
        {
            if (path != "")
            {
                PlayerPrefs.SetString("LastPath", Path.GetDirectoryName(path));
                PlayerPrefs.Save();

                _saveMappingFile(path);
            }
        });
    }

    private void _saveMappingFile(string path)
    {
        Dictionary<SourceMappingOptions, MapEntry> mapping = new Dictionary<SourceMappingOptions, MapEntry>();
        List<string> importedColumns = new List<string>();
        for (int i = 0; i < _sourceRowObjects.Length; i++)
        {
            var row = _sourceRowObjects[i].GetComponent<SourceRow>();
            if (_sourceRowObjects[i].transform.Find("Import_toggle").gameObject.GetComponent<Toggle>().isOn)
                importedColumns.Add(row.SourceName);
            if (row.CurrentMapping != SourceMappingOptions.none)
                mapping.Add(row.CurrentMapping, new MapEntry { Source = row.SourceName });
        }

        var mappingObject = new Mapping
        {
            ID = mapping.ContainsKey(SourceMappingOptions.ID) ? mapping[SourceMappingOptions.ID] : new MapEntry { Source = "" },
            X = mapping.ContainsKey(SourceMappingOptions.X) ? mapping[SourceMappingOptions.X] : new MapEntry { Source = "" },
            Y = mapping.ContainsKey(SourceMappingOptions.Y) ? mapping[SourceMappingOptions.Y] : new MapEntry { Source = "" },
            Z = mapping.ContainsKey(SourceMappingOptions.Z) ? mapping[SourceMappingOptions.Z] : new MapEntry { Source = "" },
            XMin = mapping.ContainsKey(SourceMappingOptions.Xmin) ? mapping[SourceMappingOptions.Xmin] : new MapEntry { Source = "" },
            XMax = mapping.ContainsKey(SourceMappingOptions.Xmax) ? mapping[SourceMappingOptions.Xmax] : new MapEntry { Source = "" },
            YMin = mapping.ContainsKey(SourceMappingOptions.Ymin) ? mapping[SourceMappingOptions.Ymin] : new MapEntry { Source = "" },
            YMax = mapping.ContainsKey(SourceMappingOptions.Ymax) ? mapping[SourceMappingOptions.Ymax] : new MapEntry { Source = "" },
            ZMin = mapping.ContainsKey(SourceMappingOptions.Zmin) ? mapping[SourceMappingOptions.Zmin] : new MapEntry { Source = "" },
            ZMax = mapping.ContainsKey(SourceMappingOptions.Zmax) ? mapping[SourceMappingOptions.Zmax] : new MapEntry { Source = "" },
            RA = mapping.ContainsKey(SourceMappingOptions.Ra) ? mapping[SourceMappingOptions.Ra] : new MapEntry { Source = "" },
            Dec = mapping.ContainsKey(SourceMappingOptions.Dec) ? mapping[SourceMappingOptions.Dec] : new MapEntry { Source = "" },
            Vel = mapping.ContainsKey(SourceMappingOptions.Velo) ? mapping[SourceMappingOptions.Velo] : new MapEntry { Source = "" },
            Freq = mapping.ContainsKey(SourceMappingOptions.Freq) ? mapping[SourceMappingOptions.Freq] : new MapEntry { Source = "" },
            Redshift = mapping.ContainsKey(SourceMappingOptions.Redshift) ? mapping[SourceMappingOptions.Redshift] : new MapEntry { Source = "" },
            Flag = mapping.ContainsKey(SourceMappingOptions.Flag) ? mapping[SourceMappingOptions.Flag] : new MapEntry { Source = "" },
            ImportedColumns = importedColumns.ToArray()
        };
        var featureMappingObject = new FeatureMapping { Mapping = mappingObject };
        featureMappingObject.SaveMappingToFile(path);
    }

    public void ChangeSourceMapping(int sourceIndex, SourceMappingOptions option)
    {
        for (var i = 0; i < _sourceRowObjects.Length; i++)
        {
            if (i == sourceIndex)
                continue;
            var sourceRow = _sourceRowObjects[i].GetComponent<SourceRow>();
            if (AreMappingsIncompatible(option, sourceRow.CurrentMapping))
            {
                sourceRow.CurrentMapping = SourceMappingOptions.none;
                _sourceRowObjects[i].transform.Find("Coord_dropdown").gameObject.GetComponent<TMP_Dropdown>().value = 0;
            }
        }
    }

    private bool AreMappingsIncompatible(SourceMappingOptions option1, SourceMappingOptions option2)
    {
        return option1 == option2 ||
               (option1 == SourceMappingOptions.X || option1 == SourceMappingOptions.Y || option1 == SourceMappingOptions.Z) &&
               (option2 == SourceMappingOptions.Ra || option2 == SourceMappingOptions.Dec || option2 == SourceMappingOptions.Velo || option2 == SourceMappingOptions.Freq ||
                option2 == SourceMappingOptions.Redshift) ||
               (option2 == SourceMappingOptions.X || option2 == SourceMappingOptions.Y || option2 == SourceMappingOptions.Z) &&
               (option1 == SourceMappingOptions.Ra || option1 == SourceMappingOptions.Dec || option1 == SourceMappingOptions.Velo || option1 == SourceMappingOptions.Freq ||
                option1 == SourceMappingOptions.Redshift) ||
               option1 == SourceMappingOptions.Velo && (option2 == SourceMappingOptions.Freq || option2 == SourceMappingOptions.Redshift) ||
               option1 == SourceMappingOptions.Freq && (option2 == SourceMappingOptions.Redshift || option2 == SourceMappingOptions.Velo) ||
               option1 == SourceMappingOptions.Redshift && (option2 == SourceMappingOptions.Freq || option2 == SourceMappingOptions.Velo);
    }

    private bool AreMinimalMappingsSet()
    {
        List<SourceMappingOptions> setOptions = new List<SourceMappingOptions>();
        foreach (var row in _sourceRowObjects)
        {
            var currentMapping = row.GetComponent<SourceRow>().CurrentMapping;
            if (currentMapping != SourceMappingOptions.none)
                setOptions.Add(currentMapping);
        }

        bool spatialIsSet = setOptions.Contains(SourceMappingOptions.X) && setOptions.Contains(SourceMappingOptions.Y) && setOptions.Contains(SourceMappingOptions.Z) ||
                            setOptions.Contains(SourceMappingOptions.Ra) && setOptions.Contains(SourceMappingOptions.Dec) &&
                            (setOptions.Contains(SourceMappingOptions.Freq) || setOptions.Contains(SourceMappingOptions.Velo) ||
                             setOptions.Contains(SourceMappingOptions.Redshift)) ||
                            setOptions.Contains(SourceMappingOptions.Xmin);
        bool boxCornersWork = !setOptions.Contains(SourceMappingOptions.Xmin) && !setOptions.Contains(SourceMappingOptions.Xmax) &&
                              !setOptions.Contains(SourceMappingOptions.Ymin) && !setOptions.Contains(SourceMappingOptions.Ymax) &&
                              !setOptions.Contains(SourceMappingOptions.Zmin) && !setOptions.Contains(SourceMappingOptions.Zmax) ||
                              setOptions.Contains(SourceMappingOptions.Xmin) && setOptions.Contains(SourceMappingOptions.Xmax) &&
                              setOptions.Contains(SourceMappingOptions.Ymin) && setOptions.Contains(SourceMappingOptions.Ymax) &&
                              setOptions.Contains(SourceMappingOptions.Zmin) && setOptions.Contains(SourceMappingOptions.Zmax);
        return spatialIsSet && boxCornersWork;
    }

    public void LoadSourcesFile()
    {
        var loadingText = sourcesPanelContent.gameObject.transform.Find("Lower_container").gameObject.transform.Find("SourcesLoad_container").gameObject.transform.Find("Text").gameObject;
        var excludeExternalSources = sourcesPanelContent.gameObject.transform.Find("Lower_container").gameObject.transform.Find("SourcesLoad_container").gameObject
            .transform.Find("ExternalSourcesToggle").gameObject.GetComponent<Toggle>().isOn;    
        loadingText.GetComponent<TextMeshProUGUI>().color = new Color(0, 0.6f, 0.1f);
        loadingText.SetActive(true);
        bool[] columnsMask = new bool[_sourceRowObjects.Length];
        if (!AreMinimalMappingsSet())
        {
            Debug.Log("Minimal source mappings not set!");
            loadingText.GetComponent<TextMeshProUGUI>().color = Color.red;
            loadingText.GetComponent<TextMeshProUGUI>().text = "Spatial coordinates not set!";
            return;
        }

        var featureSetManager = GetFirstActiveRenderer().GetComponentInChildren<FeatureSetManager>();
        Dictionary<SourceMappingOptions, string> finalMapping = new Dictionary<SourceMappingOptions, string>();
        for (int i = 0; i < _sourceRowObjects.Length; i++)
        {
            var row = _sourceRowObjects[i].GetComponent<SourceRow>();
            if (row.CurrentMapping != SourceMappingOptions.none)
                finalMapping.Add(row.CurrentMapping, row.SourceName);
            columnsMask[i] = _sourceRowObjects[i].transform.Find("Import_toggle").gameObject.GetComponent<Toggle>().isOn;
        }

        if (featureSetManager.FeatureFileToLoad != "")
        {
            featureSetManager.ImportFeatureSetFromTable(finalMapping, FeatureTable.GetFeatureTableFromFile(_sourcesPath), Path.GetFileName(_sourcesPath), columnsMask, excludeExternalSources);
        }
        loadingText.GetComponent<TextMeshProUGUI>().text = $"Successfully loaded sources from:{Environment.NewLine}{Path.GetFileName(_sourcesPath)}";
        sourcesPanelContent.gameObject.transform.Find("Lower_container").gameObject.transform.Find("SourcesLoad_container").gameObject.transform.Find("Button").GetComponent<Button>().interactable = false;
    }

    public void DismissFileLoad()
    {
        fileLoadCanvassDesktop.SetActive(false);
        mainCanvassDesktop.SetActive(true);
    }

    public void Exit()
    {
        StopAllCoroutines();

        var initOpenVR = (!SteamVR.active && !SteamVR.usingNativeSupport);
        if (initOpenVR)
            OpenVR.Shutdown();

        Application.Quit();
    }

    private VolumeDataSetRenderer GetFirstActiveRenderer()
    {
        if (_volumeDataSetRenderers != null)
        {
            foreach (var dataSet in _volumeDataSetRenderers)
            {
                if (dataSet != null && dataSet.isActiveAndEnabled)
                {
                    return dataSet;
                }
            }
        }

        return null;
    }

    void OnGUI()
    {
        if (_showPopUp)
        {
            GUI.backgroundColor = new Color(1, 0, 0, 1f);
            GUI.Window(0, new Rect((Screen.width / 2) - 150, (Screen.height / 2) - 75
                , 300, 250), ShowGUI, "Invalid Cube");
        }
    }

    void ShowGUI(int windowID)
    {
// You may put a label to show a message to the player
        GUI.Label(new Rect(65, 40, 300, 250), _textPopUp);
// You may put a button to close the pop up too
        if (GUI.Button(new Rect(50, 150, 75, 30), "OK"))
        {
            _showPopUp = false;
            _textPopUp = "";
            // you may put other code to run according to your game too
        }
    }

    private void populateStatsValue()
    {
        var volumeDataSet = GetFirstActiveRenderer().Data;

        Transform stats = statsPanelContent.gameObject.transform.Find("Stats_container").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject
            .transform.Find("Stats");
        stats.gameObject.transform.Find("Line_min").gameObject.transform.Find("InputField_min").GetComponent<TMP_InputField>().text = volumeDataSet.MinValue.ToString();
        stats.gameObject.transform.Find("Line_max").gameObject.transform.Find("InputField_max").GetComponent<TMP_InputField>().text = volumeDataSet.MaxValue.ToString();
        stats.gameObject.transform.Find("Line_std").gameObject.transform.Find("Text_std").GetComponent<TextMeshProUGUI>().text = volumeDataSet.StanDev.ToString();
        stats.gameObject.transform.Find("Line_mean").gameObject.transform.Find("Text_mean").GetComponent<TextMeshProUGUI>().text = volumeDataSet.MeanValue.ToString();
        _histogramHelper.CreateHistogramImg(volumeDataSet.Histogram, volumeDataSet.HistogramBinWidth, volumeDataSet.MinValue, volumeDataSet.MaxValue, volumeDataSet.MeanValue,
            volumeDataSet.StanDev);
    }

    private void populateColorMapDropdown()
    {
        renderingPanelContent.gameObject.transform.Find("Rendering_container").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject.transform
            .Find("Settings").gameObject.transform.Find("Colormap_container").gameObject.transform.Find("Dropdown_colormap").GetComponent<TMP_Dropdown>().options.Clear();

        foreach (var colorMap in Enum.GetValues(typeof(ColorMapEnum)))
        {
            renderingPanelContent.gameObject.transform.Find("Rendering_container").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject.transform
                .Find("Settings").gameObject.transform.Find("Colormap_container").gameObject.transform.Find("Dropdown_colormap").GetComponent<TMP_Dropdown>().options
                .Add((new TMP_Dropdown.OptionData() { text = colorMap.ToString() }));
        }

        renderingPanelContent.gameObject.transform.Find("Rendering_container").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject.transform
                .Find("Settings").gameObject.transform.Find("Colormap_container").gameObject.transform.Find("Dropdown_colormap").GetComponent<TMP_Dropdown>().value =
            Config.Instance.defaultColorMap.GetHashCode();
    }

    public void ChangeColorMap()
    {
        var firstActiveRenderer = GetFirstActiveRenderer();
        if (firstActiveRenderer != null)
        {
            _activeColorMap = ColorMapUtils.FromHashCode(renderingPanelContent.gameObject.transform.Find("Rendering_container").gameObject.transform.Find("Viewport").gameObject
                .transform.Find("Content").gameObject.transform.Find("Settings").gameObject.transform.Find("Colormap_container").gameObject.transform.Find("Dropdown_colormap")
                .GetComponent<TMP_Dropdown>().value);
            firstActiveRenderer.ColorMap = _activeColorMap;
        }
    }

    public void UpdateSigma(Int32 optionIndex)
    {
        float sigma = optionIndex + 1f;
        float histMin = float.Parse(statsPanelContent.gameObject.transform.Find("Stats_container").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content")
            .gameObject.transform.Find("Stats").gameObject.transform.Find("Line_min")
            .gameObject.transform.Find("InputField_min").GetComponent<TMP_InputField>().text);
        float histMax = float.Parse(statsPanelContent.gameObject.transform.Find("Stats_container").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content")
            .gameObject.transform.Find("Stats").gameObject.transform.Find("Line_max")
            .gameObject.transform.Find("InputField_max").GetComponent<TMP_InputField>().text);
        var volumeDataSet = GetFirstActiveRenderer().Data;
        _histogramHelper.CreateHistogramImg(volumeDataSet.Histogram, volumeDataSet.HistogramBinWidth, histMin, histMax, volumeDataSet.MeanValue, volumeDataSet.StanDev, sigma);
    }

    public void RestoreDefaults()
    {
        statsPanelContent.gameObject.transform.Find("Stats_container").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject.transform.Find("Stats")
            .gameObject.transform.Find("Line_sigma").gameObject.transform.Find("Dropdown").GetComponent<TMP_Dropdown>().value = 0;

        var firstActiveRenderer = GetFirstActiveRenderer();
        VolumeDataSet.UpdateHistogram(firstActiveRenderer.Data, firstActiveRenderer.Data.MinValue, firstActiveRenderer.Data.MaxValue);
        firstActiveRenderer.ResetThresholds();
        populateStatsValue();
    }
    
    /// <summary>
    /// Function to update the minimum and maximum scale data values of the histogram
    /// essentially where the colormap begins and ends on the data
    /// </summary>
    /// <param name="min"></param>
    /// <param name="max"></param>
    public void UpdateScale(float min, float max)
    {
        var firstActiveRenderer = GetFirstActiveRenderer();
        VolumeDataSet volumeDataSet = firstActiveRenderer.Data;
        float sigma = statsPanelContent.gameObject.transform.Find("Stats_container").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject.transform
            .Find("Stats").gameObject.transform.Find("Line_sigma")
            .gameObject.transform.Find("Dropdown").GetComponent<TMP_Dropdown>().value + 1f;
        firstActiveRenderer.ScaleMin = min;
        firstActiveRenderer.ScaleMax = max;
        VolumeDataSet.UpdateHistogram(volumeDataSet, min, max);
        _histogramHelper.CreateHistogramImg(volumeDataSet.Histogram, volumeDataSet.HistogramBinWidth, min, max, volumeDataSet.MeanValue, volumeDataSet.StanDev, sigma);
        firstActiveRenderer.ResetThresholds();
    }
    
    /// <summary>
    /// Function to set the minimum and maximum scale data values of the histogram to the given percentiles
    /// </summary>
    /// <param name="maxPercentile"></param>
    public void SetMaxMinPercentile(float maxPercentile)
    {
        var config = Config.Instance;
        float minPercentileValue, maxPercentileValue;
        var minPercentile = 100 - maxPercentile;
        var dataSet = GetFirstActiveRenderer().Data;
        if (maxPercentile == 100)
        {
            minPercentileValue = dataSet.MinValue;
            maxPercentileValue = dataSet.MaxValue;
        }
        // Use the quick percentile calculation if the option is enabled
        else if (config.useQuickModeForPercentiles)
        {
            IntPtr histogramPtr = IntPtr.Zero;
            if (dataSet.FullHistogram != null)
            {
                histogramPtr = Marshal.AllocHGlobal(dataSet.FullHistogram.Length * sizeof(int));
                Marshal.Copy(dataSet.FullHistogram, 0, histogramPtr, dataSet.FullHistogram.Length);
            }

            if (DataAnalysis.GetPercentileValuesFromHistogram(histogramPtr, dataSet.FullHistogram.Length,
                    dataSet.MinValue, dataSet.MaxValue, minPercentile,
                    maxPercentile, out minPercentileValue, out maxPercentileValue) != 0)
            {
                Debug.LogError("Error calculating percentiles from histogram.");
            }
            Marshal.FreeHGlobal(histogramPtr);
        }
        // otherwise, use the more precise method of calculating percentiles from the data
        else 
        {
            if (DataAnalysis.GetPercentileValuesFromData(dataSet.FitsData, dataSet.NumPoints,
                    minPercentile, maxPercentile, out minPercentileValue, out maxPercentileValue) != 0)
            {
                Debug.LogError("Error calculating percentiles from data.");
            }
        }
        
        Debug.Log("Setting histogram scale min to percentiles: " + minPercentile + "% and " + maxPercentile + "% with values: " + minPercentileValue + " and " + maxPercentileValue + ".");
        UpdateScale(minPercentileValue, maxPercentileValue);
    }
    
    /// <summary>
    /// Function to expose setting the minimum histogram scale value to the UI
    /// </summary>
    /// <param name="minString"></param>
    public void UpdateScaleMin(string minString)
    {
        float max = float.Parse(statsPanelContent.gameObject.transform.Find("Stats_container").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content")
            .gameObject.transform.Find("Stats").gameObject.transform.Find("Line_max")
            .gameObject.transform.Find("InputField_max").GetComponent<TMP_InputField>().text);
        UpdateScale(float.Parse(minString), max); 
    }

    /// <summary>
    /// Function to expose setting the maximum histogram scale value to the UI
    /// </summary>
    /// <param name="maxString"></param>
    public void UpdateScaleMax(string maxString)
    {
        float min = float.Parse(statsPanelContent.gameObject.transform.Find("Stats_container").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content")
            .gameObject.transform.Find("Stats").gameObject.transform.Find("Line_min")
            .gameObject.transform.Find("InputField_min").GetComponent<TMP_InputField>().text);
        UpdateScale(min, float.Parse(maxString));
    }

    public void UpdateThresholdMin(float value)
    {
        var firstActiveRenderer = GetFirstActiveRenderer();
        if (firstActiveRenderer != null)
        {
            firstActiveRenderer.ThresholdMin = Mathf.Clamp(value, 0, firstActiveRenderer.ThresholdMax);
        }
    }

    public void UpdateThresholdMax(float value)
    {
        var firstActiveRenderer = GetFirstActiveRenderer();
        if (firstActiveRenderer != null)
        {
            firstActiveRenderer.ThresholdMax = Mathf.Clamp(value, firstActiveRenderer.ThresholdMin, 1);
        }
    }

    public void ResetThresholds()
    {
        var firstActiveRenderer = GetFirstActiveRenderer();
        if (firstActiveRenderer != null)
        {
            firstActiveRenderer.ThresholdMin = firstActiveRenderer.InitialThresholdMin;
            _minThreshold.value = firstActiveRenderer.ThresholdMin;

            firstActiveRenderer.ThresholdMax = firstActiveRenderer.InitialThresholdMax;
            _maxThreshold.value = firstActiveRenderer.ThresholdMax;
        }
    }

    public void UpdateUI(float min, float max, Sprite img)
    {
        statsPanelContent.gameObject.transform.Find("Stats_container").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject.transform.Find("Stats")
            .gameObject.transform.Find("Line_min")
            .gameObject.transform.Find("InputField_min").GetComponent<TMP_InputField>().text = min.ToString();
        statsPanelContent.gameObject.transform.Find("Stats_container").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject.transform.Find("Stats")
            .gameObject.transform.Find("Line_max")
            .gameObject.transform.Find("InputField_max").GetComponent<TMP_InputField>().text = max.ToString();
        statsPanelContent.gameObject.transform.Find("Histogram_container").gameObject.transform.Find("Histogram").GetComponent<Image>().sprite = img;
    }
}