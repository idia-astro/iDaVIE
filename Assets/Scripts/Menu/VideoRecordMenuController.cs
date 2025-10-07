using System;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class VideoRecordMenuController : MonoBehaviour
{

    private VolumeInputController _volumeInputController = null;

    private VideoPosRecorder _videoPosRecorder = null;

    private VideoRecPointListController _videoRecPointListController = null;

    public GameObject videoRecPosListMenu;

    private GameObject headPosImage = null;
    private GameObject cursorPosImage = null;

    public Text notificationBar = null;
    
    // Start is called before the first frame update
    void Start()
    {

    }

    public void OnEnable()
    {
        if (_volumeInputController == null)
            _volumeInputController = FindObjectOfType<VolumeInputController>();

        if (_videoPosRecorder == null)
            _videoPosRecorder = new VideoPosRecorder();

        _volumeInputController._videoPosRecorder = _videoPosRecorder;

        if (headPosImage == null || cursorPosImage == null)
        {
            headPosImage = this.gameObject.transform.Find("Content").Find("FirstRow").Find("ToggleRecordingMode").Find("HeadPosImage").gameObject;
            cursorPosImage = this.gameObject.transform.Find("Content").Find("FirstRow").Find("ToggleRecordingMode").Find("CursorPosImage").gameObject;
        }

        if (_videoRecPointListController == null)
        {
            _videoRecPointListController = videoRecPosListMenu.gameObject.GetComponentInChildren<VideoRecPointListController>(true);
            _videoRecPointListController.setVideoRecorder(_videoPosRecorder);
            _videoRecPointListController.setVolumeInputController(_volumeInputController);
        }

        _volumeInputController.InteractionStateMachine.Fire(VolumeInputController.InteractionEvents.StartVideoRecording);
    }

    // Update is called once per frame
    void Update()
    {

    }

    //TODO: Set the button to toggle instead
    public void ToggleRecordingMode()
    {
        switch (_videoPosRecorder.GetRecordingMode())
        {
            case VideoPosRecorder.videoLocRecMode.CURSOR:
                _videoPosRecorder.SetRecordingMode(VideoPosRecorder.videoLocRecMode.HEAD);
                headPosImage.SetActive(true);
                cursorPosImage.SetActive(false);
                notificationBar.text = "Recording head position";
                break;
            case VideoPosRecorder.videoLocRecMode.HEAD:
                _videoPosRecorder.SetRecordingMode(VideoPosRecorder.videoLocRecMode.CURSOR);
                cursorPosImage.SetActive(true);
                headPosImage.SetActive(false);
                notificationBar.text = "Recording cursor position";
                break;
            default:
                Debug.LogError("Invalid video recording mode... how did you manage that?");
                break;
        }
    }

    public void ExitVideoRecordingMode()
    {
        if (_volumeInputController.InteractionStateMachine.State == VolumeInputController.InteractionState.VideoCamPosRecording)
            _volumeInputController.InteractionStateMachine.Fire(VolumeInputController.InteractionEvents.EndVideoRecording);
        else
            Debug.LogError("Trying to exit video recording mode while not in video recording mode... How did you manage that?");

        gameObject.SetActive(false);
        videoRecPosListMenu.SetActive(false);
    }

    /// <summary>
    /// 
    /// </summary>
    public void ExportToFile()
    {

        var directory = new DirectoryInfo(Application.dataPath);
        var directoryPath = Path.Combine(directory.Parent.FullName, "Outputs/VideoScripts");

        string dataFileName = Path.GetFileName(_volumeInputController.ActiveDataSet.FileName);
        string path = "";
        try
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            string filename = string.Format("{0}_VideoScript_{1}.idvs", dataFileName, System.DateTime.Now.ToString("yyyyMMdd_Hmmssf"));
            path = Path.Combine(directoryPath, filename);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.Log("Error creating video script directory!");
            UnityEngine.Debug.Log(ex);
        }
        if (_videoPosRecorder.ExportToIDVS(path) == 0)
        {
            _volumeInputController.VibrateController(Valve.VR.SteamVR_Input_Sources.RightHand);
            ToastNotification.ShowSuccess($"Exported positions to video script {Path.GetFileName(path)}.");
        }
        else
        {
            ToastNotification.ShowError("Failed to export video script file");
        }
    }

    /// <summary>
    /// This function is called when the user presses the list button in the menu, and opens the list of points.
    /// </summary>
    public void OpenListOfLocations()
    {
        spawnMenu(videoRecPosListMenu);
    }

    public void spawnMenu(GameObject menu)
    {
        Vector3 playerPos = Camera.main.transform.position;
        Vector3 playerDirection = Camera.main.transform.forward;
        Quaternion playerRotation = Camera.main.transform.rotation;
        float spawnDistance = 1.5f;

        Vector3 spawnPos = playerPos + playerDirection * spawnDistance;

        menu.transform.position = spawnPos;
        menu.transform.rotation = Quaternion.LookRotation(new Vector3(spawnPos.x - playerPos.x, 0, spawnPos.z - playerPos.z));

        if (!menu.activeSelf)
        {
            menu.SetActive(true);
        }
    }
}