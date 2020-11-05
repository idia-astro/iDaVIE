﻿using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;
using Valve.VR;
using Valve.VR.InteractionSystem;

namespace VolumeData
{
    public class VolumeCommandController : MonoBehaviour
    {
        public GameObject mainCanvassDesktop;

        public VolumeInputController VolumeInputController;
        public QuickMenuController QuickMenuController;
        public PaintMenuController PaintMenuController;

        public float VibrationDuration = 0.25f;
        public float VibrationFrequency = 100.0f;
        public float VibrationAmplitude = 1.0f;

        private List<VolumeDataSetRenderer> _dataSets;

        // Keywords
        public struct Keywords
        {
            public static readonly string EditThresholdMin = "edit min";
            public static readonly string EditThresholdMax = "edit max";
            public static readonly string EditZAxis = "edit zee axis";
            public static readonly string SaveZAxis = "save zee axis";
            public static readonly string ResetZAxis = "reset zee axis";
            public static readonly string EditZAxisAlt = "edit zed axis";
            public static readonly string SaveZAxisAlt = "save zed axis";
            public static readonly string ResetZAxisAlt = "reset zed axis";
            public static readonly string SaveThreshold = "save threshold";
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
            public static readonly string Teleport = "teleport";
            public static readonly string ResetCropSelection = "reset crop";
            public static readonly string MaskDisabled = "mask off";
            public static readonly string MaskEnabled = "mask on";
            public static readonly string MaskInverted = "mask invert";
            public static readonly string MaskIsolated = "mask isolate";
            public static readonly string ProjectionMaximum = "projection maximum";
            public static readonly string ProjectionAverage = "projection average";
            public static readonly string PaintMode = "paint mode";
            public static readonly string ExitPaintMode = "exit paint mode";
            public static readonly string BrushAdd = "brush add";
            public static readonly string BrushErase = "brush erase";
            public static readonly string ShowMaskOutline = "show mask outline";
            public static readonly string HideMaskOutline = "hide mask outline";
            public static readonly string TakePicture = "take picture";
            public static readonly string CursorInfo = "cursor info";
            public static readonly string LinearScale = "linear scale";            
            public static readonly string LogScale = "log scale";
            public static readonly string SqrtScale = "square root scale";

            public static readonly string[] All =
            {
                EditThresholdMin, EditThresholdMax, EditZAxis, EditZAxisAlt, SaveThreshold, ResetThreshold, ResetTransform, ColormapPlasma, ColormapRainbow, 
                ColormapMagma, ColormapInferno, ColormapViridis, ColormapCubeHelix, ResetZAxis, ResetZAxisAlt, SaveZAxis, SaveZAxisAlt, NextDataSet, 
                PreviousDataSet, CropSelection, Teleport, ResetCropSelection, MaskDisabled, MaskEnabled, MaskInverted, MaskIsolated, ProjectionMaximum, 
                ProjectionAverage, PaintMode, ExitPaintMode, BrushAdd, BrushErase, ShowMaskOutline, HideMaskOutline, TakePicture, CursorInfo, LinearScale,
                LogScale, SqrtScale
            };
        }
   
        private KeywordRecognizer _speechKeywordRecognizer;
        private VolumeInputController _volumeInputController;

        private VolumeDataSetRenderer _activeDataSet;


        void OnEnable()
        {
            _dataSets = new List<VolumeDataSetRenderer>();
            _dataSets.AddRange(GetComponentsInChildren<VolumeDataSetRenderer>(true));            
            _speechKeywordRecognizer = new KeywordRecognizer(Keywords.All, ConfidenceLevel.Low);
            _speechKeywordRecognizer.OnPhraseRecognized += OnPhraseRecognized;

            _speechKeywordRecognizer.Start();
            _volumeInputController = FindObjectOfType<VolumeInputController>();
        }

        // private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
        private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
        {
            _volumeInputController.VibrateController(_volumeInputController.PrimaryHand, VibrationDuration, VibrationFrequency, VibrationAmplitude);

            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("{0} ({1}){2}", args.text, args.confidence, Environment.NewLine);
            builder.AppendFormat("\tTimestamp: {0}{1}", args.phraseStartTime, Environment.NewLine);
            builder.AppendFormat("\tDuration: {0} seconds{1}", args.phraseDuration.TotalSeconds, Environment.NewLine);
            Debug.Log(builder.ToString());
            executeVoiceCommand(args.text);
        }

        private void executeVoiceCommand(string args)
        { 
            if (args == Keywords.EditThresholdMin)
            {
                startThresholdEditing(false);
            }
            else if (args == Keywords.EditThresholdMax)
            {
                startThresholdEditing(true);
            }
            else if (args == Keywords.EditZAxis || args == Keywords.EditZAxisAlt)
            {
                startZAxisEditing();
            }
            else if (args == Keywords.SaveZAxis || args == Keywords.SaveZAxisAlt)
            {
                endZAxisEditing();
            }
            else if (args == Keywords.ResetZAxis || args == Keywords.ResetZAxisAlt)
            {
                resetZAxis();
            }
            else if (args == Keywords.SaveThreshold)
            {
                endThresholdEditing();
            }
            else if (args == Keywords.ResetThreshold)
            {
                resetThreshold();
            }
            else if (args == Keywords.ResetTransform)
            {
                resetTransform();
            }
            else if (args == Keywords.NextDataSet)
            {
                stepDataSet(true);
            }
            else if (args == Keywords.PreviousDataSet)
            {
                stepDataSet(false);
            }
            else if (args == Keywords.ColormapPlasma)
            {
                setColorMap(ColorMapEnum.Plasma);
            }
            else if (args == Keywords.ColormapRainbow)
            {
                setColorMap(ColorMapEnum.Rainbow);
            }
            else if (args == Keywords.ColormapMagma)
            {
                setColorMap(ColorMapEnum.Magma);
            }
            else if (args == Keywords.ColormapInferno)
            {
                setColorMap(ColorMapEnum.Inferno);
            }
            else if (args == Keywords.ColormapViridis)
            {
                setColorMap(ColorMapEnum.Viridis);
            }
            else if (args == Keywords.ColormapCubeHelix)
            {
                setColorMap(ColorMapEnum.Cubehelix);
            }
            else if (args == Keywords.CropSelection)
            {
                cropDataSet();
            }
            else if (args == Keywords.ResetCropSelection)
            {
                resetCropDataSet();
            }
            else if (args == Keywords.Teleport)
            {
                teleportToSelection();
            }
            else if (args == Keywords.MaskDisabled)
            {
                setMask(MaskMode.Disabled);
            }
            else if (args == Keywords.MaskEnabled)
            {
                setMask(MaskMode.Enabled);
            }
            else if (args == Keywords.MaskInverted)
            {
                setMask(MaskMode.Inverted);
            }
            else if (args == Keywords.MaskIsolated)
            {
                setMask(MaskMode.Isolated);
            }
            else if (args == Keywords.ProjectionMaximum)
            {
                setProjection(ProjectionMode.MaximumIntensityProjection);
            }
            else if (args == Keywords.ProjectionAverage)
            {
                setProjection(ProjectionMode.AverageIntensityProjection);
            }
            else if (args == Keywords.PaintMode)
            {
                EnablePaintMode();
            }
            else if (args == Keywords.ExitPaintMode)
            {
                DisablePaintMode();
            }
            else if (args == Keywords.BrushAdd)
            {
                SetBrushAdditive();
            }
            else if (args == Keywords.BrushErase)
            {
                SetBrushSubtractive();
            }
            else if (args == Keywords.ShowMaskOutline)
            {
                ShowMaskOutline();
            }
            else if (args == Keywords.HideMaskOutline)
            {
                HideMaskOutline();
            }
            else if (args == Keywords.TakePicture)
            {
                TakePicture();
            }
            else if (args == Keywords.CursorInfo)
            {
                ToggleCursorInfo();
            }
            else if (args == Keywords.LogScale)
            {
                ChangeScalingType(ScalingType.Log);
            }
            else if (args == Keywords.LinearScale)
            {
                ChangeScalingType(ScalingType.Linear);
            }
            else if (args == Keywords.SqrtScale)
            {
                ChangeScalingType(ScalingType.Sqrt);
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
        }

        public void AddDataSet(VolumeDataSetRenderer setToAdd)
        {
            _dataSets.Add(setToAdd);
        }

        public void resetThreshold()
        {
            if (_activeDataSet)
            {
                _activeDataSet.ThresholdMin = _activeDataSet.InitialThresholdMin;
                mainCanvassDesktop.gameObject.transform.Find("RightPanel").gameObject.transform.Find("Panel_container").gameObject.transform.Find("RenderingPanel").gameObject.transform.Find("Rendering_container").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject.transform.Find("Settings").gameObject.transform.Find("Threshold_container").gameObject.transform.Find("Threshold_min")
                .gameObject.transform.Find("Slider").GetComponent<Slider>().value = _activeDataSet.ThresholdMin;

                _activeDataSet.ThresholdMax = _activeDataSet.InitialThresholdMax;
                mainCanvassDesktop.gameObject.transform.Find("RightPanel").gameObject.transform.Find("Panel_container").gameObject.transform.Find("RenderingPanel").gameObject.transform.Find("Rendering_container").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject.transform.Find("Settings").gameObject.transform.Find("Threshold_container").gameObject.transform.Find("Threshold_max")
                .gameObject.transform.Find("Slider").GetComponent<Slider>().value = _activeDataSet.ThresholdMax;
            }
        }

        public void resetZAxis()
        {
            if (_activeDataSet)
            {
                float zxRatio = _activeDataSet.InitialScale.z / _activeDataSet.InitialScale.x;
                _activeDataSet.transform.localScale = new Vector3(_activeDataSet.transform.localScale.x, _activeDataSet.transform.localScale.y, zxRatio * _activeDataSet.transform.localScale.x);
            }
        }

        public void startThresholdEditing(bool editingMax)
        {
            _volumeInputController.StartThresholdEditing(editingMax);
        }

        public void endThresholdEditing()
        {
            _volumeInputController.EndEditing();

            mainCanvassDesktop.gameObject.transform.Find("RightPanel").gameObject.transform.Find("Panel_container").gameObject.transform.Find("RenderingPanel").gameObject.transform.Find("Rendering_container").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject.transform.Find("Settings").gameObject.transform.Find("Threshold_container").gameObject.transform.Find("Threshold_min")
                .gameObject.transform.Find("Slider").GetComponent<Slider>().value = _activeDataSet.ThresholdMin;
            mainCanvassDesktop.gameObject.transform.Find("RightPanel").gameObject.transform.Find("Panel_container").gameObject.transform.Find("RenderingPanel").gameObject.transform.Find("Rendering_container").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject.transform.Find("Settings").gameObject.transform.Find("Threshold_container").gameObject.transform.Find("Threshold_max")
                .gameObject.transform.Find("Slider").GetComponent<Slider>().value = _activeDataSet.ThresholdMax;
            _volumeInputController.EndEditing();
        }

        public void startZAxisEditing()
        {
            _volumeInputController.StartZAxisEditing();
        }

        public void endZAxisEditing()
        {
            _volumeInputController.EndEditing();
        }
        
        public void resetTransform()
        {
            if (_activeDataSet)
            {
                _activeDataSet.transform.position = _activeDataSet.InitialPosition;
                _activeDataSet.transform.rotation = _activeDataSet.InitialRotation;
                _activeDataSet.transform.localScale = _activeDataSet.InitialScale;

                mainCanvassDesktop.gameObject.transform.Find("RightPanel").gameObject.transform.Find("Panel_container").gameObject.transform.Find("RenderingPanel")
                    .gameObject.transform.Find("Rendering_container").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject.transform.Find("Settings")
                    .gameObject.transform.Find("Ratio_container").gameObject.transform.Find("Ratio_Dropdown").GetComponent<TMP_Dropdown>().value = 0;
            }
        }

        public void setColorMap(ColorMapEnum colorMap)
        {
            if (_activeDataSet)
            {
                _activeDataSet.ColorMap = colorMap;
            }
        }               

        public void stepDataSet(bool forwards)
        {
            for (var i = 0; i < _dataSets.Count; i++)
            {
                var dataSet = _dataSets[i];
                Debug.Log(dataSet);
                if (dataSet == _activeDataSet)
                {
                    var newIndex = (i + _dataSets.Count + (forwards ? 1 : -1)) % _dataSets.Count;
                    _activeDataSet.gameObject.SetActive(false);
                    _dataSets[newIndex].gameObject.SetActive(true);
                    Debug.Log("Switching from dataset " + i + " to dataset " + newIndex);
                    break;
                }
            }
        }

        public void cropDataSet()
        {
            if (_activeDataSet)
            {
                _activeDataSet.CropToRegion();
            }
        }

        public void resetCropDataSet()
        {
            if (_activeDataSet)
            {
                _activeDataSet.ResetCrop();

                Debug.Log("cropped: "+_activeDataSet.IsCropped);
            }
        }

        public void teleportToSelection()
        {
            if (_activeDataSet)
            {
                _activeDataSet.TeleportToRegion();
                Debug.Log("cropped: " + _activeDataSet.IsCropped);
            }
        }

        public void setMask(MaskMode mode)
        {
            if (_activeDataSet)
            {
                _activeDataSet.MaskMode = mode;
            }
        }
        
        public void setProjection(ProjectionMode mode)
        {
            if (_activeDataSet)
            {
                _activeDataSet.ProjectionMode = mode;
            }
        }

        public void EnablePaintMode()
        {
            QuickMenuController.OpenPaintMenu();
        }

        public void DisablePaintMode()
        {
            PaintMenuController.ExitPaintMode();
        }

        public void SetBrushAdditive()
        {
            VolumeInputController.AdditiveBrush = true;
        }

        public void SetBrushSubtractive()
        {
            VolumeInputController.AdditiveBrush = false;
        }

        public void ShowMaskOutline()
        {
            foreach (var dataSet in _dataSets)
            {
                dataSet.DisplayMask = true;
            }
        }

        public void HideMaskOutline()
        {
            foreach (var dataSet in _dataSets)
            {
                dataSet.DisplayMask = false;
            }
        }

        public void TakePicture()
        {
            Debug.Log("Cannot take screenshot with voice yet...");
        }

        public void ToggleCursorInfo()
        {
            _volumeInputController.ToggleCursorInfoVisibility();
        }

        public void ChangeScalingType(ScalingType scalingType)
        {
            getFirstActiveDataSet().SetScalingType(scalingType);
        }

        public VolumeDataSetRenderer getFirstActiveDataSet()
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

        public void ExecuteVoiceCommandFromList(string cmd)
        {
            Debug.Log(cmd);
            executeVoiceCommand(cmd);
        }
    }
}