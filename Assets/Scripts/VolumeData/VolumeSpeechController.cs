using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Windows.Speech;
using Valve.VR.InteractionSystem;
using VolumeData;

namespace VolumeData
{
    public class VolumeSpeechController : MonoBehaviour
    {
        public Hand EditingHand;

        private enum SpeechControllerState
        {
            Idle,
            EditThresholdMin,
            EditThresholdMax
        }

        private VolumeDataSetRenderer[] _dataSets;

        private SpeechControllerState _state;

        // Keywords
        private string _keywordEditThresholdMin = "edit min";
        private string _keywordEditThresholdMax = "edit max";
        private string _keywordSaveThreshold = "save";
        private string _keywordResetThreshold = "reset threshold";
        private string _keywordResetTransform = "reset transform";
        private string _keywordColormapPlasma = "color map plasma";
        private string _keywordColormapRainbow = "color map rainbow";
        private string _keywordColormapMagma = "color map magma";
        private string _keywordColormapInferno = "color map inferno";
        private string _keywordColormapViridis = "color map viridis";
        private string _keywordColormapCubeHelix = "color map cube helix";
        private string _keywordNextDataSet = "next data set";
        private string _keywordPrevDataSet = "previous data set";

        private KeywordRecognizer _speechKeywordRecognizer;
        private float previousControllerHeight;

        private VolumeDataSetRenderer _activeDataSet;


        void Start()
        {
            _dataSets = GetComponentsInChildren<VolumeDataSetRenderer>(true);
            Debug.Log(_dataSets.Length);
            string[] keywords =
            {
                _keywordEditThresholdMin, _keywordEditThresholdMax, _keywordSaveThreshold,
                _keywordResetThreshold, _keywordResetTransform,
                _keywordColormapPlasma, _keywordColormapRainbow, _keywordColormapMagma, _keywordColormapInferno,
                _keywordColormapViridis, _keywordColormapCubeHelix,
                _keywordNextDataSet, _keywordPrevDataSet
            };
            _speechKeywordRecognizer = new KeywordRecognizer(keywords, ConfidenceLevel.Low);
            _speechKeywordRecognizer.OnPhraseRecognized += OnPhraseRecognized;

            _speechKeywordRecognizer.Start();
        }

        private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("{0} ({1}){2}", args.text, args.confidence, Environment.NewLine);
            builder.AppendFormat("\tTimestamp: {0}{1}", args.phraseStartTime, Environment.NewLine);
            builder.AppendFormat("\tDuration: {0} seconds{1}", args.phraseDuration.TotalSeconds, Environment.NewLine);
            Debug.Log(builder.ToString());
            if (args.text == _keywordEditThresholdMin)
            {
                _state = SpeechControllerState.EditThresholdMin;
                previousControllerHeight = EditingHand.transform.position.y;
            }
            else if (args.text == _keywordEditThresholdMax)
            {
                _state = SpeechControllerState.EditThresholdMax;
                previousControllerHeight = EditingHand.transform.position.y;
            }
            else if (args.text == _keywordSaveThreshold)
            {
                _state = SpeechControllerState.Idle;
            }
            else if (args.text == _keywordResetThreshold)
            {
                resetThreshold();
            }
            else if (args.text == _keywordResetTransform)
            {
                resetTransform();
            }
            else if (args.text == _keywordNextDataSet)
            {
                stepDataSet(true);
            }
            else if (args.text == _keywordPrevDataSet)
            {
                stepDataSet(false);
            }
            else if (args.text == _keywordColormapPlasma)
            {
                setColorMap(ColorMapEnum.Plasma);
            }
            else if (args.text == _keywordColormapRainbow)
            {
                setColorMap(ColorMapEnum.Rainbow);
            }
            else if (args.text == _keywordColormapMagma)
            {
                setColorMap(ColorMapEnum.Magma);
            }
            else if (args.text == _keywordColormapInferno)
            {
                setColorMap(ColorMapEnum.Inferno);
            }
            else if (args.text == _keywordColormapViridis)
            {
                setColorMap(ColorMapEnum.Viridis);
            }
            else if (args.text == _keywordColormapCubeHelix)
            {
                setColorMap(ColorMapEnum.Cubehelix);
            }
        }

        // Update is called once per frame
        void Update()
        {
            var firstActive = getFirstActiveDataSet();
            if (firstActive && _activeDataSet != firstActive)
            {
                _activeDataSet = firstActive;
            }

            if (_activeDataSet)
            {
                switch (_state)
                {
                    case SpeechControllerState.EditThresholdMin:
                        UpdateThreshold(false);
                        break;
                    case SpeechControllerState.EditThresholdMax:
                        UpdateThreshold(true);
                        break;
                    case SpeechControllerState.Idle:
                        break;
                }
            }
        }

        private void UpdateThreshold(bool editingMax)
        {
            if (EditingHand)
            {
                float controllerHeight = EditingHand.transform.position.y;
                float controlerDelta = controllerHeight - previousControllerHeight;
                previousControllerHeight = controllerHeight;
                if (_activeDataSet)
                {
                    if (editingMax)
                    {
                        var newValue = _activeDataSet.ThresholdMax + controlerDelta;
                        _activeDataSet.ThresholdMax = Mathf.Clamp(newValue, _activeDataSet.ThresholdMin, 1);
                    }
                    else
                    {
                        var newValue = _activeDataSet.ThresholdMin + controlerDelta;
                        _activeDataSet.ThresholdMin = Mathf.Clamp(newValue, 0, _activeDataSet.ThresholdMax);
                    }
                }
            }
        }

        private void resetThreshold()
        {
            if (_activeDataSet)
            {
                _activeDataSet.ThresholdMin = _activeDataSet.InitialThresholdMin;
                _activeDataSet.ThresholdMax = _activeDataSet.InitialThresholdMax;
            }
        }

        private void resetTransform()
        {
            if (_activeDataSet)
            {
                _activeDataSet.transform.position = _activeDataSet.InitialPosition;
                _activeDataSet.transform.rotation = _activeDataSet.InitialRotation;
                _activeDataSet.transform.localScale = _activeDataSet.InitialScale;
            }
        }

        private void setColorMap(ColorMapEnum colorMap)
        {
            if (_activeDataSet)
            {
                _activeDataSet.ColorMap = colorMap;
            }
        }

        private void stepDataSet(bool forwards)
        {
            for (var i = 0; i < _dataSets.Length; i++)
            {
                var dataSet = _dataSets[i];
                Debug.Log(dataSet);
                if (dataSet == _activeDataSet)
                {
                    var newIndex = (i + _dataSets.Length + (forwards ? 1 : -1)) % _dataSets.Length;
                    _activeDataSet.gameObject.SetActive(false);
                    _dataSets[newIndex].gameObject.SetActive(true);
                    Debug.Log("Switching from dataset " + i + " to dataset " + newIndex);
                    break;
                }
            }
        }

        private VolumeDataSetRenderer getFirstActiveDataSet()
        {
            foreach (var dataSet in _dataSets)
            {
                if (dataSet.isActiveAndEnabled)
                {
                    return dataSet;
                }
            }

            return null;
        }
    }
}