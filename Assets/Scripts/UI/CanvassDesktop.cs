using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
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
using SFB;

public class CanvassDesktop : MonoBehaviour
{
    private VolumeDataSetRenderer[] _volumeDataSetRenderers;
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
    public TextMeshProUGUI loadTextLabel;

    public GameObject progressBar;

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

    Dictionary<double, double> axisSize = null;
    Dictionary<double, double> maskAxisSize = null;

    private int ratioDropdownIndex = 0;

    private ColorMapEnum activeColorMap = ColorMapEnum.None;

    private Slider minThreshold;
    private TextMeshProUGUI minThresholdLabel;

    private Slider maxThreshold;
    private TextMeshProUGUI maxThresholdLabel;
    
    
    private FeatureMapping featureMapping;


    protected Coroutine loadCubeCoroutine;
    protected Coroutine showLoadDialogCoroutine;


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

    void checkCubesDataSet()
    {
        volumeDataSetManager = GameObject.Find("VolumeDataSetManager");
        if (volumeDataSetManager)
        {
            _volumeDataSetRenderers = volumeDataSetManager.GetComponentsInChildren<VolumeDataSetRenderer>(true);
        }
        else
        {
            _volumeDataSetRenderers = new VolumeDataSetRenderer[0];
        }
    }

    // Update is called once per frame
    void Update()
    {
        var firstActiveRenderer = GetFirstActiveRenderer();
        if (firstActiveRenderer != null)
        {
            if (minThreshold.value > maxThreshold.value)
            {
                minThreshold.value = maxThreshold.value;
            }

            var effectiveMin = firstActiveRenderer.ScaleMin + firstActiveRenderer.ThresholdMin 
                * (firstActiveRenderer.ScaleMax - firstActiveRenderer.ScaleMin);
            var effectiveMax = firstActiveRenderer.ScaleMin + firstActiveRenderer.ThresholdMax 
                * (firstActiveRenderer.ScaleMax - firstActiveRenderer.ScaleMin);
            minThresholdLabel.text = effectiveMin.ToString();
            maxThresholdLabel.text = effectiveMax.ToString();

            if (firstActiveRenderer.ThresholdMin != minThreshold.value)
            {
                minThreshold.value = firstActiveRenderer.ThresholdMin;
            }

            if (firstActiveRenderer.ThresholdMax != maxThreshold.value)
            {
                maxThreshold.value = firstActiveRenderer.ThresholdMax;
            }


            if (firstActiveRenderer.ColorMap != activeColorMap)
            {
                renderingPanelContent.gameObject.transform.Find("Rendering_container").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content")
                    .gameObject.transform.Find("Settings").gameObject.transform.Find("Colormap_container")
                    .gameObject.transform.Find("Dropdown_colormap").GetComponent<TMP_Dropdown>().value = (int)firstActiveRenderer.ColorMap;
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

            //if it is valid enable loading button
            if (loadable)
            {
                informationPanelContent.gameObject.transform.Find("MaskFile_container").gameObject.transform.Find("Button").GetComponent<Button>().interactable = true;
                informationPanelContent.gameObject.transform.Find("Loading_container").gameObject.transform.Find("Button").GetComponent<Button>().interactable = true;
            }
        }

        if (showLoadDialogCoroutine != null)
            StopCoroutine(showLoadDialogCoroutine);
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


    public IEnumerator LoadCubeCoroutine(string _imagePath, string _maskPath)
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

        newCube.transform.SetParent(volumeDataSetManager.transform, false);

        newCube.GetComponent<VolumeDataSetRenderer>().FileName = _imagePath; //_dataSet.FileName.ToString();
        newCube.GetComponent<VolumeDataSetRenderer>().MaskFileName = _maskPath; // _maskDataSet.FileName.ToString();
        newCube.GetComponent<VolumeDataSetRenderer>().loadText = this.loadTextLabel;
        newCube.GetComponent<VolumeDataSetRenderer>().progressBar = this.progressBar.GetComponent<Slider>();
        newCube.GetComponent<VolumeDataSetRenderer>().CubeDepthAxis = int.Parse(informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform
            .Find("Z_Dropdown").GetComponent<TMP_Dropdown>()
            .options[informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("Z_Dropdown").GetComponent<TMP_Dropdown>().value].text) - 1;
        newCube.GetComponent<VolumeDataSetRenderer>().FileChanged = false;
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
        StartCoroutine(newCube.GetComponent<VolumeDataSetRenderer>()._startFunc());

        while (!newCube.GetComponent<VolumeDataSetRenderer>().started)
        {
            yield return new WaitForSeconds(.1f);
        }

        loadTextLabel.text = "Loading complete!";
        Debug.Log("Loading image " + _imagePath + " and mask " + _maskPath + " complete!");
        progressBar.GetComponent<Slider>().value = 6;
        yield return new WaitForSeconds(0.001f);
        postLoadFileFileSystem();
    }

    public void OnRatioDropdownValueChanged(int optionIndex)
    {
        ratioDropdownIndex = optionIndex;
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
        sourcesPath = path;
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
        featureMapping = FeatureMapping.GetMappingFromFile(path);
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
                else if (sourceRow.SourceName == featureMapping.Mapping.Flag.Source)
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
            featureSetManager.ImportFeatureSetFromTable(finalMapping, FeatureTable.GetFeatureTableFromFile(sourcesPath), Path.GetFileName(sourcesPath), columnsMask, excludeExternalSources);
        }
        loadingText.GetComponent<TextMeshProUGUI>().text = $"Successfully loaded sources from:{Environment.NewLine}{Path.GetFileName(sourcesPath)}";
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
        var volumeDataSet = GetFirstActiveRenderer().Data;

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
        var firstActiveRenderer = GetFirstActiveRenderer();
        if (firstActiveRenderer != null)
        {
            activeColorMap = ColorMapUtils.FromHashCode(renderingPanelContent.gameObject.transform.Find("Rendering_container").gameObject.transform.Find("Viewport").gameObject
                .transform.Find("Content").gameObject.transform.Find("Settings").gameObject.transform.Find("Colormap_container").gameObject.transform.Find("Dropdown_colormap")
                .GetComponent<TMP_Dropdown>().value);
            firstActiveRenderer.ColorMap = activeColorMap;
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
        histogramHelper.CreateHistogramImg(volumeDataSet.Histogram, volumeDataSet.HistogramBinWidth, histMin, histMax, volumeDataSet.MeanValue, volumeDataSet.StanDev, sigma);
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
        histogramHelper.CreateHistogramImg(volumeDataSet.Histogram, volumeDataSet.HistogramBinWidth, min, max, volumeDataSet.MeanValue, volumeDataSet.StanDev, sigma);
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
            minThreshold.value = firstActiveRenderer.ThresholdMin;

            firstActiveRenderer.ThresholdMax = firstActiveRenderer.InitialThresholdMax;
            maxThreshold.value = firstActiveRenderer.ThresholdMax;
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