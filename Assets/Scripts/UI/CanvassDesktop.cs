using SFB;
using System.Collections;
using System.Collections.Generic;
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
    string []paths ;
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


        informationPanelContent.gameObject.transform.Find("Filename_Value").GetComponent<Text>().text = getFirstActiveDataSet().FileName;
        //informationPanelContent.gameObject.transform.Find("Maskname_Value").GetComponent<Text>().text = getFirstActiveDataSet().MaskFileName;

        


    }

    public void BrowseImageFile()
    {

        // Open file with filter
        var extensions = new[] {
            new ExtensionFilter("Fits Files", "fits" ),
            new ExtensionFilter("All Files", "*" ),
        };

        paths = StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, false);

        if (paths != null)
        {

            VolumeDataSet _dataSet = VolumeDataSet.LoadDataFromFitsFile(paths[0].ToString(), false);
            // ValidateCube(_dataSet);

            //set the path of selected file to the ui
            informationPanelContent.gameObject.transform.Find("ImageFile_container").gameObject.transform.Find("ImageFilePath_text").GetComponent<Text>().text = paths[0].ToString();

            //visualize the header into the scroll view
            string _header = "";
            IDictionary<string, string> _headerDictionary = _dataSet.GetHeaderDictionary();
            //fileLoadCanvassDesktop.transform.Find("LeftPanel").gameObject.transform.Find("Panel_container").gameObject.transform.Find("InformationPanel").gameObject.transform.Find("Scroll View").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject.transform.Find("Header").GetComponent<Text>().text = "ciaon2";
            foreach (KeyValuePair<string, string> entry in _headerDictionary)
            {
                _header += entry.Key + "\t\t " + entry.Value + "\n";

            }
            informationPanelContent.gameObject.transform.Find("Header_container").gameObject.transform.Find("Scroll View").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject.transform.Find("Header").GetComponent<Text>().text = _header;

            informationPanelContent.gameObject.transform.Find("Header_container").gameObject.transform.Find("Scroll View").gameObject.transform.Find("Scrollbar Vertical").GetComponent<Scrollbar>().value = 1;

        }
    }

    public void LoadFileFromFileSystem()
    {
       

        // Open file with filter
        var extensions = new[] {
            new ExtensionFilter("Fits Files", "fits" ),
            new ExtensionFilter("All Files", "*" ),
        };

        paths = StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, true);

        if (paths != null)
        {
         
            VolumeDataSet _dataSet = VolumeDataSet.LoadDataFromFitsFile(paths[0].ToString(), false);
            ValidateCube(_dataSet);
         
           
          

        }
        //volumeDataSetManager.;
    }

    public void LoadCubeWithSpecifiedAxis()
    {
        int i0=fileLoadCanvassDesktop.transform.Find("RightPanel").gameObject.transform.Find("Axis1").gameObject.transform.Find("Dropdown").GetComponent<Dropdown>().value;
        int i1=fileLoadCanvassDesktop.transform.Find("RightPanel").gameObject.transform.Find("Axis2").gameObject.transform.Find("Dropdown").GetComponent<Dropdown>().value;
        int i2=fileLoadCanvassDesktop.transform.Find("RightPanel").gameObject.transform.Find("Axis3").gameObject.transform.Find("Dropdown").GetComponent<Dropdown>().value;
       
        VolumeDataSet _dataSet = VolumeDataSet.LoadDataFromFitsFile(paths[0].ToString(), false,i0,i1,i2);
        loadCube(_dataSet);
        DismissFileLoad();
    }


   private void ValidateCube(VolumeDataSet _dataSet)
   {
       bool loadable = false;
        string localMsg = "";
       Debug.Log("NAxis_ " + _dataSet.NAxis);
       var list = new List<int>();

        if (_dataSet.NAxis > 2  )
       {
           if (_dataSet.NAxis == 3)
           {
              
               //check if all 3 axis dim are > 1
               for (int i = 0; i < _dataSet.NAxis; i++)
                {
                    Debug.Log("Axis[" + i + "]: " + _dataSet.cubeSize[i]);
                    localMsg += "Axis[" + i + "]: " + _dataSet.cubeSize[i] + "\n";
                    if (_dataSet.cubeSize[i] > 1)
                        list.Add(i);
                }

                if (list.Count == 3)
                {
                    loadable = true;
                }
            }
           else
           {
                // more than 3 axis, check if 3..n axis dim are > 1
                for (int i = 0; i < _dataSet.NAxis; i++)
                {
                    Debug.Log("Axis[" + i + "]: " + _dataSet.cubeSize[i]);
                    localMsg += "Axis[" + i + "]: " + _dataSet.cubeSize[i]+"\n";
                    if (_dataSet.cubeSize[i] > 1)
                        list.Add(i);
                 }

                if (list.Count == 3)
                {
                    loadable = true;
                }
           }
       }

       if(!loadable && list.Count < 3)
       {
           showPopUp = true;
           textPopUp = "NAxis_ " + _dataSet.NAxis + "\n" + localMsg ;
       }
        else if (!loadable && list.Count > 3)
        {
            // showPopUp = true;
            //textPopUp = "NEED TO SELECT SOME AXIS";

            fileLoadCanvassDesktop.transform.Find("RightPanel").gameObject.transform.Find("Axis1").gameObject.transform.Find("Dropdown").GetComponent<Dropdown>().options.Clear();
            fileLoadCanvassDesktop.transform.Find("RightPanel").gameObject.transform.Find("Axis2").gameObject.transform.Find("Dropdown").GetComponent<Dropdown>().options.Clear();
            fileLoadCanvassDesktop.transform.Find("RightPanel").gameObject.transform.Find("Axis3").gameObject.transform.Find("Dropdown").GetComponent<Dropdown>().options.Clear();
            for (int i = 0; i < _dataSet.NAxis; i++)
            {
                if (_dataSet.cubeSize[i] > 1)
                    fileLoadCanvassDesktop.transform.Find("RightPanel").gameObject.transform.Find("Axis1").gameObject.transform.Find("Dropdown").GetComponent<Dropdown>().options.Add((new Dropdown.OptionData() { text = "NAxis" + (i + 1) }));
                    fileLoadCanvassDesktop.transform.Find("RightPanel").gameObject.transform.Find("Axis2").gameObject.transform.Find("Dropdown").GetComponent<Dropdown>().options.Add((new Dropdown.OptionData() { text = "NAxis" + (i + 1) }));
                    fileLoadCanvassDesktop.transform.Find("RightPanel").gameObject.transform.Find("Axis3").gameObject.transform.Find("Dropdown").GetComponent<Dropdown>().options.Add((new Dropdown.OptionData() { text = "NAxis" + (i + 1) }));
            }

            fileLoadCanvassDesktop.transform.Find("RightPanel").gameObject.transform.Find("Axis1").gameObject.transform.Find("Dropdown").GetComponent<Dropdown>().RefreshShownValue();
            fileLoadCanvassDesktop.transform.Find("RightPanel").gameObject.transform.Find("Axis2").gameObject.transform.Find("Dropdown").GetComponent<Dropdown>().RefreshShownValue();
            fileLoadCanvassDesktop.transform.Find("RightPanel").gameObject.transform.Find("Axis3").gameObject.transform.Find("Dropdown").GetComponent<Dropdown>().RefreshShownValue();

                /*
                //fill the dropdown menu OptionData with all COM's Name in ports[]
                foreach (string c in ports)
                {
                    Maindropdown.options.Add(new Dropdown.OptionData() { text = c });
                }
                */
                string _header = "";

            mainCanvassDesktop.SetActive(false);
            fileLoadCanvassDesktop.SetActive(true);
            //GameObject.Find("FileLoadCanvassDesktop").gameObject.SetActive(true);

            IDictionary<string, string> _headerDictionary = _dataSet.GetHeaderDictionary();
            //fileLoadCanvassDesktop.transform.Find("LeftPanel").gameObject.transform.Find("Panel_container").gameObject.transform.Find("InformationPanel").gameObject.transform.Find("Scroll View").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject.transform.Find("Header").GetComponent<Text>().text = "ciaon2";
            foreach (KeyValuePair<string, string> entry in _headerDictionary)
            {
                _header +=entry.Key + "\t\t " + entry.Value+"\n";

            }
            fileLoadCanvassDesktop.transform.Find("LeftPanel").gameObject.transform.Find("Panel_container").gameObject.transform.Find("InformationPanel").gameObject.transform.Find("Scroll View").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject.transform.Find("Header").GetComponent<Text>().text += _header;

        }
        else
        {

            loadCube(_dataSet);
        }
      
   }

    private void loadCube(VolumeDataSet _dataSet)
    {
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
        newCube.GetComponent<VolumeDataSetRenderer>().FileName = _dataSet.FileName.ToString();
        newCube.GetComponent<VolumeDataSetRenderer>().MaskFileName = _dataSet.FileName.ToString();


        newCube.SetActive(true);
        checkCubesDataSet();

        //Deactivate and reactivate VolumeInputController to update VolumeInputController's list of datasets

        _volumeInputController.gameObject.SetActive(false);
        _volumeInputController.gameObject.SetActive(true);

        _volumeSpeechController.AddDataSet(newCube.GetComponent<VolumeDataSetRenderer>());

        InformationTab();
    }

    public void DismissFileLoad()
    {
        fileLoadCanvassDesktop.SetActive(false);
        mainCanvassDesktop.SetActive(true);
    }

   /*
    private void ValidateCube(GameObject newCube)
    {
        bool isValid = true;
        newCube.SetActive(true);



        checkCubesDataSet();

        //Deactivate and reactivate VolumeInputController to update VolumeInputController's list of datasets

        _volumeInputController.gameObject.SetActive(false);
        _volumeInputController.gameObject.SetActive(true);

        InformationTab();


        //Debug.Log("NAxis_ " + newCube.GetComponent<VolumeDataSetRenderer>().GetDatsSet().XDim);

        


            }
             */
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
