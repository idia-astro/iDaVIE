using System;
using System.IO;
using UnityEngine;

public class VideoRecordMenuController : MonoBehaviour
{

    private VolumeInputController _volumeInputController = null;

    private VideoPosRecorder _videoPosRecorder = null;

    public GameObject videoRecPosListMenu;

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

        _volumeInputController.InteractionStateMachine.Fire(VolumeInputController.InteractionEvents.StartVideoRecording);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SetRecordingModeHead()
    {
        _videoPosRecorder.SetRecordingMode(VideoPosRecorder.videoLocRecMode.HEAD);
    }

    public void SetRecordingModeCursor()
    {
        _videoPosRecorder.SetRecordingMode(VideoPosRecorder.videoLocRecMode.CURSOR);
    }

    public void ExitVideoRecordingMode()
    {
        if (_volumeInputController.InteractionStateMachine.State == VolumeInputController.InteractionState.VideoCamPosRecording)
            _volumeInputController.InteractionStateMachine.Fire(VolumeInputController.InteractionEvents.EndVideoRecording);
        else
            Debug.LogError("Trying to exit video recording mode while not in video recording mode... How did you manage that?");

        this.gameObject.SetActive(false);
    }

    public void ExportToFile()
    {

        var directory = new DirectoryInfo(Application.dataPath);
        var directoryPath = Path.Combine(directory.Parent.FullName, "Outputs/VideoScripts");

        string dataFileName = "DataFile";
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
        _videoPosRecorder.ExportToIDVS(path);
    }

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