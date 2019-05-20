using System;
using System.Text;
using UnityEngine;
using UnityEngine.Windows.Speech;
using Valve.VR;
using Valve.VR.InteractionSystem;

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
        private struct Keywords
        {
            public static readonly string EditThresholdMin = "edit min";
            public static readonly string EditThresholdMax = "edit max";
            public static readonly string SaveThreshold = "save";
            public static readonly string ResetThreshold = "reset threshold";
            public static readonly string ResetTransform = "reset transform";
            public static readonly string ColormapPlasma = "color map plasma";
            public static readonly string ColormapRainbow = "color map rainbow";
            public static readonly string ColormapMagma = "color map magma";
            public static readonly string ColormapInferno = "color map inferno";
            public static readonly string ColormapViridis = "color map viridis";
            public static readonly string ColormapCubeHelix = "color map cube helix";
            public static readonly string NextDataSet = "next data set";
            public static readonly string PreviousDataSet = "previous data set";
            public static readonly string CropSelection = "crop selection";
            public static readonly string ResetCropSelection = "reset crop";
            public static readonly string MaskDisabled = "mask off";
            public static readonly string MaskEnabled = "mask on";
            public static readonly string MaskInverted = "mask invert";
            public static readonly string MaskIsolated = "mask isolate";
            
            public static readonly string[] All = { EditThresholdMin, EditThresholdMax, SaveThreshold, ResetThreshold, ResetTransform, 
                ColormapPlasma, ColormapRainbow, ColormapMagma, ColormapInferno, ColormapViridis, ColormapCubeHelix,
                NextDataSet, PreviousDataSet, CropSelection, ResetCropSelection, MaskDisabled, MaskEnabled, MaskInverted, MaskIsolated
            };
        }
   
        private KeywordRecognizer _speechKeywordRecognizer;
        private float previousControllerHeight;

        private VolumeDataSetRenderer _activeDataSet;


        void Start()
        {
            _dataSets = GetComponentsInChildren<VolumeDataSetRenderer>(true);            
            _speechKeywordRecognizer = new KeywordRecognizer(Keywords.All, ConfidenceLevel.Medium);
            _speechKeywordRecognizer.OnPhraseRecognized += OnPhraseRecognized;

            _speechKeywordRecognizer.Start();
            EditingHand.uiInteractAction.AddOnStateDownListener(OnUiInteractDown, SteamVR_Input_Sources.Any);
        }

        private void OnUiInteractDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
        {
            _state = SpeechControllerState.Idle;
        }

        private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("{0} ({1}){2}", args.text, args.confidence, Environment.NewLine);
            builder.AppendFormat("\tTimestamp: {0}{1}", args.phraseStartTime, Environment.NewLine);
            builder.AppendFormat("\tDuration: {0} seconds{1}", args.phraseDuration.TotalSeconds, Environment.NewLine);
            Debug.Log(builder.ToString());
            if (args.text == Keywords.EditThresholdMin)
            {
                _state = SpeechControllerState.EditThresholdMin;
                previousControllerHeight = EditingHand.transform.position.y;
            }
            else if (args.text == Keywords.EditThresholdMax)
            {
                _state = SpeechControllerState.EditThresholdMax;
                previousControllerHeight = EditingHand.transform.position.y;
            }
            else if (args.text == Keywords.SaveThreshold)
            {
                _state = SpeechControllerState.Idle;
            }
            else if (args.text == Keywords.ResetThreshold)
            {
                resetThreshold();
            }
            else if (args.text == Keywords.ResetTransform)
            {
                resetTransform();
            }
            else if (args.text == Keywords.NextDataSet)
            {
                stepDataSet(true);
            }
            else if (args.text == Keywords.PreviousDataSet)
            {
                stepDataSet(false);
            }
            else if (args.text == Keywords.ColormapPlasma)
            {
                setColorMap(ColorMapEnum.Plasma);
            }
            else if (args.text == Keywords.ColormapRainbow)
            {
                setColorMap(ColorMapEnum.Rainbow);
            }
            else if (args.text == Keywords.ColormapMagma)
            {
                setColorMap(ColorMapEnum.Magma);
            }
            else if (args.text == Keywords.ColormapInferno)
            {
                setColorMap(ColorMapEnum.Inferno);
            }
            else if (args.text == Keywords.ColormapViridis)
            {
                setColorMap(ColorMapEnum.Viridis);
            }
            else if (args.text == Keywords.ColormapCubeHelix)
            {
                setColorMap(ColorMapEnum.Cubehelix);
            }
            else if (args.text == Keywords.CropSelection)
            {
                cropDataSet();
            }
            else if (args.text == Keywords.ResetCropSelection)
            {
                resetCropDataSet();
            }
            else if (args.text == Keywords.MaskDisabled)
            {
                setMask(MaskMode.Disabled);
            }
            else if (args.text == Keywords.MaskEnabled)
            {
                setMask(MaskMode.Enabled);
            }
            else if (args.text == Keywords.MaskInverted)
            {
                setMask(MaskMode.Inverted);
            }
            else if (args.text == Keywords.MaskIsolated)
            {
                setMask(MaskMode.Isolated);
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

        private void cropDataSet()
        {
            if (_activeDataSet)
            {
                _activeDataSet.CropToRegion();
            }
        }

        private void resetCropDataSet()
        {
            if (_activeDataSet)
            {
                _activeDataSet.ResetCrop();
            }
        }

        private void setMask(MaskMode mode)
        {
            if (_activeDataSet)
            {
                _activeDataSet.MaskMode = mode;
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