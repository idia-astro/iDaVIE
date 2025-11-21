/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 *
 * This file is part of the iDaVIE project.
 *
 * iDaVIE is free software: you can redistribute it and/or modify it under the terms 
 * of the GNU Lesser General Public License (LGPL) as published by the Free Software 
 * Foundation, either version 3 of the License, or (at your option) any later version.
 *
 * iDaVIE is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR 
 * PURPOSE. See the GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License along with 
 * iDaVIE in the LICENSE file. If not, see <https://www.gnu.org/licenses/>.
 *
 * Additional information and disclaimers regarding liability and third-party 
 * components can be found in the DISCLAIMER and NOTICE files included with this project.
 *
 */
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
    public GameObject WindowUnfocusedText;
    public GameObject WindowUnfocusedIcon;

    [SerializeField]
    private VolumeInputController volumeInputController;
    [SerializeField]
    private VolumeCommandController volumeCommandController;

    private Config _config;

    private void Awake()
    {
        _config = Config.Instance;
    }

    void Start()
    {
        UpdateFocusIndicator(Application.isFocused);
        if (_config.usePushToTalk)
        {
            volumeInputController.PushToTalkButtonPressed += OnPushToTalkButtonChanged;
            volumeInputController.PushToTalkButtonReleased += OnPushToTalkButtonChanged;
        }
    }

    private void OnDestroy()
    {
        if (_config.usePushToTalk)
        {
            volumeInputController.PushToTalkButtonPressed -= OnPushToTalkButtonChanged;
            volumeInputController.PushToTalkButtonReleased -= OnPushToTalkButtonChanged;
        }
    }

    void OnPushToTalkButtonChanged()
    {
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
    private void UpdateFocusIndicator(bool hasFocus)
    {
        if (_config.displayVoiceCommandStatus && volumeInputController.ShowCursorInfo)
        {
            if (hasFocus && volumeCommandController.IsVoiceRecognitionActive)
            {
                if (_config.useSimpleVoiceCommandStatus)
                {
                    VoiceCommandOnIcon.SetActive(true);
                    VoiceCommandOffIcon.SetActive(false);
                    WindowUnfocusedIcon.SetActive(false);
                }
                else
                {
                    VoiceCommandOnText.SetActive(true);
                    VoiceCommandOffText.SetActive(false);
                    WindowUnfocusedText.SetActive(false);
                }
            }
            else
            {
                if (_config.useSimpleVoiceCommandStatus)
                {
                    VoiceCommandOnIcon.SetActive(false);
                    VoiceCommandOffIcon.SetActive(true);
                    if (!hasFocus)
                    {
                        WindowUnfocusedIcon.SetActive(true);
                    }
                    else
                    {
                        WindowUnfocusedIcon.SetActive(false);
                    }
                }
                else
                {
                    VoiceCommandOnText.SetActive(false);
                    VoiceCommandOffText.SetActive(true);
                    if (!hasFocus)
                    {
                        WindowUnfocusedText.SetActive(true);
                    }
                    else
                    {
                        WindowUnfocusedText.SetActive(false);
                    }
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
