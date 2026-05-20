using System.Collections;
using System.Collections.Generic;
using PolyAndCode.UI;
using UnityEngine;

/// <summary>
/// This class manages the data of the RecyclableScrollView that displays the list of video locations.
/// </summary>
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

    /// <summary>
    /// This function is used to set some of the references, and initialise the data source instance.
    /// </summary>
    /// <param name="rect">The RecyclableScrollView that this data source is responsible for.</param>
    /// <param name="VolInController">The VolumeInputController instance that manages user input, used to pass through for functionality.</param>
    public void Initialize(RecyclableScrollRect rect, VolumeInputController VolInController)
    {
        _recyclableScrollRect = rect;
        _volumeInputController = VolInController;
        Awake();
    }

    /// <summary>
    /// Copies the list of locations from the video recorder to this data source. Since all members of the videoRecLocation struct are value types, it is implicitly a deep copy, and can therefore not be left as a reference. It is thus called whenever the videorecorder data changes.
    /// </summary>
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
