using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CameraControllerTool : MonoBehaviour
{
    public GameObject QuickMenuCanvas;
    public Camera targetCamera;


    public float VibrationDuration = 0.25f;
    public float VibrationFrequency = 100.0f;
    public float VibrationAmplitude = 1.0f;


    private VolumeInputController _volumeInputController = null;
    // Start is called before the first frame update

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnEnable()
    {
        if (_volumeInputController == null)
            _volumeInputController = FindObjectOfType<VolumeInputController>();
    }

    // public interface 
    public void OnUse()
    {
        StartCoroutine(ToolCoroutine());

        _volumeInputController.VibrateController(_volumeInputController.PrimaryHand, VibrationDuration, VibrationFrequency, VibrationAmplitude);
    }

    public IEnumerator ToolCoroutine()
    {
        WaitForEndOfFrame wfeof = new WaitForEndOfFrame();

        QuickMenuCanvas.GetComponent<CanvasGroup>().alpha = 0;

        StartCoroutine(makeScreenshot());
        yield return wfeof;

    }


    IEnumerator makeScreenshot()
    {

        yield return new WaitForSeconds(0.1f);
        yield return new WaitForEndOfFrame();
 
        int width = Screen.width;
        int height = Screen.height;
        var rt = new RenderTexture(width*3, height*3, 24);
        var oldTargetTexture = targetCamera.targetTexture;

          targetCamera.targetTexture=rt;
       
        RenderTexture.active = targetCamera.targetTexture;

        // Render the camera's view.
        targetCamera.Render();
        // Make a new texture and read the active Render Texture into it.
        Texture2D tex = new Texture2D(targetCamera.targetTexture.width, targetCamera.targetTexture.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, targetCamera.targetTexture.width, targetCamera.targetTexture.height), 0, 0);
        tex.Apply();
        // Encode texture into PNG
        byte[] bytes = tex.EncodeToPNG();
        Destroy(tex);
        var directory = new DirectoryInfo(Application.dataPath);
        var directoryPath = Path.Combine(directory.Parent.FullName, "Outputs/Camera");
        try
        {
            if (!Directory.Exists(directoryPath))
            {
             Directory.CreateDirectory(directoryPath);
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine(ex.Message);
        }
        var path = Path.Combine(directoryPath, string.Format("Screenshot_{0}.png", DateTime.Now.ToString("yyyyMMdd_Hmmssf")));
        File.WriteAllBytes(path, bytes);
        QuickMenuCanvas.GetComponent<CanvasGroup>().alpha = 1;
        targetCamera.targetTexture = oldTargetTexture;

        yield return null;
    }
}
