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

   void Start()
    {
        _config = Config.Instance;
        UpdateFocusIndicator(Application.isFocused);
    }

    void OnApplicationFocus(bool hasFocus)
    {
        UpdateFocusIndicator(hasFocus);
    }
    
    /// <summary>
    /// Method looks at the config and cursor info status to set whether the voice command
    /// indicator is displayed (based on hasFocus) and whether to use the simple icon or text version
    /// </summary>
    /// <param name="hasFocus">variable determines whether indicator should show that voice commands
    /// work or not</param>
    void UpdateFocusIndicator(bool hasFocus)
    {
        if (_config.displayVoiceCommandStatus && VolumeInputController.ShowCursorInfo)
        {
            if (hasFocus)
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
