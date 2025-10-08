using PolyAndCode.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This class holds the data for displaying the list of video positions in the RecyclableScrollView, and is a passthrough for some of the available functionality.
/// </summary>
public class VideoPosListCell : MonoBehaviour, ICell
{

    public int CellIndex { get; private set; }
    public float CellHeight { get; private set; }

    public TMP_Text locationType;

    public TMP_Text indexText;

    public TMP_Text locText;

    private static readonly Color _lightGrey = new Color(0.4039216f, 0.5333334f, 0.5882353f, 1f);
    private static readonly Color _darkGrey = new Color(0.2384301f, 0.3231786f, 0.3584906f, 1f);

    private static readonly string headPos = "Camera position";
    private static readonly string cursorPos = "Cursor position";
    private VideoPosRecorder.videoRecLocation _location;
    private VideoPosRecorder _videoPosRecorder;
    private VolumeInputController _volumeInputController;

    /// <summary>
    /// This function is called by VideoPosListDataSource to set the data values of the cell, and update the cell UI.
    /// </summary>
    /// <param name="loc">The VideoLocation this cell represents.</param>
    /// <param name="index">The index this cell is displaying.</param>
    /// <param name="vidPosRec">Reference to the VideoPosRecorder instance that manages the list of locations, used for passing functions through.</param>
    /// <param name="volInController">Reference to the VolumeInputController instance that manages most of the user input, used for passing functions through.</param>
    public void ConfigureCell(VideoPosRecorder.videoRecLocation loc, int index, VideoPosRecorder vidPosRec, VolumeInputController volInController)
    {
        _location = loc;
        CellIndex = index;
        _videoPosRecorder = vidPosRec;
        _volumeInputController = volInController;

        indexText.SetText($"p{index.ToString()}");
        locText.SetText(_location.ToString());
        if (_location.rotation == Vector3.zero)
        {
            locationType.SetText(cursorPos);
        }
        else
        {
            locationType.SetText(headPos);
        }
    }

    /// <summary>
    /// Is called before the first frame update.
    /// Determines the colour and height of the cell.
    /// </summary>
    void Start()
    {
        if (CellIndex % 2 != 0)
            GetComponent<Image>().color = _lightGrey;
        else if (CellIndex % 2 != 1)
            GetComponent<Image>().color = _darkGrey;
        CellHeight = GetComponent<RectTransform>().rect.height;
    }

    /// <summary>
    /// The function that is triggered when the user clicks on the teleport button of the cell.
    /// </summary>
    public void GoTo()
    {
        _volumeInputController.TeleportToVidRecLoc(_location.position, _location.rotation);
    }

    /// <summary>
    /// Function is called when the user clicks on the delete button of the cell.
    /// </summary>
    public void RemoveFromList()
    {
        _videoPosRecorder.removeLocation(_location);
    }
}
