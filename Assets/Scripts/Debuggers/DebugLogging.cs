using SFB;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        StandaloneFileBrowser.OpenFilePanelAsync("Open File", lastPath, extensions, false, (string[] paths) =>
        {
            if (paths.Length == 1)
            {
                PlayerPrefs.SetString("LastPath", Path.GetDirectoryName(paths[0]));
                PlayerPrefs.Save();

                SaveToFile(paths[0]);
            }
        });
    }

    void SaveToFile(string file)
    {
        var builder = new StringBuilder();
        foreach (string st in debugLogQueue)
        {
            builder.Append(st).Append("\n");
        }

        StreamWriter writer = new StreamWriter(file, true);
        writer.Write(builder.ToString());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
