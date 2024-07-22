using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using TMPro;
using UnityEngine;
using VolumeData;

/// <summary>
/// Class for behavior of the voice command indicator under the cursor info
/// </summary>
public class VoiceCommandIndicator : MonoBehaviour
{
    public GameObject VoiceCommandOnText;
    public GameObject VoiceCommandOffText;
    public GameObject VoiceCommandOnIcon;
    public GameObject VoiceCommandOffIcon;

    public VolumeInputController VolumeInputController;

    private Config _config;

    // Start is called before the first frame update
    void Start()
    {
        _config = Config.Instance;
    }

    /// <summary>
    /// Update looks at the config and cursor info status to set whether the voice command
    /// indicator is displayed and whether to use the simple icon or text version
    /// </summary>
    void Update()
    {
        if (_config.displayVoiceCommandStatus && VolumeInputController.ShowCursorInfo)
        {
            if (Application.isFocused)
            {
                if (_config.useSimpleVoiceCommandStatus)
                {
                    VoiceCommandOnIcon.SetActive(true);
                    VoiceCommandOffIcon.SetActive(false);
                }
                else
                {
                    VoiceCommandOnText.SetActive(true);
                    VoiceCommandOffText.SetActive(false);
                }
            }
            else
            {
                if (_config.useSimpleVoiceCommandStatus)
                {
                    VoiceCommandOnIcon.SetActive(false);
                    VoiceCommandOffIcon.SetActive(true);
                }
                else
                {
                    VoiceCommandOnText.SetActive(false);
                    VoiceCommandOffText.SetActive(true);
                }
            }
        }
        else
        {
            VoiceCommandOnText.SetActive(false);
            VoiceCommandOffText.SetActive(false);
            VoiceCommandOnIcon.SetActive(false);
            VoiceCommandOffIcon.SetActive(false);
        }
    }
}
