using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PolyAndCode.UI;

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
        if (!RecyclableScrollView && _videoPosRecorder != null && _videoPosRecorder.GetVideoRecLocCount() > 0)
        {
            _videoPosListDataSource = new VideoPosListDataSource(_videoPosRecorder);
            RecyclableScrollView = Instantiate(RecyclableScrollViewPrefab, this.transform).GetComponent<RecyclableScrollRect>();
            _videoPosListDataSource.Initialize(RecyclableScrollView, _volumeInputController);
            RecyclableScrollView.Initialize(_videoPosListDataSource);
            Debug.Log("RecyclableScrollView for video recording positions instantiated.");
        }

        if (_videoPosRecorder.listChanged)
        {
            _videoPosListDataSource.InitData();
            RecyclableScrollView.ReloadData();
            _videoPosRecorder.listUpdated();
        }
    }

    public void setVideoRecorder(VideoPosRecorder vidPosRecorder)
    {
        _videoPosRecorder = vidPosRecorder;
    }

    public void setVolumeInputController(VolumeInputController volInController)
    {
        _volumeInputController = volInController;
    }
}
