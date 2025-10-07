using PolyAndCode.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    // Start is called before the first frame update
    void Start()
    {
        if (CellIndex % 2 != 0)
            GetComponent<Image>().color = _lightGrey;
        else if (CellIndex % 2 != 1)
            GetComponent<Image>().color = _darkGrey;
        CellHeight = GetComponent<RectTransform>().rect.height;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void GoTo()
    {
        Teleport(_location.position, _location.rotation);
    }

    private void Teleport(Vector3 pos, Vector3 rot)
    {
        _volumeInputController.TeleportToVidRecLoc(pos, rot);
    }

    public void RemoveFromList()
    {
        _videoPosRecorder.removeLocation(_location);
    }
}
