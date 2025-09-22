using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Win32;
using Unity.VisualScripting;
using UnityEngine;

public class VideoPosRecorder
{
    private struct videoRecLocation
    {
        public Vector3 position;
        public Vector3 rotation;

        public videoRecLocation(Vector3 pos, Vector3 rot)
        {
            this.position = pos;
            this.rotation = rot;
        }
    }

    public enum videoLocRecMode
    {
        HEAD,
        CURSOR
    }

    private List<videoRecLocation> _videoPositions;

    videoLocRecMode _recordingMode = videoLocRecMode.HEAD;

    public VideoPosRecorder()
    {
        _videoPositions = new List<videoRecLocation>();
    }

    public videoLocRecMode GetRecordingMode()
    {
        return _recordingMode;
    }

    public void SetRecordingMode(videoLocRecMode mode = videoLocRecMode.HEAD)
    {
        this._recordingMode = mode;
    }

    public void addLocation(Vector3 position, Vector3 rotation)
    {
        videoRecLocation loc = new videoRecLocation(position, rotation);
        _videoPositions.Add(loc);
    }

    public int ExportToIDVS(string path)
    {
        StringBuilder output = new StringBuilder();

        // Video settings
        output.AppendLine("# Video settings");
        output.AppendLine("Height : 720");
        output.AppendLine("Width : 1280");
        output.AppendLine("FrameRate : 25");
        output.AppendLine("LogoPos : BR");
        output.AppendLine();

        // Position declarations
        output.AppendLine("# List of positions:");
        for (int i = 0; i < _videoPositions.Count; i++)
        {
            videoRecLocation loc = _videoPositions[i];
            output.AppendLine(string.Format("p{0} is {{{1}, {2}}}", i, loc.position.ToString("F3"), loc.rotation.ToString("F3")));
        }
        output.AppendLine();

        // Script commands
        output.AppendLine("# Script:");
        output.AppendLine($"Start at p1");

        // Write to file
        try
        {
            StreamWriter writer = new StreamWriter(path, false);
            writer.Write(output.ToString());
            writer.Close();
        }
        catch (System.Exception)
        {
            Debug.LogError($"Error exporting positions to IDVS file \'{path}\'!");
            Debug.Log($"File contents:\n{output.ToString()}");
            return 1;
        }

        return 0;
    }
}
