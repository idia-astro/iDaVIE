using System.Collections;
using System.Collections.Generic;
using PolyAndCode.UI;
using UnityEngine;

public class VideoPosListDataSource : MonoBehaviour, IRecyclableScrollRectDataSource
{
    [SerializeField]
    RecyclableScrollRect _recyclableScrollRect;

    [SerializeField]
    private int _dataLength;

    //Dummy data List
    private List<VideoPosRecorder.videoRecLocation> _videoLocList = new List<VideoPosRecorder.videoRecLocation>();

    public VideoPosRecorder _videoPosRecorder;

    private VolumeInputController _volumeInputController;

    public VideoPosListDataSource(VideoPosRecorder vidPosRec)
    {
        _videoPosRecorder = vidPosRec;
    }

    //Recyclable scroll rect's data source must be assigned in Awake.
    private void Awake()
    {
        InitData();
        _recyclableScrollRect.DataSource = this;
    }

    public void Initialize(RecyclableScrollRect rect, VolumeInputController VolInController)
    {
        _recyclableScrollRect = rect;
        _volumeInputController = VolInController;
        Awake();
    }

    public void InitData()
    {
        _videoLocList = _videoPosRecorder.GetVideoRecLocationList();
    }

    #region DATA-SOURCE

    /// <summary>
    /// Data source method. return the list length.
    /// </summary>
    public int GetItemCount()
    {
        return _videoLocList.Count;
    }

    /// <summary>
    /// Called for a cell every time it is recycled
    /// Implement this method to do the necessary cell configuration.
    /// </summary>
    public void SetCell(ICell cell, int index)
    {
        //Casting to the implemented Cell
        var item = cell as VideoPosListCell;
        item.ConfigureCell(_videoLocList[index], index, _videoPosRecorder, _volumeInputController);
    }
    
    #endregion
}
