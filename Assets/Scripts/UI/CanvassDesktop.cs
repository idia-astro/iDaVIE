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

    private bool showPopUp = false;
    private string textPopUp = "";
    private VolumeInputController _volumeInputController;
    private VolumeSpeechController _volumeSpeechController;

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

    public void LoadFileFromFileSystem()
    {
        Debug.Log("LoadFileFromFileSystem");

        // Open file with filter
        var extensions = new[] {
            new ExtensionFilter("Fits Files", "fits" ),
            new ExtensionFilter("All Files", "*" ),
        };

        var paths = StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, true);

        if (paths != null)
        {
         
            VolumeDataSet _dataSet = VolumeDataSet.LoadDataFromFitsFile(paths[0].ToString(), false);
            ValidateCube(_dataSet);
         
           
          

        }
        //volumeDataSetManager.;
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
        if (!loadable && list.Count > 3)
        {
            showPopUp = true;
            textPopUp = "NEED TO SELECT SOME AXIS";
        }
        else
       {

            Vector3 oldpos = new Vector3(0, 0f, 0);
            Quaternion oldrot = Quaternion.identity;
            Vector3 oldscale = new Vector3(0, 0f, 0);
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
