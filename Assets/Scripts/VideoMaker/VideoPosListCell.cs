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

    private static readonly Color _lightGrey = new Color(0.4039216f, 0.5333334f, 0.5882353f, 1f);
    private static readonly Color _darkGrey = new Color(0.2384301f, 0.3231786f, 0.3584906f, 1f);

    private VideoPosRecorder.videoRecLocation _location;
    private VideoPosRecorder _videoPosRecorder;
    public void ConfigureCell(VideoPosRecorder.videoRecLocation loc, int index, VideoPosRecorder vidPosRec)
    {
        _location = loc;
        CellIndex = index;
        indexText.SetText(index.ToString());
        _videoPosRecorder = vidPosRec;
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
        Debug.Log("Teleporting is still to do...");
    }

    public void RemoveFromList()
    {
        Debug.Log("Removal to be implemented later...");
    }
}
