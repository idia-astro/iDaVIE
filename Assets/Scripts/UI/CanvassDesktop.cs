﻿
using SimpleFileBrowser;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VolumeData;

using OxyPlot;

using System.IO;
using OxyPlot.Series;


using OxyPlot.WindowsForms;
using OxyPlot.Annotations;

using Valve;
using Valve.VR;


public class CanvassDesktop : MonoBehaviour
{


    private VolumeDataSetRenderer[] _volumeDataSets;
    private GameObject volumeDataSetManager;

    public GameObject cubeprefab;
    public GameObject informationPanelContent;
    public GameObject mainCanvassDesktop;
    public GameObject fileLoadCanvassDesktop;
    public GameObject VolumePlayer;

    public GameObject WelcomeMenu;
    public GameObject LoadingText;


    private bool showPopUp = false;
    private string textPopUp = "";
    private VolumeInputController _volumeInputController;
    private VolumeSpeechController _volumeSpeechController;
    string imagePath = "";
    string maskPath = "";

    private double imageNAxis = 0;
    private double imageSize = 1;
    private double maskNAxis = 0;
    private double maskSize = 1;

    Dictionary<double, double> axisSize = null;
    Dictionary<double, double> maskAxisSize = null;

    private int ratioDropdownIndex = 0;

    private ColorMapEnum activeColorMap = ColorMapEnum.None;


    private PlotModel model = null;
    private float histMin = 0;
    private float histMax = 1;
    private float sigma = 1;

    protected Coroutine loadCubeCoroutine;
    protected Coroutine showLoadDialogCoroutine;

    // Start is called before the first frame update
    void Start()
    {
        _volumeInputController = FindObjectOfType<VolumeInputController>();
        _volumeSpeechController = FindObjectOfType<VolumeSpeechController>();
        checkCubesDataSet();
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
        if (getFirstActiveDataSet() && (getFirstActiveDataSet().ColorMap != activeColorMap))
        {
            mainCanvassDesktop.gameObject.transform.Find("RightPanel").gameObject.transform.Find("Panel_container").gameObject.transform.Find("RenderingPanel").gameObject.transform.Find("Colormap_container")
      .gameObject.transform.Find("Line_6").gameObject.transform.Find("Dropdown_colormap").GetComponent<TMP_Dropdown>().value = (int)getFirstActiveDataSet().ColorMap;
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
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Fits File", ".fits", ".fit"));

        // Set default filter that is selected when the dialog is shown (optional)
        // Returns true if the default filter is set successfully
        // In this case, set Images filter as the default filter
        FileBrowser.SetDefaultFilter(".fits");
        showLoadDialogCoroutine = StartCoroutine(ShowLoadDialogCoroutine(0));
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
            informationPanelContent.gameObject.transform.Find("ImageFile_container").gameObject.transform.Find("ImageFilePath_text").GetComponent<TextMeshProUGUI>().text = System.IO.Path.GetFileName(imagePath);

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
            informationPanelContent.gameObject.transform.Find("Header_container").gameObject.transform.Find("Scroll View").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject.transform.Find("Header").GetComponent<TextMeshProUGUI>().text = _header;
            informationPanelContent.gameObject.transform.Find("Header_container").gameObject.transform.Find("Scroll View").gameObject.transform.Find("Scrollbar Vertical").GetComponent<Scrollbar>().value = 1;

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
                }

                //update dropdow
                informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("X_Dropdown").GetComponent<TMP_Dropdown>().interactable = false;
                informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("Y_Dropdown").GetComponent<TMP_Dropdown>().interactable = false;
                informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("Z_Dropdown").GetComponent<TMP_Dropdown>().interactable = false;

                informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("X_Dropdown").GetComponent<TMP_Dropdown>().options.Clear();
                informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("Y_Dropdown").GetComponent<TMP_Dropdown>().options.Clear();
                informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("Z_Dropdown").GetComponent<TMP_Dropdown>().options.Clear();

                foreach (KeyValuePair<double, double> axes in axisSize)
                {
                    if (axes.Value > 1)
                    {
                        informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("X_Dropdown").GetComponent<TMP_Dropdown>().options.Add((new TMP_Dropdown.OptionData() { text = axes.Key.ToString() }));
                        informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("Y_Dropdown").GetComponent<TMP_Dropdown>().options.Add((new TMP_Dropdown.OptionData() { text = axes.Key.ToString() }));
                        informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("Z_Dropdown").GetComponent<TMP_Dropdown>().options.Add((new TMP_Dropdown.OptionData() { text = axes.Key.ToString() }));
                    }
                }

                informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("X_Dropdown").GetComponent<TMP_Dropdown>().RefreshShownValue();
                informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("X_Dropdown").GetComponent<TMP_Dropdown>().RefreshShownValue();
                informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("X_Dropdown").GetComponent<TMP_Dropdown>().RefreshShownValue();
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

                    informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("X_Dropdown").GetComponent<TMP_Dropdown>().interactable = true;
                    informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("Y_Dropdown").GetComponent<TMP_Dropdown>().interactable = true;
                    informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("Z_Dropdown").GetComponent<TMP_Dropdown>().interactable = true;

                    loadable = true;

                }

            }
            else { loadable = false; localMsg = "Please select a valid cube!"; }
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
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Fits File", ".fits", ".fit"));
        FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe", ".sys");

        // Set default filter that is selected when the dialog is shown (optional)
        // Returns true if the default filter is set successfully
        // In this case, set Images filter as the default filter
        FileBrowser.SetDefaultFilter(".fits");
        showLoadDialogCoroutine = StartCoroutine(ShowLoadDialogCoroutine(1));
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

            informationPanelContent.gameObject.transform.Find("MaskFile_container").gameObject.transform.Find("MaskFilePath_text").GetComponent<TextMeshProUGUI>().text = System.IO.Path.GetFileName(maskPath);

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
                int i0 = informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("X_Dropdown").GetComponent<TMP_Dropdown>().value;
                int i1 = informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("Y_Dropdown").GetComponent<TMP_Dropdown>().value;
                int i2 = informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("Z_Dropdown").GetComponent<TMP_Dropdown>().value;

                if (axisSize[i0 + 1] == maskAxisSize[1] && axisSize[i1 + 1] == maskAxisSize[2] && axisSize[i2 + 1] == maskAxisSize[3])
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
            int i0 = informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("X_Dropdown").GetComponent<TMP_Dropdown>().value;
            int i1 = informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("Y_Dropdown").GetComponent<TMP_Dropdown>().value;
            int i2 = informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("Z_Dropdown").GetComponent<TMP_Dropdown>().value;

            if (axisSize[i0 + 1] != maskAxisSize[1] || axisSize[i1 + 1] != maskAxisSize[2] || axisSize[i2 + 1] != maskAxisSize[3])
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


    IEnumerator ShowLoadDialogCoroutine(int type)
    {
        string lastPath = PlayerPrefs.GetString("LastPath");
        if (!FileBrowserHelpers.DirectoryExists(lastPath))
            lastPath = null;

        // Show a load file dialog and wait for a response from user
        // Load file/folder: file, Initial path: last path or default (Documents), Title: "Load File", submit button text: "Load"
        yield return FileBrowser.WaitForLoadDialog(false, lastPath, "Load File", "Load");

        // Dialog is closed
        // Print whether a file is chosen (FileBrowser.Success)
        // and the path to the selected file (FileBrowser.Result) (null, if FileBrowser.Success is false)

        if (FileBrowser.Success)
        {
            PlayerPrefs.SetString("LastPath", Path.GetDirectoryName(FileBrowser.Result));
            PlayerPrefs.Save();

            // If a file was chosen, read its bytes via FileBrowserHelpers
            // Contrary to File.ReadAllBytes, this function works on Android 10+, as well
            switch (type)
            {
                case 0:
                    _browseImageFile(FileBrowser.Result);
                    break;
                case 1:
                    _browseMaskFile(FileBrowser.Result);
                    break;
            }
            //byte[] bytes = FileBrowserHelpers.ReadBytesFromFile(FileBrowser.Result);
        }

        yield return null;
    }

    public void LoadFileFromFileSystem()
    {

        StartCoroutine(LoadCubeCoroutine(imagePath, maskPath));
    }

    private void postLoadFileFileSystem()
    {

        if (true)
        {


            if (loadCubeCoroutine != null)
                StopCoroutine(loadCubeCoroutine);
            VolumePlayer.SetActive(false);
            VolumePlayer.SetActive(true);

            if (getFirstActiveDataSet().MaskFileName != "")
            {
                mainCanvassDesktop.gameObject.transform.Find("RightPanel").gameObject.transform.Find("Panel_container").gameObject.transform.Find("RenderingPanel").gameObject.transform.Find("Colormap_container")
            .gameObject.transform.Find("Line_7").gameObject.transform.Find("Dropdown_mask").GetComponent<TMP_Dropdown>().interactable = true;

            }
            else
                mainCanvassDesktop.gameObject.transform.Find("RightPanel").gameObject.transform.Find("Panel_container").gameObject.transform.Find("RenderingPanel").gameObject.transform.Find("Colormap_container")
            .gameObject.transform.Find("Line_7").gameObject.transform.Find("Dropdown_mask").GetComponent<TMP_Dropdown>().interactable = false;


            populateColorMapDropdown();
            populateStatsValue();


            mainCanvassDesktop.gameObject.transform.Find("RightPanel").gameObject.transform.Find("Tabs_ container").gameObject.transform.Find("Rendering_Button").GetComponent<Button>().interactable = true;
            mainCanvassDesktop.gameObject.transform.Find("RightPanel").gameObject.transform.Find("Tabs_ container").gameObject.transform.Find("Rendering_Button").GetComponent<Button>().onClick.Invoke();

            LoadingText.gameObject.SetActive(false);
            WelcomeMenu.gameObject.SetActive(false);
        }

    }



    public IEnumerator LoadCubeCoroutine(string _imagePath, string _maskPath)
    {
        LoadingText.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.001f);

        float zScale = 1f;
        if (ratioDropdownIndex == 1)
        {
            // case X=Y, calculate z scale from NAXIS1 and NAXIS3
            int i0 = informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("X_Dropdown").GetComponent<TMP_Dropdown>().value;
            int i2 = informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("Z_Dropdown").GetComponent<TMP_Dropdown>().value;

            double x, z;
            if (axisSize.TryGetValue(i0 + 1, out x) && axisSize.TryGetValue(i2 + 1, out z))
            {
                zScale = (float)(z / x);
            }
        }

        Vector3 oldpos = new Vector3(0, 0f, 0);
        Quaternion oldrot = Quaternion.identity;
        Vector3 oldscale = new Vector3(1, 1, zScale);

        if (getFirstActiveDataSet() != null)
        {
            getFirstActiveDataSet()._voxelOutline.active = false;
            getFirstActiveDataSet()._regionOutline.active = false;
            getFirstActiveDataSet()._cubeOutline.active = false;

            oldpos = getFirstActiveDataSet().transform.localPosition;
            oldrot = getFirstActiveDataSet().transform.localRotation;
            oldscale = getFirstActiveDataSet().transform.localScale;
            getFirstActiveDataSet().transform.gameObject.SetActive(false);

        }

        GameObject newCube = Instantiate(cubeprefab, new Vector3(0, 0f, 0), Quaternion.identity);
        newCube.SetActive(true);

        newCube.transform.parent = volumeDataSetManager.transform;
        newCube.transform.localPosition = oldpos;
        newCube.transform.localRotation = oldrot;
        newCube.transform.localScale = oldscale;

        newCube.GetComponent<VolumeDataSetRenderer>().FileName = _imagePath;//_dataSet.FileName.ToString();
        newCube.GetComponent<VolumeDataSetRenderer>().MaskFileName = _maskPath;// _maskDataSet.FileName.ToString();

        checkCubesDataSet();

        //Deactivate and reactivate VolumeInputController to update VolumeInputController's list of datasets
        _volumeInputController.gameObject.SetActive(false);
        _volumeInputController.gameObject.SetActive(true);

        _volumeSpeechController.AddDataSet(newCube.GetComponent<VolumeDataSetRenderer>());

        while (!newCube.GetComponent<VolumeDataSetRenderer>().started)
        {
            yield return new WaitForSeconds(.1f);
        }
        postLoadFileFileSystem();
    }

    public void OnRatioDropdownValueChanged(int optionIndex)
    {
        ratioDropdownIndex = optionIndex;
        if (getFirstActiveDataSet() != null)
        {
            if (optionIndex == 0)
            {
                // X=Y=Z
                getFirstActiveDataSet().ZScale = 1f;
            }
            else
            {
                // X=Y
                getFirstActiveDataSet().ZScale = 1f * getFirstActiveDataSet().GetCubeDimensions().z / getFirstActiveDataSet().GetCubeDimensions().x;
            }
        }
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


    private VolumeDataSetRenderer getFirstActiveDataSet()
    {
        foreach (var dataSet in _volumeDataSets)
        {
            if (dataSet.isActiveAndEnabled)
            {
                return dataSet;
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
        VolumeDataSet volumeDataSet = getFirstActiveDataSet().GetDatsSet();

        mainCanvassDesktop.gameObject.transform.Find("RightPanel").gameObject.transform.Find("Panel_container").gameObject.transform.Find("RenderingPanel").gameObject.transform.Find("Colormap_container")
          .gameObject.transform.Find("Line_1").gameObject.transform.Find("InputField_min").GetComponent<TMP_InputField>().text = volumeDataSet.MinValue.ToString();

        mainCanvassDesktop.gameObject.transform.Find("RightPanel").gameObject.transform.Find("Panel_container").gameObject.transform.Find("RenderingPanel").gameObject.transform.Find("Colormap_container")
          .gameObject.transform.Find("Line_1").gameObject.transform.Find("InputField_max").GetComponent<TMP_InputField>().text = volumeDataSet.MaxValue.ToString();

        mainCanvassDesktop.gameObject.transform.Find("RightPanel").gameObject.transform.Find("Panel_container").gameObject.transform.Find("RenderingPanel").gameObject.transform.Find("Colormap_container")
          .gameObject.transform.Find("Line_2").gameObject.transform.Find("Text_std").GetComponent<TextMeshProUGUI>().text = volumeDataSet.StanDev.ToString();

        mainCanvassDesktop.gameObject.transform.Find("RightPanel").gameObject.transform.Find("Panel_container").gameObject.transform.Find("RenderingPanel").gameObject.transform.Find("Colormap_container")
          .gameObject.transform.Find("Line_2").gameObject.transform.Find("Text_mean").GetComponent<TextMeshProUGUI>().text = volumeDataSet.MeanValue.ToString();

        // Default values
        sigma = 1;
        histMin = volumeDataSet.MinValue;
        histMax = volumeDataSet.MaxValue;

        createHistogramImg(volumeDataSet.Histogram, volumeDataSet.HistogramBinWidth, volumeDataSet.MinValue, volumeDataSet.MaxValue, volumeDataSet.MeanValue, volumeDataSet.StanDev);
    }
    private void populateColorMapDropdown()
    {
        //LabelColormap.gameObject.GetComponent<Text>().text = ColorMapUtils.FromHashCode(colorIndex) + "";

        mainCanvassDesktop.gameObject.transform.Find("RightPanel").gameObject.transform.Find("Panel_container").gameObject.transform.Find("RenderingPanel").gameObject.transform.Find("Colormap_container")
            .gameObject.transform.Find("Line_6").gameObject.transform.Find("Dropdown_colormap").GetComponent<TMP_Dropdown>().options.Clear();

        foreach (var colorMap in Enum.GetValues(typeof(ColorMapEnum)))
        {
            //ColorMapUtils.FromHashCode(colorIndex) + ""
            mainCanvassDesktop.gameObject.transform.Find("RightPanel").gameObject.transform.Find("Panel_container").gameObject.transform.Find("RenderingPanel").gameObject.transform.Find("Colormap_container")
                .gameObject.transform.Find("Line_6").gameObject.transform.Find("Dropdown_colormap").GetComponent<TMP_Dropdown>().options.Add((new TMP_Dropdown.OptionData() { text = colorMap.ToString() }));
        }

        mainCanvassDesktop.gameObject.transform.Find("RightPanel").gameObject.transform.Find("Panel_container").gameObject.transform.Find("RenderingPanel").gameObject.transform.Find("Colormap_container")
      .gameObject.transform.Find("Line_6").gameObject.transform.Find("Dropdown_colormap").GetComponent<TMP_Dropdown>().value = 33;

    }

    public void ChangeColorMap()
    {
        if (getFirstActiveDataSet())
        {
            activeColorMap = ColorMapUtils.FromHashCode(mainCanvassDesktop.gameObject.transform.Find("RightPanel").gameObject.transform.Find("Panel_container").gameObject.transform.Find("RenderingPanel").gameObject.transform.Find("Colormap_container")
     .gameObject.transform.Find("Line_6").gameObject.transform.Find("Dropdown_colormap").GetComponent<TMP_Dropdown>().value);
            getFirstActiveDataSet().ColorMap = activeColorMap;
        }
    }

    public void UpdateSigma(Int32 optionIndex)
    {
        sigma = optionIndex + 1f;
        VolumeDataSet volumeDataSet = getFirstActiveDataSet().GetDatsSet();
        createHistogramImg(volumeDataSet.Histogram, volumeDataSet.HistogramBinWidth, histMin, histMax, volumeDataSet.MeanValue, volumeDataSet.StanDev, sigma);
    }

    public void RestoreDefaults()
    {
        getFirstActiveDataSet().ScaleMax = getFirstActiveDataSet().GetDatsSet().MaxValue;
        getFirstActiveDataSet().ScaleMin = getFirstActiveDataSet().GetDatsSet().MinValue;

        mainCanvassDesktop.gameObject.transform.Find("RightPanel").gameObject.transform.Find("Panel_container").gameObject.transform.Find("RenderingPanel").gameObject.transform.Find("Colormap_container")
          .gameObject.transform.Find("Line_4").gameObject.transform.Find("Dropdown").GetComponent<TMP_Dropdown>().value = 0;

        VolumeDataSet.UpdateHistogram(getFirstActiveDataSet().GetDatsSet(), getFirstActiveDataSet().GetDatsSet().MinValue, getFirstActiveDataSet().GetDatsSet().MaxValue);
        populateStatsValue();
    }

    public void createHistogramImg(int[] h, float binWidth, float min, float max, float mean, float stanDev, float sigma = 1f)
    {
        // var model = new PlotModel { Title = "Histogram" };
        model = new PlotModel { Title = "Histogram " };

        var s1 = new HistogramSeries { StrokeThickness = 1 };
        var s2 = new HistogramSeries { StrokeThickness = 1, StrokeColor = OxyColors.Green };

        int c = 0;

        //for (int i = 0; i < h.Length; i++)
        for (float i = min; i <= max && c < h.Length; i += binWidth)
        {
            s1.Items.Add(new HistogramItem(i, i + binWidth, h[c], 1));

            if (Mathf.Abs(i - mean) <= (stanDev * sigma))
            {
                s2.Items.Add(new HistogramItem(i, i + binWidth, h[c], 1));
            }

            c++;
        }

        Debug.Log("c: " + c + " h:" + h.Length);

        model.Series.Add(s1);
        model.Series.Add(s2);

        var min_annotation = new LineAnnotation();
        min_annotation.Color = OxyColors.Blue;

        min_annotation.X = min;
        min_annotation.LineStyle = LineStyle.Solid;
        min_annotation.Type = LineAnnotationType.Vertical;
        model.Annotations.Add(min_annotation);


        var max_annotation = new LineAnnotation();
        max_annotation.Color = OxyColors.Red;

        max_annotation.X = max;
        max_annotation.LineStyle = LineStyle.Solid;
        max_annotation.Type = LineAnnotationType.Vertical;
        model.Annotations.Add(max_annotation);

        ShowHistogram();
    }


    private void ShowHistogram()
    {
        int width = 600;
        int height = 300;

        var stream = new MemoryStream();

        var exporter = new OxyPlot.WindowsForms.PngExporter { Width = width, Height = height };
        exporter.Export(model, stream);


        Texture2D tex = new Texture2D(width, height);
        tex.LoadImage(stream.ToArray());
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(tex.width / 2, tex.height / 2));

        mainCanvassDesktop.gameObject.transform.Find("RightPanel").gameObject.transform.Find("Panel_container").gameObject.transform.Find("RenderingPanel").gameObject.transform.Find("Histogram_container")
    .gameObject.transform.Find("GameObject").GetComponent<Image>().sprite = sprite;
    }

    public void UpdateScaleMin(String min)
    {
        VolumeDataSet volumeDataSet = getFirstActiveDataSet().GetDatsSet();
        float newMin = float.Parse(min);
        histMin = newMin;
        getFirstActiveDataSet().ScaleMin = Mathf.Clamp(newMin, volumeDataSet.MinValue, histMax);
        VolumeDataSet.UpdateHistogram(volumeDataSet, histMin, histMax);
        createHistogramImg(volumeDataSet.Histogram, volumeDataSet.HistogramBinWidth, histMin, histMax, volumeDataSet.MeanValue, volumeDataSet.StanDev, sigma);
    }

    public void UpdateScaleMax(String max)
    {
        VolumeDataSet volumeDataSet = getFirstActiveDataSet().GetDatsSet();
        float newMax = float.Parse(max);
        histMax = newMax;
        getFirstActiveDataSet().ScaleMax = Mathf.Clamp(newMax, histMin, volumeDataSet.MaxValue);
        VolumeDataSet.UpdateHistogram(volumeDataSet, histMin, histMax);
        createHistogramImg(volumeDataSet.Histogram, volumeDataSet.HistogramBinWidth, histMin, histMax, volumeDataSet.MeanValue, volumeDataSet.StanDev, sigma);
    }
}
