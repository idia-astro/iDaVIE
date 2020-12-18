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
            var path = Path.Combine(directoryPath, string.Format("Screenshot_{0}.png", DateTime.Now.ToString("yyyyMMdd_Hmmssffff")));

            // For testing purposes, also write to a file in the project folder
            File.WriteAllBytes(path, bytes);
            //write geotag
          


            
            QuickMenuCanvas.GetComponent<CanvasGroup>().alpha = 1;

        /*
            toolControllerComponent.CameraControlUIp.GetComponent<CanvasGroup>().alpha = 1;
            GameObject.Find("Canvas_Oculus").gameObject.transform.Find("CameraControlUI").GetComponent<CanvasGroup>().alpha = 1;

            if (StateSingleton.stateView == StateSingleton.StateView.MODE2D_PLUS_OCULUS)
            {


                toolControllerComponent.hands[0].transform.position = oldpositionL;
                toolControllerComponent.hands[1].transform.position = oldpositionR;






            }

            StartCoroutine(ShowNotification("Done!", 1.5f));
            */
        yield return null;


    }




}
