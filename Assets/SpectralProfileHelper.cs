using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using VolumeData;

/// <summary>
/// Class to help with the creation of spectral profile images.
/// </summary>
public class SpectralProfileHelper : MonoBehaviour
{
    
    [SerializeField] private SpectralProfileMenuController SpectralProfileMenuController;
    [SerializeField] private GameObject VolumeDataSetManager;
    private VolumeDataSetRenderer activeDataSet;
    
    private bool _isInitialized = false;


    // Update is called once per frame
    void Update()
    {
        // Delayed initialization because the active dataset is not set until later
        if (!_isInitialized)
        {
            if (VolumeDataSetManager != null)
            {
                activeDataSet = getFirstActiveDataSet();
                if (activeDataSet != null)
                {
                    activeDataSet.FeatureSetManagerPrefab.MaskFeatureSelected += OnMaskedSourceSelected;
                    _isInitialized = true;
                }
            }
        }
    }
    
    void OnDestroy()
    {
        if (activeDataSet != null)
        {
            activeDataSet.FeatureSetManagerPrefab.MaskFeatureSelected -= OnMaskedSourceSelected;
        }
    }

    public VolumeDataSetRenderer getFirstActiveDataSet()
    {
        VolumeDataSetRenderer[] dataSets = VolumeDataSetManager.GetComponentsInChildren<VolumeDataSetRenderer>(true);
        foreach (VolumeDataSetRenderer dataSet in dataSets)
        {
            if (dataSet.gameObject.activeSelf)
            {
                return dataSet;
            }
        }

        return null;
    }

    /// <summary>
    /// Function to update the spectral profile image when the cropped region is changed.
    /// TODO: Implement later
    /// </summary>
    public void OnCroppedRegionChanged()
    {
        DataAnalysis.SourceInfo region = new DataAnalysis.SourceInfo();
        region.minX = activeDataSet.CurrentCropMin.x;
        region.minY = activeDataSet.CurrentCropMin.y;
        region.minZ = activeDataSet.CurrentCropMin.z;
        region.maxX = activeDataSet.CurrentCropMax.x;
        region.maxY = activeDataSet.CurrentCropMax.y;
        region.maxZ = activeDataSet.CurrentCropMax.z;
        region.maskVal = -1;
        DataAnalysis.SourceStats sourceStats = new DataAnalysis.SourceStats();
        DataAnalysis.GetSourceStats(activeDataSet.Data.FitsData, activeDataSet.Mask.FitsData, activeDataSet.GetCubeDimensions().x,  
            activeDataSet.GetCubeDimensions().y, activeDataSet.GetCubeDimensions().z , region, ref sourceStats, activeDataSet.Data.AstFrameSet);
        CreateSpectralProfileImg(sourceStats);
        DataAnalysis.FreeDataAnalysisMemory(sourceStats.spectralProfilePtr);
    }

    /// <summary>
    /// Function to update the spectral profile image when a masked source is selected.
    /// </summary>
    public void OnMaskedSourceSelected()
    {
        var sourceStats =
            activeDataSet.Mask.SourceStatsDict.ElementAt(activeDataSet.FeatureSetManagerPrefab.SelectedFeature.Index).Value;
        CreateSpectralProfileImg(sourceStats);

    }
    
    /// <summary>
    /// Function to create a spectral profile image from the source stats.
    /// </summary>
    /// <param name="sourceStats"></param>
    public unsafe void CreateSpectralProfileImg(DataAnalysis.SourceStats sourceStats)
    {
        double[] spectralProfile = new double[sourceStats.spectralProfileSize];
        //System.Runtime.InteropServices.Marshal.Copy(sourceStats.spectralProfilePtr, spectralProfile, 0, sourceStats.spectralProfileSize);
        var spectralProfilePtr = (double*)sourceStats.spectralProfilePtr;
        // Create a line plot of the spectral profile using oxyplot
        OxyPlot.PlotModel model = new OxyPlot.PlotModel { Title = "Spectral Profile" };
        OxyPlot.Series.LineSeries lineSeries = new OxyPlot.Series.LineSeries();
        var zUnit = activeDataSet.HasWCS ? activeDataSet.Data.GetAxisUnit(3) : "z";
        var valueUnit = activeDataSet.HasWCS ? activeDataSet.Data.PixelUnit : "units";
        var xAxis = new OxyPlot.Axes.LinearAxis { Position = OxyPlot.Axes.AxisPosition.Bottom, Title = zUnit };
        var yAxis = new OxyPlot.Axes.LinearAxis { Position = OxyPlot.Axes.AxisPosition.Left, Title = valueUnit };
        for (int i = 0; i < spectralProfile.Length; i++)
        {
            AstTool.Transform3D(activeDataSet.AstFrame, 0, 0, i + sourceStats.minZ, 1 , out _, out _, out double spectralValue);
            //lineSeries.Points.Add(new OxyPlot.DataPoint(i, spectralProfile[i]));
            if (spectralProfilePtr != null) lineSeries.Points.Add(new OxyPlot.DataPoint(spectralValue, spectralProfilePtr[i]));
        }
        model.Axes.Add(xAxis);
        model.Axes.Add(yAxis);
        model.Series.Add(lineSeries);
        model.InvalidatePlot(true);
        int width = 600;
        int height = 300;
        var stream = new MemoryStream();
        var exporter = new OxyPlot.WindowsForms.PngExporter { Width = width, Height = height };
        exporter.Export(model, stream);
        Texture2D tex = new Texture2D(width, height);
        tex.LoadImage(stream.ToArray());
        var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(tex.width / 2, tex.height / 2));
        
        // Updates UI elements
        SpectralProfileMenuController.UpdateUI(sprite);
        SpectralProfileMenuController.SelectedSourceStats = sourceStats;
    }
        
    
}
