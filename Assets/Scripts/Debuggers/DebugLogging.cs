using SFB;
using System.Collections;
using System.Collections.Generic;
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
    Queue debugLogQueue = new Queue();
    
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Start debug logging.");
        saveButton.onClick.AddListener(saveToFileClick);
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
        debugLogQueue.Enqueue("[" + type + "] : " + logString);
        if (type == LogType.Exception)
            debugLogQueue.Enqueue(stackTrace);
        
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
        string lastPath = PlayerPrefs.GetString("LastPath");
        if (!Directory.Exists(lastPath))
            lastPath = "";
        
        var extensions = new[]
        {
            new ExtensionFilter("Log Files", "log"),
            new ExtensionFilter("All Files", "*"),
        };

        StandaloneFileBrowser.SaveFilePanelAsync("Save log file", lastPath, "i-DaVIE_Debug.log", extensions, (string dest) =>
        {
            if (dest.Equals(""))
                return;

            PlayerPrefs.SetString("LastPath", Path.GetDirectoryName(dest));
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

    // Update is called once per frame
    void Update()
    {
        
    }
}
