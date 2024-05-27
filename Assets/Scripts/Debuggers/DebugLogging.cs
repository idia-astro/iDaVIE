using SFB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 
/// </summary>
public class DebugLogging : MonoBehaviour
{
    public TMP_InputField logOutput;
    public Button saveButton;

    public const string defaultFile = "i-DaVIE-v_Log.log";

    private string autosavePath;
    Queue debugLogQueue = new Queue();
    
    // Start is called before the first frame update
    void Start()
    {
        // Initializing the autosave
        var directory = new DirectoryInfo(Application.dataPath);
        var directoryPath = Path.Combine(directory.Parent.FullName, "Outputs/Logs");
        try
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            autosavePath = Path.Combine(directoryPath, defaultFile);
            if (File.Exists(autosavePath))
            {
                // Move existing log to default with '_old' appended
                File.Copy(autosavePath, Path.Combine(directoryPath, defaultFile.Substring(0, defaultFile.Length - 4) + "_previous.log"), true);
                File.Delete(autosavePath);
            }
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

        StandaloneFileBrowser.SaveFilePanelAsync("Save log file", lastPath, "i-DaVIE_Debug.log", extensions, (string dest) =>
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
