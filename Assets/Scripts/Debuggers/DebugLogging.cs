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
using SFB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VolumeData;

/// <summary>
/// 
/// </summary>
public class DebugLogging : MonoBehaviour
{
    public TMP_InputField logOutput;
    public Button saveButton;

    public const string defaultFile = "iDaVIE_Log";
    public const string defaultPluginFile = "iDaVIE_Plugin_Log";
    public const string logExtension = ".log";

    private string autosavePath;
    private string pluginSavePath;
    Queue debugLogQueue = new Queue();
    
    // Start is called before the first frame update
    void Start()
    {
        // Initializing the autosave
        var directory = new DirectoryInfo(Application.dataPath);
        var directoryPath = Path.Combine(directory.Parent.FullName, "Outputs/Logs");

        int maxLogs = Config.Instance.numberOfLogsToKeep;

        try
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Rotate existing logs: from maxLogs - 1 down to 0
            for (int i = maxLogs - 1; i >= 0; i--)
            {
                string searchPatternUnity = $"{defaultFile}_{i}_*{logExtension}";
                string searchPatternPlugin = $"{defaultPluginFile}_{i}{logExtension}";
                var existingLog = Directory.GetFiles(directoryPath, searchPatternUnity).FirstOrDefault();

                if (existingLog != null)
                {
                    if (i == maxLogs - 1)
                    {
                        // Delete the oldest log (N=maxLogs-1)
                        File.Delete(existingLog);
                    }
                    else
                    {
                        // Rename log N to log N+1
                        Regex regex = new Regex(@"(\d{8}_\d{6})");
                        Match match = regex.Match(existingLog);
                        string oldTimestamp = match.Groups[1].Value;

                        string newLogName = Path.Combine(directoryPath, $"{defaultFile}_{i + 1}_{oldTimestamp}{logExtension}");
                        File.Move(existingLog, newLogName);
                    }
                }

                existingLog = Directory.GetFiles(directoryPath, searchPatternPlugin).FirstOrDefault();
                if (existingLog != null)
                {
                    if (i == maxLogs - 1)
                    {
                        // Delete the oldest log (N=maxLogs-1)
                        File.Delete(existingLog);
                    }
                    else
                    {
                        // Rename log N to log N+1

                        string newLogName = Path.Combine(directoryPath, $"{defaultPluginFile}_{i + 1}{logExtension}");
                        File.Move(existingLog, newLogName);
                    }
                }
            }

            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            autosavePath = Path.Combine(directoryPath, $"{defaultFile}_0_{timestamp}{logExtension}");
            // if (File.Exists(autosavePath))
            // {
            //     // Move existing log to default with '_old' appended
            //     File.Copy(autosavePath, Path.Combine(directoryPath, defaultFile.Substring(0, defaultFile.Length - 4) + "_previous.log"), true);
            //     File.Delete(autosavePath);
            // }

            // pluginSavePath = Path.Combine(directoryPath, defaultPluginFile);
            // if (File.Exists(pluginSavePath))
            // {
            //     // Move existing log to default with '_old' appended
            //     File.Copy(pluginSavePath, Path.Combine(directoryPath, defaultPluginFile.Substring(0, defaultPluginFile.Length - 4) + "_previous.log"), true);
            //     File.Delete(pluginSavePath);
            // }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.Log("Error moving autosave logs!");
            UnityEngine.Debug.Log(ex);
        }

        // Initializing the event handler
        UnityEngine.Debug.Log("Start debug logging.");
        saveButton.onClick.AddListener(saveToFileClick);

        // Check if default config file was created.
        int newConfig = PlayerPrefs.GetInt("NewConfigFileCreated");
        string configPath = PlayerPrefs.GetString("ConfigFilePath");
        
        // If new default config, inform user and set new to false.
        if (newConfig != 0)
        {
            UnityEngine.Debug.Log($"Default configuration file created at {configPath}");
            PlayerPrefs.SetInt("NewConfigFileCreated", 0);
            PlayerPrefs.Save();
        }
        // Else, inform user of current location of config file in use.
        else
        {
            UnityEngine.Debug.Log($"Using configuration file at {configPath}");
        }
    }

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    /// <summary>
    /// This function gets called whenever a message is sent to the log, and writes it to the debug console.
    /// </summary>
    /// <param name="logString">The message that is sent to the log</param>
    /// <param name="stackTrace">The stack trace that is output when an exception occurs</param>
    /// <param name="type">The type of the message (Message, Warning, Exception)</param>
    void HandleLog(string logString, string stackTrace, LogType type)
    {
        string logMessage = "[" + type + "] : " + logString;
        debugLogQueue.Enqueue(logMessage);
        AutoSave(logMessage);

        if (type == LogType.Exception)
        {
            debugLogQueue.Enqueue(stackTrace);
            AutoSave(stackTrace);
        }
        
        var builder = new StringBuilder();
        foreach (string st in debugLogQueue)
        {
            builder.Append(st).Append("\n");
        }

        logOutput.text = builder.ToString();
    }

    /// <summary>
    /// This function is called when the button on the debug log is clicked. It opens the native file dialog to get the destination
    /// before writing it out.
    /// </summary>
    void saveToFileClick()
    {
        string lastPath = PlayerPrefs.GetString("LastDebugPath");
        if (!Directory.Exists(lastPath))
        {    
            var directory = new DirectoryInfo(Application.dataPath);
            lastPath = Path.Combine(directory.Parent.FullName, "Outputs/Logs");;
        }
        var extensions = new[]
        {
            new ExtensionFilter("Log Files", "log"),
            new ExtensionFilter("All Files", "*"),
        };

        StandaloneFileBrowser.SaveFilePanelAsync("Save log file", lastPath, "iDaVIE_Debug.log", extensions, (string dest) =>
        {
            if (dest.Equals(""))
                return;

            PlayerPrefs.SetString("LastDebugPath", Path.GetDirectoryName(dest));
            PlayerPrefs.Save();
            SaveToFile(dest);
        });
    }

    /// <summary>
    /// A function that writes out the debug log at that time to a file.
    /// </summary>
    /// <param name="file">The name of the file to be written to.</param>
    void SaveToFile(string file)
    {
        var builder = new StringBuilder();
        foreach (string st in debugLogQueue)
        {
            builder.Append(st).Append("\n");
        }

        StreamWriter writer = new StreamWriter(file, false);
        writer.Write(builder.ToString());
        writer.Close();
    }

    /// <summary>
    /// This function is called when something is written to the log, and will automatically update the autosaved log.
    /// </summary>
    /// <param name="message">The message that was written to the log, to be written to the file now.</param>
    void AutoSave(string message)
    {
        StreamWriter writer = new StreamWriter(autosavePath, true);
        writer.Write(message + "\n");
        writer.Close();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
