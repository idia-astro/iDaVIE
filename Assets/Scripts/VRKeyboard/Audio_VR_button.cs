using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows.Speech;

public class Audio_VR_button : Abstract_VR_button
{

    private bool clicked = false;

    protected new void Update()
    {
        if (!clicked)
        {
            if (keepSelected > 0)
            {
                if (materialIndex == 0)
                {
                    materialIndex = 1;
                    renderer.sharedMaterial = materials[materialIndex];
                }
                keepSelected -= Time.deltaTime;
            }
            else if (materialIndex == 1)
            {
                materialIndex = 0;
                renderer.sharedMaterial = materials[materialIndex];
            }
        }
    }

    public override void onPress()
    {
        if(keyboardManager.dictationRecognizer.Status == SpeechSystemStatus.Stopped)
        {
            keyboardManager.dictationRecognizer.Start();
            clicked = true;
            renderer.sharedMaterial = materials[1];
            Debug.Log("recognition started");
        }
        else if(keyboardManager.dictationRecognizer.Status == SpeechSystemStatus.Running)
        {
            keyboardManager.dictationRecognizer.Stop();
            clicked = false;
            renderer.sharedMaterial = materials[0];
            Debug.Log("recognition stopped");
        }
    }

    public void OnApplicationQuit()
    {
        keyboardManager.dictationRecognizer.Stop();
        Debug.Log("recognition stopped");
    }
}