using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VolumeData;

public class Colorbar : MonoBehaviour
{
    [Range(2, 10)]
    public int NumTicks = 4;
    public ColorMapEnum ColorMap = ColorMapEnum.Inferno;
    public ScalingType ScalingType = ScalingType.Linear;
    public float ScaleMin = 0.0f;
    public float ScaleMax = 1.0f;
    public GameObject TickPrefab;

    private TMP_Text[] _ticks;
    private Image _colorbarImage;
    private RectTransform _rectTransform;
    private Sprite[] _colormapSprites;
    
    private void OnEnable()
    {
        _colormapSprites = Resources.LoadAll<Sprite>("allmaps_sprites");
        if (_colormapSprites?.Length != ColorMapUtils.NumColorMaps)
        {
            _colormapSprites = null;
        }
        
         _colorbarImage = GetComponentInChildren<Image>();
        _rectTransform = GetComponent<RectTransform>();
        BuildTicks();
        ApplyColormap();
    }
    
    // Update is called once per frame
    void Update()
    {
        if (_ticks?.Length != NumTicks)
        {
            BuildTicks();
        }

        UpdateTickText();
        ApplyColormap();
    }

    private void UpdateTickText()
    {
        for (int i = 0; i < NumTicks; i++)
        {
            var divider = (ScaleMax - ScaleMin) / (NumTicks - 1);
            var tickVal = ScaleMin + divider * i;
            _ticks[i].text = tickVal.ToString(CultureInfo.InvariantCulture.NumberFormat);
        }
    }

    private void BuildTicks()
    {
        if (_ticks != null)
        {
            foreach (var tick in _ticks)
            {
                Destroy(tick);
            }    
        }

        var height = _rectTransform.rect.height;
        var delta = height / (NumTicks - 1);
        _ticks = new TMP_Text[NumTicks];
        for (int i = 0; i < NumTicks; i++)
        {
            var tickObject = Instantiate(TickPrefab, transform);
            tickObject.transform.localPosition = Vector3.zero;
            tickObject.GetComponent<RectTransform>().localPosition = new Vector3(7.5f, -height/2.0f + i * delta, 0);
            tickObject.name = $"Tick{i}";
            _ticks[i] = tickObject.GetComponentInChildren<TMP_Text>();
        }
    }

    private void ApplyColormap()
    {
        int index = ColorMap.GetHashCode();
        if (index >= 0 && index < _colormapSprites?.Length)
        {
            _colorbarImage.sprite = _colormapSprites[index];
        }
    }
}
