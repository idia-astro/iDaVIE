using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Series;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HistogramHelper : MonoBehaviour
{
    public HistogramMenuController histogramMenu;
    public CanvassDesktop canvassDesktop;

    public float CurrentMin { get; private set; }
    public float CurrentMax { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CreateHistogramImg(int[] h, float binWidth, float min, float max, float mean, float stanDev, float sigma = 1f)
    {
        CurrentMin = min;
        CurrentMax = max;

        var model = new PlotModel { Title = "Histogram " };

        var s1 = new HistogramSeries { StrokeThickness = 1 };
        var s2 = new HistogramSeries { StrokeThickness = 1, StrokeColor = OxyColors.Green };

        int c = 0;
        for (float i = min; i <= max && c < h.Length; i += binWidth)
        {
            s1.Items.Add(new HistogramItem(i, i + binWidth, h[c], 1));

            if (Mathf.Abs(i - mean) <= (stanDev * sigma))
            {
                s2.Items.Add(new HistogramItem(i, i + binWidth, h[c], 1));
            }

            c++;
        }

        Debug.Log("c: " + c + " h:" + h.Length);

        model.Series.Add(s1);
        model.Series.Add(s2);

        var min_annotation = new LineAnnotation();
        min_annotation.Color = OxyColors.Blue;
        min_annotation.X = min;
        min_annotation.LineStyle = LineStyle.Solid;
        min_annotation.Type = LineAnnotationType.Vertical;
        model.Annotations.Add(min_annotation);
        var max_annotation = new LineAnnotation();
        max_annotation.Color = OxyColors.Red;
        max_annotation.X = max;
        max_annotation.LineStyle = LineStyle.Solid;
        max_annotation.Type = LineAnnotationType.Vertical;
        model.Annotations.Add(max_annotation);

        int width = 600;
        int height = 300;
        var stream = new MemoryStream();
        var exporter = new OxyPlot.WindowsForms.PngExporter { Width = width, Height = height };
        exporter.Export(model, stream);
        Texture2D tex = new Texture2D(width, height);
        tex.LoadImage(stream.ToArray());
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(tex.width / 2, tex.height / 2));

        // Updates UI elements
        histogramMenu.UpdateUI(min, max, sprite);
        canvassDesktop.UpdateUI(min, max, sprite);
    }

}
