/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 *
 * This file is part of the iDaVIE project.
 *
 * iDaVIE is free software: you can redistribute it and/or modify it under the terms 
 * of the GNU Lesser General Public License (LGPL) as published by the Free Software 
 * Foundation, either version 3 of the License, or (at your option) any later version.
 *
 * iDaVIE is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR 
 * PURPOSE. See the GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License along with 
 * iDaVIE in the LICENSE file. If not, see <https://www.gnu.org/licenses/>.
 *
 * Additional information and disclaimers regarding liability and third-party 
 * components can be found in the DISCLAIMER and NOTICE files included with this project.
 *
 */
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
