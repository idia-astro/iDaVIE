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

    public bool reloadNeeded = false;
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
            RecyclableScrollView.Initialize(_videoPosListDataSource);
            reloadNeeded = false;
        }

        if (reloadNeeded)
        {
            RecyclableScrollView.ReloadData();
        }
    }
}
