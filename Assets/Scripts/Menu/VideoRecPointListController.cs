using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PolyAndCode.UI;

/// <summary>
/// This class is used to control the list of points for the video recording mode.
/// It is built on the RecyclableScrollView package.
/// </summary>
public class VideoRecPointListController : MonoBehaviour
{
    VideoPosRecorder _videoPosRecorder;

    VideoPosListDataSource _videoPosListDataSource;
    public GameObject RecyclableScrollViewPrefab;

    public RecyclableScrollRect RecyclableScrollView;

    VolumeInputController _volumeInputController;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // If not initialised and there are points to list, initialise.
        if (!RecyclableScrollView && _videoPosRecorder != null && _videoPosRecorder.GetVideoRecLocCount() > 0)
        {
            _videoPosListDataSource = new VideoPosListDataSource(_videoPosRecorder);
            RecyclableScrollView = Instantiate(RecyclableScrollViewPrefab, this.transform).GetComponent<RecyclableScrollRect>();
            _videoPosListDataSource.Initialize(RecyclableScrollView, _volumeInputController);
            RecyclableScrollView.Initialize(_videoPosListDataSource);
            Debug.Log("RecyclableScrollView for video recording positions instantiated.");
        }

        // If the data has changed, update the list.
        if (_videoPosRecorder.listChanged)
        {
            _videoPosListDataSource.InitData();
            RecyclableScrollView.ReloadData();
            _videoPosRecorder.listUpdated();
        }
    }

    /// <summary>
    /// Function used to set the VideoPosRecorder that should be used as the data source for this instance.
    /// </summary>
    /// <param name="vidPosRecorder">A reference to the VideoPosRecorder that should be used.</param>
    public void setVideoRecorder(VideoPosRecorder vidPosRecorder)
    {
        _videoPosRecorder = vidPosRecorder;
    }

    /// <summary>
    /// Function used to set the VolumeInputController, passed through to cells of the scroll view to manage some of the functions available.
    /// </summary>
    /// <param name="volInController">A reference to the VolumeInputController that should be used.</param>
    public void setVolumeInputController(VolumeInputController volInController)
    {
        _volumeInputController = volInController;
    }
}
