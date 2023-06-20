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
using SFB;

public class CanvassDesktop : MonoBehaviour
{
    private VolumeDataSetRenderer[] _volumeDataSets;
    private GameObject volumeDataSetManager;
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

    private HistogramHelper histogramHelper;

    private bool showPopUp = false;
    private string textPopUp = "";
    private VolumeInputController _volumeInputController;
    private VolumeCommandController _volumeCommandController;
    string imagePath = "";
    string maskPath = "";
    string sourcesPath = "";

    private double imageNAxis = 0;
    private double imageSize = 1;
    private double maskNAxis = 0;
    private double maskSize = 1;

    private int subsetMin = 0;
    private int subsetMax_X = 1;
    private int subsetMax_Y = 1;
    private int subsetMax_Z = 1;
    private int[] subset;

    Dictionary<double, double> axisSize = null;
    Dictionary<double, double> maskAxisSize = null;

    private int ratioDropdownIndex = 0;

    private ColorMapEnum activeColorMap = ColorMapEnum.None;

    private Slider minThreshold;
    private TextMeshProUGUI minThresholdLabel;

    private Slider maxThreshold;
    private TextMeshProUGUI maxThresholdLabel;

    private float restFrequency;
    private FeatureMapping featureMapping;

    private Toggle subsetToggle;
    private TMP_InputField subset_XMin_input;
    private TMP_InputField subset_YMin_input;
    private TMP_InputField subset_ZMin_input;
    private TMP_InputField subset_XMax_input;
    private TMP_InputField subset_YMax_input;
    private TMP_InputField subset_ZMax_input;

    protected Coroutine loadCubeCoroutine;
    protected Coroutine showLoadDialogCoroutine;

    // Start is called before the first frame update
    void Start()
    {
        _volumeInputController = FindObjectOfType<VolumeInputController>();
        _volumeCommandController = FindObjectOfType<VolumeCommandController>();
        histogramHelper = FindObjectOfType<HistogramHelper>();

        checkCubesDataSet();

        minThreshold = renderingPanelContent.gameObject.transform.Find("Rendering_container").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject
            .transform.Find("Settings").gameObject.transform.Find("Threshold_container").gameObject.transform.Find("Threshold_min").gameObject.transform.Find("Slider")
            .GetComponent<Slider>();
        minThresholdLabel = renderingPanelContent.gameObject.transform.Find("Rendering_container").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content")
            .gameObject.transform.Find("Settings").gameObject.transform.Find("Threshold_container").gameObject.transform.Find("Threshold_min").gameObject.transform
            .Find("Min_label").GetComponent<TextMeshProUGUI>();

        maxThreshold = renderingPanelContent.gameObject.transform.Find("Rendering_container").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject
            .transform.Find("Settings").gameObject.transform.Find("Threshold_container").gameObject.transform.Find("Threshold_max").gameObject.transform.Find("Slider")
            .GetComponent<Slider>();
        maxThresholdLabel = renderingPanelContent.gameObject.transform.Find("Rendering_container").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content")
            .gameObject.transform.Find("Settings").gameObject.transform.Find("Threshold_container").gameObject.transform.Find("Threshold_max").gameObject.transform
            .Find("Max_label").GetComponent<TextMeshProUGUI>();

        subsetToggle = informationPanelContent.gameObject.transform.Find("SubsetSelection_container").gameObject.transform.Find("LoadSubset_Toggle").GetComponent<Toggle>();
        // subsetToggle.onValueChanged.AddListener(onSubsetToggleSelected);
        subset_XMin_input = informationPanelContent.gameObject.transform.Find("SubsetMin_container").gameObject.transform.Find("SubsetX_min").GetComponent<TMP_InputField>();
        subset_XMin_input.onEndEdit.AddListener(checkSubsetBounds);
        subset_YMin_input = informationPanelContent.gameObject.transform.Find("SubsetMin_container").gameObject.transform.Find("SubsetY_min").GetComponent<TMP_InputField>();
        subset_YMin_input.onEndEdit.AddListener(checkSubsetBounds);
        subset_ZMin_input = informationPanelContent.gameObject.transform.Find("SubsetMin_container").gameObject.transform.Find("SubsetZ_min").GetComponent<TMP_InputField>();
        subset_ZMin_input.onEndEdit.AddListener(checkSubsetBounds);
        subset_XMax_input = informationPanelContent.gameObject.transform.Find("SubsetMax_container").gameObject.transform.Find("SubsetX_max").GetComponent<TMP_InputField>();
        subset_XMax_input.onEndEdit.AddListener(checkSubsetBounds);
        subset_YMax_input = informationPanelContent.gameObject.transform.Find("SubsetMax_container").gameObject.transform.Find("SubsetY_max").GetComponent<TMP_InputField>();
        subset_YMax_input.onEndEdit.AddListener(checkSubsetBounds);
        subset_ZMax_input = informationPanelContent.gameObject.transform.Find("SubsetMax_container").gameObject.transform.Find("SubsetZ_max").GetComponent<TMP_InputField>();
        subset_ZMax_input.onEndEdit.AddListener(checkSubsetBounds);

        subset_XMin_input.text = subsetMin.ToString();
        subset_YMin_input.text = subsetMin.ToString();
        subset_ZMin_input.text = subsetMin.ToString();
        subset_XMax_input.text = subsetMax_X.ToString();
        subset_YMax_input.text = subsetMax_Y.ToString();
        subset_ZMax_input.text = subsetMax_Z.ToString();
        subset = new int[6];
    }

    void checkCubesDataSet()
    {
        volumeDataSetManager = GameObject.Find("VolumeDataSetManager");
        if (volumeDataSetManager)
        {
            _volumeDataSets = volumeDataSetManager.GetComponentsInChildren<VolumeDataSetRenderer>(true);
        }
        else
        {
            _volumeDataSets = new VolumeDataSetRenderer[0];
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (GetFirstActiveDataSet() != null)
        {
            VolumeDataSetRenderer dataSet = GetFirstActiveDataSet();

            if (minThreshold.value > maxThreshold.value)
            {
                minThreshold.value = maxThreshold.value;
            }

            var effectiveMin = dataSet.ScaleMin + dataSet.ThresholdMin * (dataSet.ScaleMax - dataSet.ScaleMin);
            var effectiveMax = dataSet.ScaleMin + dataSet.ThresholdMax * (dataSet.ScaleMax - dataSet.ScaleMin);
            minThresholdLabel.text = effectiveMin.ToString();
            maxThresholdLabel.text = effectiveMax.ToString();

            if (dataSet.ThresholdMin != minThreshold.value)
            {
                minThreshold.value = dataSet.ThresholdMin;
            }

            if (dataSet.ThresholdMax != maxThreshold.value)
            {
                maxThreshold.value = dataSet.ThresholdMax;
            }


            if (dataSet.ColorMap != activeColorMap)
            {
                renderingPanelContent.gameObject.transform.Find("Rendering_container").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content")
                    .gameObject.transform.Find("Settings").gameObject.transform.Find("Colormap_container")
                    .gameObject.transform.Find("Dropdown_colormap").GetComponent<TMP_Dropdown>().value = (int)dataSet.ColorMap;
            }
        }
    }

    public void InformationTab()
    {
    }

    public void RenderingTab()
    {
    }

    public void BrowseImageFile()
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

                _browseImageFile(paths[0]);
            }
        });
    }

    private void _browseImageFile(string path)
    {
        if (path != null)
        {
            imageSize = 1;
            bool loadable = false;
            string localMsg = "";

            imagePath = path;

            //each time you select a fits image, reset the mask and disable loading button
            maskPath = "";
            informationPanelContent.gameObject.transform.Find("MaskFile_container").gameObject.transform.Find("Button").GetComponent<Button>().interactable = false;
            informationPanelContent.gameObject.transform.Find("MaskFile_container").gameObject.transform.Find("MaskFilePath_text").GetComponent<TextMeshProUGUI>().text = "...";
            informationPanelContent.gameObject.transform.Find("Loading_container").gameObject.transform.Find("Button").GetComponent<Button>().interactable = false;

            IntPtr fptr;
            int status = 0;

            if (FitsReader.FitsOpenFile(out fptr, imagePath, out status, true) != 0)
            {
                Debug.Log("Fits open failure... code #" + status.ToString());
            }

            axisSize = new Dictionary<double, double>();

            List<double> list = new List<double>();

            //set the path of selected file to the ui
            informationPanelContent.gameObject.transform.Find("ImageFile_container").gameObject.transform.Find("ImageFilePath_text").GetComponent<TextMeshProUGUI>().text =
                System.IO.Path.GetFileName(imagePath);

            //visualize the header into the scroll view
            string _header = "";
            IDictionary<string, string> _headerDictionary = FitsReader.ExtractHeaders(fptr, out status);
            FitsReader.FitsCloseFile(fptr, out status);

            foreach (KeyValuePair<string, string> entry in _headerDictionary)
            {
                //switch (entry.Key)
                if (entry.Key.Length > 4)
                    switch (entry.Key.Substring(0, 5))
                    {
                        case "NAXIS":
                            string sub = entry.Key.Substring(5);

                            if (sub == "")
                                imageNAxis = Convert.ToDouble(entry.Value, CultureInfo.InvariantCulture);
                            else
                                axisSize.Add(Convert.ToDouble(sub, CultureInfo.InvariantCulture), Convert.ToDouble(entry.Value, CultureInfo.InvariantCulture));
                            break;
                    }

                _header += entry.Key + "\t\t " + entry.Value + "\n";
            }

            informationPanelContent.gameObject.transform.Find("Header_container").gameObject.transform.Find("Scroll View").gameObject.transform.Find("Viewport").gameObject
                .transform.Find("Content").gameObject.transform.Find("Header").GetComponent<TextMeshProUGUI>().text = _header;
            informationPanelContent.gameObject.transform.Find("Header_container").gameObject.transform.Find("Scroll View").gameObject.transform.Find("Scrollbar Vertical")
                .GetComponent<Scrollbar>().value = 1;

            //check if it is a valid fits cube
            if (imageNAxis > 2)
            {
                if (imageNAxis == 3)
                {
                    //check if all 3 axis dim are > 1
                    //foreach (var axes in axisSize)
                    foreach (KeyValuePair<double, double> axes in axisSize)
                    {
                        localMsg += "Axis[" + axes.Key + "]: " + axes.Value + "\n";
                        if (axes.Value > 1)
                        {
                            list.Add(axes.Key);
                            imageSize *= axes.Value;
                        }
                    }

                    //if the cube have just 3 axis with n element > 3 is valid
                    if (list.Count == 3)
                    {
                        loadable = true;
                    }
                }
                //more than 3 axis
                else
                {
                    // more than 3 axis, check if axis dim are > 1
                    foreach (KeyValuePair<double, double> axes in axisSize)
                    {
                        localMsg += "Axis[" + axes.Key + "]: " + axes.Value + "\n";
                        if (axes.Value > 1)
                        {
                            list.Add(axes.Key);
                            imageSize *= axes.Value;
                        }
                    }

                    //more than 3 axis but just 3 axis have nelement > 1
                    if (list.Count == 3)
                    {
                        loadable = true;
                    }
                    else
                        informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.SetActive(true);
                }

                //update dropdow
                informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("Z_Dropdown").GetComponent<TMP_Dropdown>().interactable = false;
                informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("Z_Dropdown").GetComponent<TMP_Dropdown>().ClearOptions();

                foreach (KeyValuePair<double, double> axes in axisSize)
                {
                    if (axes.Value > 1 && axes.Key > 2)
                    {
                        informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("Z_Dropdown").GetComponent<TMP_Dropdown>().options
                            .Add((new TMP_Dropdown.OptionData() { text = axes.Key.ToString() }));
                    }
                }

                informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("Z_Dropdown").GetComponent<TMP_Dropdown>().RefreshShownValue();
                informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("Z_Dropdown").GetComponent<TMP_Dropdown>().value = 0;
                //end update dropdown

                //Cube is not loadable with valid axis < 3
                if (!loadable && list.Count < 3)
                {
                    showPopUp = true;
                    textPopUp = "NAxis_ " + imageNAxis + "\n" + localMsg;
                }
                //cube is not loadable with more than 3 axis with nelement
                else if (!loadable && list.Count > 3)
                {
                    informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("Z_Dropdown").GetComponent<TMP_Dropdown>().interactable = true;

                    loadable = true;
                }
            }
            else
            {
                loadable = false;
                localMsg = "Please select a valid cube!";
            }

            //if it is valid enable loading button and subset selector
            if (loadable)
            {
                informationPanelContent.gameObject.transform.Find("MaskFile_container").gameObject.transform.Find("Button").GetComponent<Button>().interactable = true;
                informationPanelContent.gameObject.transform.Find("Loading_container").gameObject.transform.Find("Button").GetComponent<Button>().interactable = true;
                informationPanelContent.gameObject.transform.Find("SubsetSelection_container").gameObject.SetActive(true);


            }
        }

        if (showLoadDialogCoroutine != null)
            StopCoroutine(showLoadDialogCoroutine);
    }

    public void onSubsetToggleSelected(bool val)
    {
        if (subsetToggle.isOn)
        {
            informationPanelContent.gameObject.transform.Find("SubsetLabel_container").gameObject.SetActive(true);
            informationPanelContent.gameObject.transform.Find("SubsetMin_container").gameObject.SetActive(true);
            informationPanelContent.gameObject.transform.Find("SubsetMax_container").gameObject.SetActive(true);
        }
        else
        {
            informationPanelContent.gameObject.transform.Find("SubsetLabel_container").gameObject.SetActive(false);
            informationPanelContent.gameObject.transform.Find("SubsetMin_container").gameObject.SetActive(false);
            informationPanelContent.gameObject.transform.Find("SubsetMax_container").gameObject.SetActive(false);
        }
    }

    public void checkSubsetBounds(string val)
    {
        Debug.Log("One of the subset input fields had its value changed to " + val);
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

        if (maskPath != null)
        {
            informationPanelContent.gameObject.transform.Find("Loading_container").gameObject.transform.Find("Button").GetComponent<Button>().interactable = false;
            maskSize = 1;
            maskPath = path;

            IntPtr fptr;
            int status = 0;

            if (FitsReader.FitsOpenFile(out fptr, maskPath, out status, true) != 0)
            {
                Debug.Log("Fits open failure... code #" + status.ToString());
            }

            informationPanelContent.gameObject.transform.Find("MaskFile_container").gameObject.transform.Find("MaskFilePath_text").GetComponent<TextMeshProUGUI>().text =
                System.IO.Path.GetFileName(maskPath);

            maskAxisSize = new Dictionary<double, double>();
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
                                maskNAxis = Convert.ToDouble(entry.Value, CultureInfo.InvariantCulture);
                            else
                            {
                                maskAxisSize.Add(Convert.ToDouble(sub, CultureInfo.InvariantCulture), Convert.ToDouble(entry.Value, CultureInfo.InvariantCulture));
                            }

                            break;
                    }
            }

            if (maskNAxis > 2)
            {
                //Get Axis size from Image Cube
                int i2 = int.Parse(informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("Z_Dropdown").GetComponent<TMP_Dropdown>()
                             .options[
                                 informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("Z_Dropdown").GetComponent<TMP_Dropdown>().value]
                             .text) -
                         1;
                if (axisSize[1] == maskAxisSize[1] && axisSize[2] == maskAxisSize[2] && axisSize[i2 + 1] == maskAxisSize[3])
                {
                    loadable = true;
                    informationPanelContent.gameObject.transform.Find("Loading_container").gameObject.transform.Find("Button").GetComponent<Button>().interactable = true;
                }
                else
                    loadable = false;
            }

            if (!loadable)
            {
                //mask is not valid
                informationPanelContent.gameObject.transform.Find("MaskFile_container").gameObject.transform.Find("MaskFilePath_text").GetComponent<TextMeshProUGUI>().text = "...";
                maskPath = "";
                showPopUp = true;
                textPopUp = "Selected Mask\ndoesn't match image file";
            }
        }

        if (showLoadDialogCoroutine != null)
            StopCoroutine(showLoadDialogCoroutine);
    }

    public void CheckImgMaskAxisSize()
    {
        if (maskPath != "")
        {
            //Get Axis size from Image Cube
            int i2 = int.Parse(informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("Z_Dropdown").GetComponent<TMP_Dropdown>()
                .options[informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("Z_Dropdown").GetComponent<TMP_Dropdown>().value].text) - 1;

            if (axisSize[1] != maskAxisSize[1] || axisSize[2] != maskAxisSize[2] || axisSize[i2 + 1] != maskAxisSize[3])
            {
                informationPanelContent.gameObject.transform.Find("MaskFile_container").gameObject.transform.Find("MaskFilePath_text").GetComponent<TextMeshProUGUI>().text = "...";
                showPopUp = true;
                textPopUp = "Selected axis size \ndoesn't match mask axis size";
                informationPanelContent.gameObject.transform.Find("Loading_container").gameObject.transform.Find("Button").GetComponent<Button>().interactable = false;
            }
            else
            {
                informationPanelContent.gameObject.transform.Find("Loading_container").gameObject.transform.Find("Button").GetComponent<Button>().interactable = true;
            }
        }
    }

    IEnumerator ShowSaveDialogCoroutine(int type)
    {
        yield return null;
    }

    /*
       var extensions = new[] {
            new ExtensionFilter("VOTables", "xml"),
            new ExtensionFilter("All Files", "*" ),
        };
        StandaloneFileBrowser.OpenFilePanelAsync("Open File", lastPath, extensions, false, (string[] paths) => {
            if (paths.Length == 1)
            {
                PlayerPrefs.SetString("LastPath", Path.GetDirectoryName(paths[0]));
                PlayerPrefs.Save();

                _browseSourcesFile(paths[0]);
            }
        });
    */

    public void LoadFileFromFileSystem()
    {
        StartCoroutine(LoadCubeCoroutine(imagePath, maskPath));
    }

    private void postLoadFileFileSystem()
    {
        if (loadCubeCoroutine != null)
            StopCoroutine(loadCubeCoroutine);

        VolumePlayer.SetActive(false);
        VolumePlayer.SetActive(true);

        renderingPanelContent.gameObject.transform.Find("Rendering_container").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject.transform
                .Find("Settings").gameObject.transform.Find("Mask_container").gameObject.transform.Find("Dropdown_mask").GetComponent<TMP_Dropdown>().interactable =
            GetFirstActiveDataSet().MaskFileName != "";

        populateColorMapDropdown();
        populateStatsValue();

        LoadingText.gameObject.SetActive(false);
        WelcomeMenu.gameObject.SetActive(false);

        mainCanvassDesktop.gameObject.transform.Find("RightPanel").gameObject.transform.Find("Tabs_ container").gameObject.transform.Find("Rendering_Button").GetComponent<Button>()
            .interactable = true;
        mainCanvassDesktop.gameObject.transform.Find("RightPanel").gameObject.transform.Find("Tabs_ container").gameObject.transform.Find("Stats_Button").GetComponent<Button>()
            .interactable = true;
        mainCanvassDesktop.gameObject.transform.Find("RightPanel").gameObject.transform.Find("Tabs_ container").gameObject.transform.Find("Sources_Button").GetComponent<Button>()
            .interactable = true;

        mainCanvassDesktop.gameObject.transform.Find("RightPanel").gameObject.transform.Find("Tabs_ container").gameObject.transform.Find("Stats_Button").GetComponent<Button>()
            .onClick.Invoke();
    }


    public IEnumerator LoadCubeCoroutine(string _imagePath, string _maskPath)
    {
        LoadingText.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.001f);

        float zScale = 1f;
        if (ratioDropdownIndex == 1)
        {
            // case X=Y, calculate z scale from NAXIS1 and NAXIS3
            int i2 = int.Parse(informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("Z_Dropdown").GetComponent<TMP_Dropdown>()
                .options[informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("Z_Dropdown").GetComponent<TMP_Dropdown>().value].text) - 1;

            double x, z;
            if (axisSize.TryGetValue(1, out x) && axisSize.TryGetValue(i2 + 1, out z))
            {
                zScale = (float)(z / x);
            }
        }

        var activeDataSet = GetFirstActiveDataSet();
        if (activeDataSet != null)
        {
            Debug.Log("Replacing data cube...");

            activeDataSet.transform.gameObject.SetActive(false);
            _volumeCommandController.RemoveDataSet(activeDataSet);
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
            activeDataSet.Data.CleanUp(activeDataSet.RandomVolume);
            activeDataSet.Mask?.CleanUp(false);
            Destroy(activeDataSet);
        }

        GameObject newCube = Instantiate(cubeprefab, new Vector3(0, 0f, 0), Quaternion.identity);
        newCube.transform.localScale = new Vector3(1, 1, zScale);
        newCube.SetActive(true);

        newCube.transform.SetParent(volumeDataSetManager.transform, false);

        newCube.GetComponent<VolumeDataSetRenderer>().FileName = _imagePath; //_dataSet.FileName.ToString();
        newCube.GetComponent<VolumeDataSetRenderer>().MaskFileName = _maskPath; // _maskDataSet.FileName.ToString();
        newCube.GetComponent<VolumeDataSetRenderer>().CubeDepthAxis = int.Parse(informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform
            .Find("Z_Dropdown").GetComponent<TMP_Dropdown>()
            .options[informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("Z_Dropdown").GetComponent<TMP_Dropdown>().value].text) - 1;
        informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("Z_Dropdown").GetComponent<TMP_Dropdown>().interactable = false;

        checkCubesDataSet();

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

        while (!newCube.GetComponent<VolumeDataSetRenderer>().started)
        {
            yield return new WaitForSeconds(.1f);
        }

        postLoadFileFileSystem();
    }

    public void OnRatioDropdownValueChanged(int optionIndex)
    {
        ratioDropdownIndex = optionIndex;
        var activeDataSet = GetFirstActiveDataSet();
        if (activeDataSet != null)
        {
            if (optionIndex == 0)
            {
                // X=Y=Z
                activeDataSet.ZScale = activeDataSet.XScale;
            }
            else
            {
                // X=Y
                activeDataSet.ZScale = activeDataSet.XScale * activeDataSet.GetCubeDimensions().z / activeDataSet.GetCubeDimensions().x;
            }
        }
    }

    public void OnRestFrequencyOverrideValueChanged(bool option)
    {
        var activeDataSet = GetFirstActiveDataSet();
        activeDataSet.OverrideRestFrequency = option;
        if (option)
        {
            activeDataSet.RestFrequency = restFrequency;
        }
        else
        {
            activeDataSet.ResetRestFrequency();
        }
    }

    public void OnRestFrequencyValueChanged(String val)
    {
        restFrequency = float.Parse(val);
        var activeDataSet = GetFirstActiveDataSet();
        if (activeDataSet.OverrideRestFrequency)
            activeDataSet.RestFrequency = restFrequency;
    }

    public void BrowseSourcesFile()
    {
        string lastPath = PlayerPrefs.GetString("LastPath");
        if (!Directory.Exists(lastPath))
            lastPath = "";
        var extensions = new[]
        {
            new ExtensionFilter("VOTables", "xml"),
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
        var volumeDataSet = GetFirstActiveDataSet();
        var featureDataSet = volumeDataSet.GetComponentInChildren<FeatureSetManager>();
        sourcesPath = path;
        featureDataSet.FeatureFileToLoad = path;
        sourcesPanelContent.gameObject.transform.Find("MappingSave_container").gameObject.transform.Find("Button").GetComponent<Button>().interactable = true;
        sourcesPanelContent.gameObject.transform.Find("SourcesLoad_container").gameObject.transform.Find("Button").GetComponent<Button>().interactable = true;
        //activate load features button
        sourcesPanelContent.gameObject.transform.Find("SourcesFile_container").gameObject.transform.Find("SourcesFilePath_text").GetComponent<TextMeshProUGUI>().text =
            System.IO.Path.GetFileName(path);
        VoTable voTable = FeatureMapper.GetVOTableFromFile(path); //be more flexible with file input (ascii)
        Transform sourceBody = sourcesPanelContent.gameObject.transform.Find("SourcesInfo_container").gameObject.transform.Find("Scroll View").gameObject.transform.Find("Viewport")
            .gameObject.transform.Find("Content").gameObject.transform;
        if (_sourceRowObjects != null)
        {
            foreach (var row in _sourceRowObjects)
                Destroy(row);
            _sourceRowObjects = null;
        }

        _sourceRowObjects = new GameObject[voTable.Column.Count];
        for (var i = 0; i < voTable.Column.Count; i++)
        {
            var row = Instantiate(SourceRowPrefab, sourceBody);
            row.transform.Find("Source_number").GetComponent<TextMeshProUGUI>().text = i.ToString();
            row.transform.Find("Source_name").GetComponent<TextMeshProUGUI>().text = voTable.Column[i].Name;
            var rowScript = row.GetComponentInParent<SourceRow>();
            rowScript.SourceName = voTable.Column[i].Name;
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
        featureMapping = FeatureMapping.GetMappingFromFile(path);
        foreach (var sourceRowObject in _sourceRowObjects)
        {
            var dropdown = sourceRowObject.transform.Find("Coord_dropdown").gameObject.GetComponent<TMP_Dropdown>();
            dropdown.value = 0;
            sourceRowObject.transform.Find("Import_toggle").gameObject.GetComponent<Toggle>().isOn = false;
        }

        foreach (var sourceRowObject in _sourceRowObjects)
        {
            var sourceRow = sourceRowObject.GetComponent<SourceRow>();
            var dropdown = sourceRowObject.transform.Find("Coord_dropdown").gameObject.GetComponent<TMP_Dropdown>();
            if (featureMapping.Mapping.ImportedColumns.Contains(sourceRow.SourceName))
                sourceRowObject.transform.Find("Import_toggle").gameObject.GetComponent<Toggle>().isOn = true;
            if (sourceRow.SourceName == featureMapping.Mapping.ID.Source)
            {
                sourceRow.CurrentMapping = SourceMappingOptions.ID;
                dropdown.value = (int)SourceMappingOptions.ID;
            }
            else if (sourceRow.SourceName == featureMapping.Mapping.X.Source)
            {
                sourceRow.CurrentMapping = SourceMappingOptions.X;
                dropdown.value = (int)SourceMappingOptions.X;
            }
            else if (sourceRow.SourceName == featureMapping.Mapping.Y.Source)
            {
                sourceRow.CurrentMapping = SourceMappingOptions.Y;
                dropdown.value = (int)SourceMappingOptions.Y;
            }
            else if (sourceRow.SourceName == featureMapping.Mapping.Z.Source)
            {
                sourceRow.CurrentMapping = SourceMappingOptions.Z;
                dropdown.value = (int)SourceMappingOptions.Z;
            }
            else if (sourceRow.SourceName == featureMapping.Mapping.XMin.Source)
            {
                sourceRow.CurrentMapping = SourceMappingOptions.Xmin;
                dropdown.value = (int)SourceMappingOptions.Xmin;
            }
            else if (sourceRow.SourceName == featureMapping.Mapping.XMax.Source)
            {
                sourceRow.CurrentMapping = SourceMappingOptions.Xmax;
                dropdown.value = (int)SourceMappingOptions.Xmax;
            }
            else if (sourceRow.SourceName == featureMapping.Mapping.YMin.Source)
            {
                sourceRow.CurrentMapping = SourceMappingOptions.Ymin;
                dropdown.value = (int)SourceMappingOptions.Ymin;
            }
            else if (sourceRow.SourceName == featureMapping.Mapping.YMax.Source)
            {
                sourceRow.CurrentMapping = SourceMappingOptions.Ymax;
                dropdown.value = (int)SourceMappingOptions.Ymax;
            }
            else if (sourceRow.SourceName == featureMapping.Mapping.ZMin.Source)
            {
                sourceRow.CurrentMapping = SourceMappingOptions.Zmin;
                dropdown.value = (int)SourceMappingOptions.Zmin;
            }
            else if (sourceRow.SourceName == featureMapping.Mapping.ZMax.Source)
            {
                sourceRow.CurrentMapping = SourceMappingOptions.Zmax;
                dropdown.value = (int)SourceMappingOptions.Zmax;
            }
            else if (sourceRow.SourceName == featureMapping.Mapping.RA.Source)
            {
                sourceRow.CurrentMapping = SourceMappingOptions.Ra;
                dropdown.value = (int)SourceMappingOptions.Ra;
            }
            else if (sourceRow.SourceName == featureMapping.Mapping.Dec.Source)
            {
                sourceRow.CurrentMapping = SourceMappingOptions.Dec;
                dropdown.value = (int)SourceMappingOptions.Dec;
            }
            else if (sourceRow.SourceName == featureMapping.Mapping.Vel.Source)
            {
                sourceRow.CurrentMapping = SourceMappingOptions.Velo;
                dropdown.value = (int)SourceMappingOptions.Velo;
            }
            else if (sourceRow.SourceName == featureMapping.Mapping.Freq.Source)
            {
                sourceRow.CurrentMapping = SourceMappingOptions.Freq;
                dropdown.value = (int)SourceMappingOptions.Freq;
            }
            else if (sourceRow.SourceName == featureMapping.Mapping.Redshift.Source)
            {
                sourceRow.CurrentMapping = SourceMappingOptions.Redshift;
                dropdown.value = (int)SourceMappingOptions.Redshift;
            }
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
        var loadingText = sourcesPanelContent.gameObject.transform.Find("SourcesLoad_container").gameObject.transform.Find("Text").gameObject;
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

        var featureSetManager = GetFirstActiveDataSet().GetComponentInChildren<FeatureSetManager>();
        Dictionary<SourceMappingOptions, string> finalMapping = new Dictionary<SourceMappingOptions, string>();
        for (int i = 0; i < _sourceRowObjects.Length; i++)
        {
            var row = _sourceRowObjects[i].GetComponent<SourceRow>();
            if (row.CurrentMapping != SourceMappingOptions.none)
                finalMapping.Add(row.CurrentMapping, row.SourceName);
            columnsMask[i] = _sourceRowObjects[i].transform.Find("Import_toggle").gameObject.GetComponent<Toggle>().isOn;
        }

        if (featureSetManager.FeatureFileToLoad != "")
            featureSetManager.ImportFeatureSet(finalMapping, FeatureMapper.GetVOTableFromFile(sourcesPath), Path.GetFileName(sourcesPath), columnsMask);
        loadingText.GetComponent<TextMeshProUGUI>().text = $"Successfully loaded sources from:{Environment.NewLine}{Path.GetFileName(sourcesPath)}";
        sourcesPanelContent.gameObject.transform.Find("SourcesLoad_container").gameObject.transform.Find("Button").GetComponent<Button>().interactable = false;
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

    private VolumeDataSetRenderer GetFirstActiveDataSet()
    {
        if (_volumeDataSets != null)
        {
            foreach (var dataSet in _volumeDataSets)
            {
                if (dataSet.isActiveAndEnabled)
                {
                    return dataSet;
                }
            }
        }

        return null;
    }


    void OnGUI()
    {
        if (showPopUp)
        {
            GUI.backgroundColor = new Color(1, 0, 0, 1f);
            GUI.Window(0, new Rect((Screen.width / 2) - 150, (Screen.height / 2) - 75
                , 300, 250), ShowGUI, "Invalid Cube");
        }
    }

    void ShowGUI(int windowID)
    {
// You may put a label to show a message to the player
        GUI.Label(new Rect(65, 40, 300, 250), textPopUp);
// You may put a button to close the pop up too
        if (GUI.Button(new Rect(50, 150, 75, 30), "OK"))
        {
            showPopUp = false;
            textPopUp = "";
            // you may put other code to run according to your game too
        }
    }

    private void populateStatsValue()
    {
        VolumeDataSet volumeDataSet = GetFirstActiveDataSet().Data;

        Transform stats = statsPanelContent.gameObject.transform.Find("Stats_container").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject
            .transform.Find("Stats");
        stats.gameObject.transform.Find("Line_min").gameObject.transform.Find("InputField_min").GetComponent<TMP_InputField>().text = volumeDataSet.MinValue.ToString();
        stats.gameObject.transform.Find("Line_max").gameObject.transform.Find("InputField_max").GetComponent<TMP_InputField>().text = volumeDataSet.MaxValue.ToString();
        stats.gameObject.transform.Find("Line_std").gameObject.transform.Find("Text_std").GetComponent<TextMeshProUGUI>().text = volumeDataSet.StanDev.ToString();
        stats.gameObject.transform.Find("Line_mean").gameObject.transform.Find("Text_mean").GetComponent<TextMeshProUGUI>().text = volumeDataSet.MeanValue.ToString();
        histogramHelper.CreateHistogramImg(volumeDataSet.Histogram, volumeDataSet.HistogramBinWidth, volumeDataSet.MinValue, volumeDataSet.MaxValue, volumeDataSet.MeanValue,
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
        var activeDataSet = GetFirstActiveDataSet();
        if (activeDataSet != null)
        {
            activeColorMap = ColorMapUtils.FromHashCode(renderingPanelContent.gameObject.transform.Find("Rendering_container").gameObject.transform.Find("Viewport").gameObject
                .transform.Find("Content").gameObject.transform.Find("Settings").gameObject.transform.Find("Colormap_container").gameObject.transform.Find("Dropdown_colormap")
                .GetComponent<TMP_Dropdown>().value);
            activeDataSet.ColorMap = activeColorMap;
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
        VolumeDataSet volumeDataSet = GetFirstActiveDataSet().Data;
        histogramHelper.CreateHistogramImg(volumeDataSet.Histogram, volumeDataSet.HistogramBinWidth, histMin, histMax, volumeDataSet.MeanValue, volumeDataSet.StanDev, sigma);
    }

    public void RestoreDefaults()
    {
        statsPanelContent.gameObject.transform.Find("Stats_container").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject.transform.Find("Stats")
            .gameObject.transform.Find("Line_sigma").gameObject.transform.Find("Dropdown").GetComponent<TMP_Dropdown>().value = 0;

        VolumeDataSet.UpdateHistogram(GetFirstActiveDataSet().Data, GetFirstActiveDataSet().Data.MinValue, GetFirstActiveDataSet().Data.MaxValue);
        populateStatsValue();
    }

    public void UpdateScaleMin(String min)
    {
        VolumeDataSetRenderer volumeDataSetRenderer = GetFirstActiveDataSet();
        VolumeDataSet volumeDataSet = volumeDataSetRenderer.Data;
        float newMin = float.Parse(min);
        float histMin = newMin;
        float histMax = float.Parse(statsPanelContent.gameObject.transform.Find("Stats_container").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content")
            .gameObject.transform.Find("Stats").gameObject.transform.Find("Line_max")
            .gameObject.transform.Find("InputField_max").GetComponent<TMP_InputField>().text);
        float sigma = statsPanelContent.gameObject.transform.Find("Stats_container").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject.transform
            .Find("Stats").gameObject.transform.Find("Line_sigma")
            .gameObject.transform.Find("Dropdown").GetComponent<TMP_Dropdown>().value + 1f;
        volumeDataSetRenderer.ScaleMin = newMin;
        VolumeDataSet.UpdateHistogram(volumeDataSet, histMin, histMax);
        histogramHelper.CreateHistogramImg(volumeDataSet.Histogram, volumeDataSet.HistogramBinWidth, histMin, histMax, volumeDataSet.MeanValue, volumeDataSet.StanDev, sigma);
    }

    public void UpdateScaleMax(String max)
    {
        VolumeDataSetRenderer volumeDataSetRenderer = GetFirstActiveDataSet();
        VolumeDataSet volumeDataSet = volumeDataSetRenderer.Data;
        float newMax = float.Parse(max);
        float histMin = float.Parse(statsPanelContent.gameObject.transform.Find("Stats_container").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content")
            .gameObject.transform.Find("Stats").gameObject.transform.Find("Line_min")
            .gameObject.transform.Find("InputField_min").GetComponent<TMP_InputField>().text);
        float histMax = newMax;
        float sigma = statsPanelContent.gameObject.transform.Find("Stats_container").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject.transform
            .Find("Stats").gameObject.transform.Find("Line_sigma")
            .gameObject.transform.Find("Dropdown").GetComponent<TMP_Dropdown>().value + 1f;
        volumeDataSetRenderer.ScaleMax = newMax;
        VolumeDataSet.UpdateHistogram(volumeDataSet, histMin, histMax);
        histogramHelper.CreateHistogramImg(volumeDataSet.Histogram, volumeDataSet.HistogramBinWidth, histMin, histMax, volumeDataSet.MeanValue, volumeDataSet.StanDev, sigma);
    }

    public void UpdateThresholdMin(float value)
    {
        var activeDataSet = GetFirstActiveDataSet();
        if (activeDataSet != null)
        {
            activeDataSet.ThresholdMin = Mathf.Clamp(value, 0, activeDataSet.ThresholdMax);
        }
    }

    public void UpdateThresholdMax(float value)
    {
        var activeDataSet = GetFirstActiveDataSet();
        if (activeDataSet != null)
        {
            activeDataSet.ThresholdMax = Mathf.Clamp(value, activeDataSet.ThresholdMin, 1);
        }
    }

    public void ResetThresholds()
    {
        var activeDataSet = GetFirstActiveDataSet();
        if (activeDataSet != null)
        {
            activeDataSet.ThresholdMin = activeDataSet.InitialThresholdMin;
            minThreshold.value = activeDataSet.ThresholdMin;

            activeDataSet.ThresholdMax = activeDataSet.InitialThresholdMax;
            maxThreshold.value = activeDataSet.ThresholdMax;
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