
using SimpleFileBrowser;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VolumeData;

public class CanvassDesktop : MonoBehaviour
{

    private VolumeDataSetRenderer[] _volumeDataSets;
    private GameObject volumeDataSetManager;

    public GameObject cubeprefab;
    public GameObject informationPanelContent;
    public GameObject mainCanvassDesktop;
    public GameObject fileLoadCanvassDesktop;


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

   // List<Tuple<double, double>> axisSize = null;
    Dictionary<double, double> axisSize = null;
    Dictionary<double, double> maskAxisSize = null;
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
        // InformationTab();


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
        StartCoroutine(ShowLoadDialogCoroutine(0));


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

            //axisSize = new List<Tuple<double, double>>();

            axisSize = new Dictionary<double, double>();

            List<double> list = new List<double>();

            //set the path of selected file to the ui
            informationPanelContent.gameObject.transform.Find("ImageFile_container").gameObject.transform.Find("ImageFilePath_text").GetComponent<TextMeshProUGUI>().text = System.IO.Path.GetFileName(imagePath);

            //visualize the header into the scroll view
            string _header = "";
            IDictionary<string, string> _headerDictionary = FitsReader.ExtractHeaders(fptr, out status);
            
             foreach (KeyValuePair<string, string> entry in _headerDictionary)
            {

                //switch (entry.Key)
                if (entry.Key.Length>4)
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
    }

    public void BrowseMaskFile()
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Fits File", ".fits", ".fit"));
        FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe", ".sys");

        // Set default filter that is selected when the dialog is shown (optional)
        // Returns true if the default filter is set successfully
        // In this case, set Images filter as the default filter
        FileBrowser.SetDefaultFilter(".fits");
        StartCoroutine(ShowLoadDialogCoroutine(1));
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
            foreach (KeyValuePair<string, string> entry in _headerDictionary)
            {

                //switch (entry.Key)
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

                if(axisSize[i0 + 1] == maskAxisSize[1] && axisSize[i1 + 1] == maskAxisSize[2] && axisSize[i2 + 1] == maskAxisSize[3])
                {
                    loadable = true;
                    informationPanelContent.gameObject.transform.Find("Loading_container").gameObject.transform.Find("Button").GetComponent<Button>().interactable = true;
                }
                else
                    loadable = false;

            }

            if(!loadable)
            {
                //mask is not valid
                informationPanelContent.gameObject.transform.Find("MaskFile_container").gameObject.transform.Find("MaskFilePath_text").GetComponent<TextMeshProUGUI>().text = "...";
                maskPath = "";
                showPopUp = true;
                textPopUp = "Selected Mask\ndoesn't match image file";
            }
        }
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
        // Show a load file dialog and wait for a response from user
        // Load file/folder: file, Initial path: default (Documents), Title: "Load File", submit button text: "Load"
        yield return FileBrowser.WaitForLoadDialog(false, null, "Load File", "Load");

        // Dialog is closed
        // Print whether a file is chosen (FileBrowser.Success)
        // and the path to the selected file (FileBrowser.Result) (null, if FileBrowser.Success is false)

        if (FileBrowser.Success)
        {
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

        //here should check if loading is ok
        if(true)
        {
            //move to rendering tab
           // mainCanvassDesktop.gameObject.transform.Find("RightPanel").gameObject.transform.Find("Tabs_ container").gameObject.transform.Find("Rendering_Button").GetComponent<Button>().interactable = true;
           // mainCanvassDesktop.gameObject.transform.Find("RightPanel").gameObject.transform.Find("Tabs_ container").gameObject.transform.Find("Rendering_Button").GetComponent<Button>().onClick.Invoke();
      



        }

       
    }

  

    IEnumerator LoadCubeCoroutine(string _imagePath, string _maskPath)
    {

        int i0 = informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("X_Dropdown").GetComponent<TMP_Dropdown>().value;
        int i1 = informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("Y_Dropdown").GetComponent<TMP_Dropdown>().value;
        int i2 = informationPanelContent.gameObject.transform.Find("Axes_container").gameObject.transform.Find("Z_Dropdown").GetComponent<TMP_Dropdown>().value;


        Vector3 oldpos = new Vector3(0, 0f, 0);
        Quaternion oldrot = Quaternion.identity;
        Vector3 oldscale = new Vector3(1, 1, 1);
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
        newCube.transform.parent = volumeDataSetManager.transform;
        newCube.transform.localPosition = oldpos;
        newCube.transform.localRotation = oldrot;
        newCube.transform.localScale = oldscale;
        newCube.GetComponent<VolumeDataSetRenderer>().FileName = _imagePath;//_dataSet.FileName.ToString();
        newCube.GetComponent<VolumeDataSetRenderer>().MaskFileName = _maskPath;// _maskDataSet.FileName.ToString();


        newCube.SetActive(true);
        checkCubesDataSet();

        //Deactivate and reactivate VolumeInputController to update VolumeInputController's list of datasets

        _volumeInputController.gameObject.SetActive(false);
        _volumeInputController.gameObject.SetActive(true);

        _volumeSpeechController.AddDataSet(newCube.GetComponent<VolumeDataSetRenderer>());

        yield return null;


        
    }

    public void DismissFileLoad()
    {
        fileLoadCanvassDesktop.SetActive(false);
        mainCanvassDesktop.SetActive(true);
    }

    public void Exit()
    {
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


}
