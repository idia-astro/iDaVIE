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
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using VolumeData.Voice;

namespace VolumeData
{
    public class VolumeCommandController : MonoBehaviour
    {
        public GameObject mainCanvassDesktop;
        public QuickMenuController QuickMenuController;
        public PaintMenuController PaintMenuController;
        public MomentMapMenuController momentMapMenuController;
        public FeatureMenuController featureMenuController;

        public bool IsVoiceRecognitionActive => _voiceRecogniser != null && _voiceRecogniser.IsRunning;

        public VolumeInputController VolumeInput => _volumeInputController;
        public VideoRecordMenuController VideoRecordMenu => _videoRecordMenuController;

        private List<VolumeDataSetRenderer> _dataSets;
        private IVoiceRecogniser _voiceRecogniser;
        private IReadOnlyDictionary<string, IVoiceCommand> _voiceCommands;
        private IVoiceCommandContext _voiceCommandContext;
        private VolumeInputController _volumeInputController;
        private VideoRecordMenuController _videoRecordMenuController;
        private VolumeDataSetRenderer _activeDataSet;
        private Config _config;

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
            public static readonly string ColormapTurbo = "color map turbo";
            public static readonly string CropSelection = "crop selection";
            public static readonly string Teleport = "teleport";
            public static readonly string ResetCropSelection = "reset crop";
            public static readonly string MaskDisabled = "mask off";
            public static readonly string MaskEnabled = "mask on";
            public static readonly string MaskInverted = "mask invert";
            public static readonly string MaskIsolated = "mask isolate";
            public static readonly string ProjectionMaximum = "projection maximum";
            public static readonly string ProjectionAverage = "projection average";
            public static readonly string SamplingModeMaximum = "sampling mode maximum";
            public static readonly string SamplingModeAverage = "sampling mode average";
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
            public static readonly string AddNewSource = "add new source";
            public static readonly string SetSourceId = "set source ID";
            public static readonly string AddToList = "add to list";
            public static readonly string Undo = "undo";
            public static readonly string Redo = "redo";
            public static readonly string SaveSubCube = "export sub cube";
            public static readonly string NextSourceList = "next source list";
            public static readonly string PreviousSourceList = "previous source list";
            public static readonly string ExportVideoScript = "export video script";
            public static readonly string EnterVideoMode = "video mode";
            public static readonly string RecordVideoPosition = "record position";

            public static readonly string[] All =
            {
                EditThresholdMin, EditThresholdMax, EditZAxis, EditZAxisAlt, SaveThreshold, ResetThreshold, ResetTransform, ColormapPlasma, ColormapRainbow,
                ColormapMagma, ColormapInferno, ColormapViridis, ColormapCubeHelix, ColormapTurbo, ResetZAxis, ResetZAxisAlt, SaveZAxis, SaveZAxisAlt,
                CropSelection, Teleport, ResetCropSelection, MaskDisabled, MaskEnabled, MaskInverted, MaskIsolated, ProjectionMaximum,
                ProjectionAverage, SamplingModeAverage, SamplingModeMaximum, PaintMode, ExitPaintMode, BrushAdd, BrushErase, ShowMaskOutline,
                HideMaskOutline, TakePicture, CursorInfo, LinearScale,
                LogScale, SqrtScale, AddNewSource, SetSourceId, Undo, Redo, SaveSubCube,
                NextSourceList, PreviousSourceList,
                ExportVideoScript, EnterVideoMode, RecordVideoPosition
            };
        }

        void OnEnable()
        {
            _config = Config.Instance;
            _dataSets = new List<VolumeDataSetRenderer>();
            _dataSets.AddRange(GetComponentsInChildren<VolumeDataSetRenderer>(true));

            _voiceCommands = VoiceCommandRegistry.Build(this);
            _voiceCommandContext = new VoiceCommandContext(this, _dataSets);

            var recogniser = new WindowsVoiceRecogniser(Keywords.All, _config.voiceCommandConfidenceLevel);
            recogniser.PhraseRecognized += OnPhraseRecognized;
            _voiceRecogniser = recogniser;

            _volumeInputController = FindObjectOfType<VolumeInputController>();
            _videoRecordMenuController = FindObjectOfType<VideoRecordMenuController>(true);

            if (!_config.usePushToTalk)
            {
                _voiceRecogniser.Start();
            }
            else
            {
                _volumeInputController.PushToTalkButtonPressed += OnPushToTalkPressed;
                _volumeInputController.PushToTalkButtonReleased += OnPushToTalkReleased;
            }
        }

        private void OnDisable()
        {
            if (_voiceRecogniser is WindowsVoiceRecogniser windowsRecogniser)
            {
                windowsRecogniser.PhraseRecognized -= OnPhraseRecognized;
                windowsRecogniser.Dispose();
            }

            _voiceRecogniser = null;
        }

        private void OnDestroy()
        {
            if (_config != null && _config.usePushToTalk && _volumeInputController != null)
            {
                _volumeInputController.PushToTalkButtonPressed -= OnPushToTalkPressed;
                _volumeInputController.PushToTalkButtonReleased -= OnPushToTalkReleased;
            }
        }

        public void OnPushToTalkPressed()
        {
            _voiceRecogniser?.Start();
        }

        public void OnPushToTalkReleased()
        {
            _voiceRecogniser?.Stop();
        }

        private void OnPhraseRecognized(object sender, VoicePhraseRecognizedEventArgs args)
        {
            _volumeInputController.VibrateController(_volumeInputController.PrimaryHand);

            var builder = new StringBuilder();
            builder.AppendFormat("{0} ({1}){2}", args.Text, args.Confidence, Environment.NewLine);
            builder.AppendFormat("\tTimestamp: {0}{1}", args.PhraseStartTime, Environment.NewLine);
            builder.AppendFormat("\tDuration: {0} seconds{1}", args.PhraseDuration.TotalSeconds, Environment.NewLine);
            Debug.Log(builder.ToString());
            ExecuteVoiceCommand(args.Text);
        }

        private void ExecuteVoiceCommand(string phrase)
        {
            VoiceCommandRegistry.TryExecute(phrase, _voiceCommands, _voiceCommandContext);
        }

        void Update()
        {
            RefreshActiveDataSetInternal();
        }

        internal VolumeDataSetRenderer GetActiveDataSetInternal()
        {
            return _activeDataSet;
        }

        internal void RefreshActiveDataSetInternal()
        {
            var firstActive = getFirstActiveDataSet();
            if (firstActive && _activeDataSet != firstActive)
            {
                _activeDataSet = firstActive;
            }
        }

        public void PreviousSourceList()
        {
            featureMenuController.DisplayPreviousSet();
        }

        public void NextSourceList()
        {
            featureMenuController.DisplayNextSet();
        }

        public void AddDataSet(VolumeDataSetRenderer setToAdd)
        {
            _dataSets.Add(setToAdd);
        }

        public void RemoveDataSet(VolumeDataSetRenderer setToRemove)
        {
            setToRemove.transform.parent = null;
            _dataSets.Remove(setToRemove);
            Destroy(setToRemove?.gameObject);
            _volumeInputController.UpdateDataSets();
        }

        public void resetThreshold()
        {
            if (!_activeDataSet)
            {
                return;
            }

            _activeDataSet.ThresholdMin = _activeDataSet.InitialThresholdMin;
            _activeDataSet.ThresholdMax = _activeDataSet.InitialThresholdMax;
            VoiceDesktopUiSync.SyncThresholdSliders(mainCanvassDesktop, _activeDataSet);
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
            VoiceDesktopUiSync.SyncThresholdSliders(mainCanvassDesktop, _activeDataSet);
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
            if (!_activeDataSet)
            {
                return;
            }

            _activeDataSet.transform.position = _activeDataSet.InitialPosition;
            _activeDataSet.transform.rotation = _activeDataSet.InitialRotation;
            _activeDataSet.transform.localScale = _activeDataSet.InitialScale;
            VoiceDesktopUiSync.ResetRatioDropdown(mainCanvassDesktop);
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
                _activeDataSet.CropToFeature();
            }
        }

        public void resetCropDataSet()
        {
            if (_activeDataSet)
            {
                _activeDataSet.ResetCrop();
                Debug.Log("cropped: " + _activeDataSet.IsCropped);
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
            if (_activeDataSet == null)
            {
                return;
            }

            if (_activeDataSet.Mask == null)
            {
                _voiceCommandContext.ShowMissingMaskError();
                return;
            }

            _activeDataSet.MaskMode = mode;
        }

        public void setProjection(ProjectionMode mode)
        {
            if (_activeDataSet)
            {
                _activeDataSet.ProjectionMode = mode;
            }

            ToastNotification.ShowInfo($"Accumulation set to {mode.ToString()}");
        }

        public void SetSamplingMode(bool maxMode)
        {
            var config = Config.Instance;
            if (config.maxModeDownsampling != maxMode)
            {
                config.maxModeDownsampling = maxMode;
                _activeDataSet?.RegenerateCubes();
            }

            var modeString = maxMode ? "max" : "average";
            ToastNotification.ShowInfo($"Downsampling mode set to {modeString}");
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
            _volumeInputController.SetBrushAdditive();
        }

        public void SetBrushSubtractive()
        {
            _volumeInputController.SetBrushSubtractive();
        }

        public void ShowMaskOutline()
        {
            if (_activeDataSet == null || _activeDataSet.Mask == null)
            {
                _voiceCommandContext.ShowMissingMaskError();
                return;
            }

            foreach (var dataSet in _dataSets)
            {
                dataSet.DisplayMask = true;
            }
        }

        public void HideMaskOutline()
        {
            if (_activeDataSet == null || _activeDataSet.Mask == null)
            {
                _voiceCommandContext.ShowMissingMaskError();
                return;
            }

            foreach (var dataSet in _dataSets)
            {
                dataSet.DisplayMask = false;
            }
        }

        public void TakePicture()
        {
            _volumeInputController.TakePicture();
        }

        public void ToggleCursorInfo()
        {
            _volumeInputController.ToggleCursorInfoVisibility();
        }

        public void ChangeScalingType(ScalingType scalingType)
        {
            getFirstActiveDataSet().ScalingType = scalingType;
            ToastNotification.ShowInfo($"Scaling type set to {scalingType.ToString()}");
        }

        public void AddNewSource()
        {
            _volumeInputController.AddNewSource();
        }

        public void SetMaskValue()
        {
            if (_activeDataSet == null || _activeDataSet.Mask == null)
            {
                _voiceCommandContext.ShowMissingMaskError();
                return;
            }

            _volumeInputController.InteractionStateMachine.Fire(VolumeInputController.InteractionEvents.StartEditSource);
        }

        public void AddSelectionToList()
        {
            _activeDataSet?.AddSelectionToList();
        }

        public void Undo()
        {
            _activeDataSet?.Mask?.UndoBrushStroke();
        }

        public void Redo()
        {
            _activeDataSet?.Mask?.RedoBrushStroke();
        }

        public void SaveSubCube()
        {
            _volumeInputController.SaveSubCube();
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
            ExecuteVoiceCommand(cmd);
        }
    }
}
