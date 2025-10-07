using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.Text;
using Microsoft.Win32;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class VideoPosRecorder
{
    /// <summary>
    /// Struct used to store a location for exporting to a video script file.
    /// </summary>
    public struct videoRecLocation
    {
        public Vector3 position;
        public Vector3 rotation;

        public videoRecLocation(Vector3 pos, Vector3 rot)
        {
            this.position = pos;
            this.rotation = rot;
        }

        public override string ToString()
        {
            string result = $"{position.ToString()},\n{rotation.ToString()}";
            return result;
        }
    }

    /// <summary>
    /// The various recording modes that the recorder can be in.
    /// </summary>
    public enum videoLocRecMode
    {
        HEAD,
        CURSOR
    }

    private List<videoRecLocation> _videoPositions;

    videoLocRecMode _recordingMode = videoLocRecMode.HEAD;

    public bool listChanged { get; private set; } = false;

    /// <summary>
    /// Constructor, initialises list.
    /// </summary>
    public VideoPosRecorder()
    {
        _videoPositions = new List<videoRecLocation>();
    }

    /// <summary>
    /// Returns a copy of the list of videopositions. Since all members of the videoRecLocation struct are value types, it is implicitly a deep copy.
    /// </summary>
    /// <returns>A copy of the video positions in this recorder.</returns>
    public List<videoRecLocation> GetVideoRecLocationList()
    {
        var list = new List<videoRecLocation>(_videoPositions);
        return list;
    }

    public int GetVideoRecLocCount()
    {
        return _videoPositions.Count;
    }

    /// <summary>
    /// Returns the current recording mode (used by VolumeInputController).
    /// </summary>
    /// <returns>The current recording mode.</returns>
    public videoLocRecMode GetRecordingMode()
    {
        return _recordingMode;
    }

    /// <summary>
    /// Sets the recording mode (used by VolumeInputController to determine the location to send to addLocation).
    /// </summary>
    /// <param name="mode">The mode to set _recordingMode to.</param>
    public void SetRecordingMode(videoLocRecMode mode = videoLocRecMode.HEAD)
    {
        this._recordingMode = mode;
    }

    /// <summary>
    /// Adds a location to _videoPositions.
    /// </summary>
    /// <param name="position">The position of the location.</param>
    /// <param name="rotation">The direction of the location.</param>
    public void addLocation(Vector3 position, Vector3 rotation)
    {
        videoRecLocation loc = new videoRecLocation(position, rotation);
        _videoPositions.Add(loc);
        listChanged = true;
    }

    /// <summary>
    /// Function is called by the VideoPosList cell when the user selects the delete button in the cell.
    /// </summary>
    /// <param name="loc">The location to be removed.</param>
    public void removeLocation(videoRecLocation loc)
    {
        _videoPositions.Remove(loc);
        listChanged = true;
    }

    /// <summary>
    /// This function is called by the VideoRecPointListController to acknowledge the change in the list data.
    /// </summary>
    public void listUpdated()
    {
        listChanged = false;
    }

    /// <summary>
    /// Exports the list of positions (stored in _videoPositions) to a file.
    /// </summary>
    /// <param name="path">Path to the file that the list of positions should be exported to.</param>
    /// <returns>0 if successful, 1 if exception is thrown.</returns>
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
        output.AppendLine("# Takes the form:");
        output.AppendLine("#    <alias> is {<location>, <direction>}");
        output.AppendLine("# Alias can be any combination of characters, excluding whitespace.");
        output.AppendLine("# Both location and direction are Vector3, printed in the form `(x, y, z)`,");
        output.AppendLine("# and are relative to the datacube's normalised position and rotation.");
        output.AppendLine();
        for (int i = 0; i < _videoPositions.Count; i++)
        {
            videoRecLocation loc = _videoPositions[i];
            output.AppendLine(string.Format("p{0} is {{{1}, {2}}}", i, loc.position.ToString("F3"), loc.rotation.ToString("F3")));
        }
        output.AppendLine();

        // Script commands
        output.AppendLine("# Script:");
        output.AppendLine("# Accepted commands (see documentation for details):");
        //TODO: Make this pull from IDVSParser's static list of examples
        output.AppendLine("#    - Start at <alias>");
        output.AppendLine("#    - Wait <n> seconds");
        output.AppendLine("#    - Move in <METHOD> to <alias> over <n> seconds");
        output.AppendLine("#        - Methods allowed: (LINE, ARC)");
        output.AppendLine("#    - Rotate around <alias> <n> times");
        output.AppendLine();
        output.AppendLine("Start at p1");

        // Write to file
        try
        {
            StreamWriter writer = new StreamWriter(path, false);
            writer.Write(output.ToString());
            writer.Close();
            Debug.Log($"Exported positions to IDVS file \'{path}\'.");
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
