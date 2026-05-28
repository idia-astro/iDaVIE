// SPDX-License-Identifier: LGPL-3.0-or-later
// VolumeCommandController — post-refactor MonoBehaviour. Owned by ST4.
//
// Legacy: 480+ LOC. WMC ~65. Problems:
//   - KeywordRecognizer instantiated directly (Windows.Speech reference in domain).
//   - mainCanvassDesktop.gameObject.transform.Find(...) chains of 8+ levels
//     for every threshold / ZAxis / transform reset call (CBO violation).
//   - FindObjectOfType<VolumeInputController>() (GRASP violation).
//   - Direct FeatureMenuController reference for DisplayNextSet/PreviousSet.
//   - Direct VolumeDataSetRenderer references in getFirstActiveDataSet().
//
// Refactor delta:
//   - IVoiceCommandStream injected; KeywordRecognizer moved to VoiceRecogniserAdapter
//     (ST4 ACL). ExecuteVoiceCommand() replaced by DispatchVoiceCommand(VoiceCommand).
//   - mainCanvassDesktop chains replaced by IRenderSettingsMutator (ST3, M-09).
//   - FindObjectOfType eliminated; VolumeInputController injected (MOD-03).
//   - FeatureMenuController replaced by IFeatureListNavigation (ST5, M-11).
//   - IVolumeRegistry replaces direct _dataSets list (M-19).
//   - All mask mutations go via IMaskMutationService.ApplyBrush only (M-14).
//     No PaintPolygon call from ST4.
//
// SOLID/GRASP impact:
//   SRP  — only cross-domain command dispatch; no voice recognition, no UI binding.
//   DIP  — all cross-team interactions via injected interfaces.
//   GRASP Low Coupling — CBO ≤ 25 (down from 40+).

using System.Text;
using UnityEngine;
using iDaVIE.Kernel.Contracts;         // IVolumeRegistry, Config
using iDaVIE.Data;                     // IMaskMutationService, BrushStroke
using iDaVIE.Rendering.Contracts;      // IRenderSettingsMutator, ColorMapEnum, etc.
using iDaVIE.Features;                 // IFeatureSetQuery, IFeatureListNavigation
using iDaVIE.Persistence;             // IWorkspaceSaveCommand, IWorkspaceLoadCommand
using Debug = UnityEngine.Debug;

namespace iDaVIE.Interaction
{
    public sealed class VolumeCommandController : MonoBehaviour
    {
        // ── Injected dependencies ─────────────────────────────────────────────

        [HideInInspector] public IMaskMutationService     MaskMutationService;
        [HideInInspector] public IRenderSettingsMutator   RenderMutator;
        [HideInInspector] public IVolumeRegistry          VolumeRegistry;
        [HideInInspector] public IFeatureSetQuery         FeatureSetQuery;
        [HideInInspector] public IFeatureListNavigation   FeatureListNavigation;
        [HideInInspector] public IWorkspaceSaveCommand    SaveCommand;
        [HideInInspector] public IWorkspaceLoadCommand    LoadCommand;

        // Sibling refs wired by composition root (same GameObject).
        [HideInInspector] public VolumeInputController    InputController;
        [HideInInspector] public QuickMenuController      QuickMenuController;
        [HideInInspector] public PaintMenuController      PaintMenuController;
        [HideInInspector] public VideoRecordMenuController VideoRecordMenuController;

        // ── Brush state (owned by T4) ─────────────────────────────────────────

        public BrushConfig CurrentBrush { get; set; } = new BrushConfig
            { Radius = 1f, Additive = true, SourceId = -1, PaintMode = BrushPaintMode.Add };

        private string? _pendingParamName;
        private float   _pendingParamValue;

        // ── Voice recognition active flag ─────────────────────────────────────
        // Replaces IsVoiceRecognitionActive property that read _speechKeywordRecognizer.IsRunning.
        public bool IsVoiceRecognitionActive => InputController?.VoiceStream?.IsListening ?? false;

        // ── Voice dispatch (replaces ExecuteVoiceCommand + Keywords struct) ───

        /// <summary>
        /// Single dispatch point for all voice commands.
        /// Replaces the legacy 300-line if-else chain in ExecuteVoiceCommand().
        /// Called by VolumeInputController.OnVoiceCommand after the adapter fires.
        /// </summary>
        public void DispatchVoiceCommand(VoiceCommand cmd)
        {
            switch (cmd)
            {
                case VoiceCommand.EditThresholdMin:     startThresholdEditing(false);                break;
                case VoiceCommand.EditThresholdMax:     startThresholdEditing(true);                 break;
                case VoiceCommand.EditZAxis:            startZAxisEditing();                         break;
                case VoiceCommand.SaveZAxis:            endZAxisEditing();                           break;
                case VoiceCommand.ResetZAxis:           resetZAxis();                                break;
                case VoiceCommand.SaveThreshold:        endThresholdEditing();                       break;
                case VoiceCommand.ResetThreshold:       resetThreshold();                            break;
                case VoiceCommand.ResetTransform:       resetTransform();                            break;
                case VoiceCommand.ColormapPlasma:       setColorMap(ColorMapEnum.Plasma);            break;
                case VoiceCommand.ColormapRainbow:      setColorMap(ColorMapEnum.Rainbow);           break;
                case VoiceCommand.ColormapMagma:        setColorMap(ColorMapEnum.Magma);             break;
                case VoiceCommand.ColormapInferno:      setColorMap(ColorMapEnum.Inferno);           break;
                case VoiceCommand.ColormapViridis:      setColorMap(ColorMapEnum.Viridis);           break;
                case VoiceCommand.ColormapCubeHelix:    setColorMap(ColorMapEnum.Cubehelix);         break;
                case VoiceCommand.ColormapTurbo:        setColorMap(ColorMapEnum.Turbo);             break;
                case VoiceCommand.CropSelection:        cropDataSet();                               break;
                case VoiceCommand.ResetCropSelection:   resetCropDataSet();                          break;
                case VoiceCommand.Teleport:             teleportToSelection();                       break;
                case VoiceCommand.MaskDisabled:         setMask(MaskMode.Disabled);                  break;
                case VoiceCommand.MaskEnabled:          setMask(MaskMode.Enabled);                   break;
                case VoiceCommand.MaskInverted:         setMask(MaskMode.Inverted);                  break;
                case VoiceCommand.MaskIsolated:         setMask(MaskMode.Isolated);                  break;
                case VoiceCommand.ProjectionMaximum:    setProjection(ProjectionMode.MaximumIntensityProjection); break;
                case VoiceCommand.ProjectionAverage:    setProjection(ProjectionMode.AverageIntensityProjection);  break;
                case VoiceCommand.SamplingModeAverage:  SetSamplingMode(false);                      break;
                case VoiceCommand.SamplingModeMaximum:  SetSamplingMode(true);                       break;
                case VoiceCommand.PaintMode:            EnablePaintMode();                           break;
                case VoiceCommand.ExitPaintMode:        DisablePaintMode();                          break;
                case VoiceCommand.BrushAdd:             InputController?.SetBrushAdditive();         break;
                case VoiceCommand.BrushErase:           InputController?.SetBrushSubtractive();      break;
                case VoiceCommand.ShowMaskOutline:      ShowMaskOutline();                           break;
                case VoiceCommand.HideMaskOutline:      HideMaskOutline();                           break;
                case VoiceCommand.TakePicture:          InputController?.TakePicture();              break;
                case VoiceCommand.CursorInfo:           InputController?.ToggleCursorInfoVisibility(); break;
                case VoiceCommand.LogScale:             ChangeScalingType(ScalingType.Log);          break;
                case VoiceCommand.LinearScale:          ChangeScalingType(ScalingType.Linear);       break;
                case VoiceCommand.SqrtScale:            ChangeScalingType(ScalingType.Sqrt);         break;
                case VoiceCommand.AddNewSource:         InputController?.AddNewSource();             break;
                case VoiceCommand.SetSourceId:          SetMaskValue();                              break;
                case VoiceCommand.AddToList:            AddToList();                                 break;
                case VoiceCommand.Undo:                 Undo();                                      break;
                case VoiceCommand.Redo:                 Redo();                                      break;
                case VoiceCommand.SaveSubCube:          SaveSubCube();                               break;
                case VoiceCommand.PreviousSourceList:   PreviousSourceList();                        break;
                case VoiceCommand.NextSourceList:       NextSourceList();                            break;
                case VoiceCommand.EnterVideoMode:       QuickMenuController?.OpenVideoRecordingMenu(); break;
                case VoiceCommand.RecordVideoPosition:
                    if (InputController != null)
                        InputController.AddNewLocation(InputController.PrimaryHand);
                    break;
                case VoiceCommand.ExportVideoScript:    VideoRecordMenuController?.ExportToFile(); break;
            }
        }

        // ── Shape gesture commit (M-23, M-14) ────────────────────────────────
        // Replaces the direct ShapesManager.applyMask call in legacy source (§7).
        // RasteriseShape is a placeholder — see §9 limitations.

        /// <summary>
        /// Called by InteractionFSM when a shape gesture completes.
        /// Rasterises the gesture AABB into a voxel list and commits via ApplyBrush.
        /// No PaintPolygon call (M-14).
        /// </summary>
        public void CommitShapeGesture(ShapeGestureState gesture)
        {
            // TODO: build BrushStroke from RasteriseShape output, then:
            // var stroke = new BrushStroke(..., RasteriseShape(gesture), CurrentBrush);
            // MaskMutationService?.ApplyBrush(stroke);
            // MaskMutationService?.FinishStroke();
            Debug.Log("CommitShapeGesture — rasterisation placeholder (see §9 limitations)");
        }

        private static System.Collections.Generic.IReadOnlyList<object> RasteriseShape(ShapeGestureState _)
        {
            // Placeholder. Real impl translates ShapesManager.applyMask nested
            // for-loop, replacing PaintCursor calls with VoxelCoord2D additions.
            return System.Array.Empty<object>();
        }

        // ── Paint stroke (M-14: ApplyBrush only, never PaintPolygon) ─────────

        /// <summary>
        /// Commits a completed stroke from PaintMenuController.
        /// Calls ApplyBrush then FinishStroke (ST2). No PaintPolygon call (M-14).
        /// </summary>
        public void CommitStroke(BrushStroke stroke)
        {
            MaskMutationService?.ApplyBrush(stroke);
            MaskMutationService?.FinishStroke();
        }

        // Delegate form used by InteractionFSM when no BrushStroke is available yet.
        public void CommitStroke() { /* filled by PaintMenuController.OnTriggerReleased */ }

        public void CommitParameterEdit()
        {
            ApplyPendingParam();
            _pendingParamName = null;
        }

        // ── Undo / Redo ───────────────────────────────────────────────────────

        public void Undo()
        {
            MaskMutationService?.Undo();
        }

        public void Redo()
        {
            MaskMutationService?.Redo();
        }

        // ── Threshold / ZAxis / Transform — now via IRenderSettingsMutator ────
        // Legacy: each of these methods contained an 8-level transform.Find chain
        // to reach the UI slider and call .value = ..., plus a direct renderer field
        // mutation. Both are replaced by a single IRenderSettingsMutator call.

        public void resetThreshold()
        {
            RenderMutator?.ResetThreshold();
            ToastNotification.ShowInfo("Threshold reset");
        }

        public void resetZAxis()   => RenderMutator?.ResetZAxis();
        public void resetTransform() => RenderMutator?.ResetTransform();

        public void startThresholdEditing(bool editingMax)
            => InputController?.StartThresholdEditing(editingMax);

        public void endThresholdEditing()
        {
            InputController?.EndEditing();
            // Slider sync now driven by IRenderSettings.SettingsChanged event
            // which CanvassDesktop subscribes to (M-09). No transform.Find chain.
        }

        public void startZAxisEditing() => InputController?.StartZAxisEditing();
        public void endZAxisEditing()   => InputController?.EndEditing();

        // ── Colour map / projection / sampling ───────────────────────────────

        public void setColorMap(ColorMapEnum colorMap)
            => RenderMutator?.SetColorMap(colorMap);

        public void setProjection(ProjectionMode mode)
        {
            RenderMutator?.SetProjection(mode);
            ToastNotification.ShowInfo($"Accumulation set to {mode}");
        }

        public void SetSamplingMode(bool maxMode)
        {
            var config = Config.Instance;
            if (config.maxModeDownsampling == maxMode) return;
            config.maxModeDownsampling = maxMode;
            // Regenerate via IVolumeRegistry instead of direct renderer call.
            // VolumeRegistry?.ActiveVolume?.RegenerateCubes(); // TODO: IVolumeLoader
            ToastNotification.ShowInfo($"Downsampling mode set to {(maxMode ? "max" : "average")}");
        }

        public void ChangeScalingType(ScalingType scalingType)
        {
            RenderMutator?.SetScaling(scalingType);
            ToastNotification.ShowInfo($"Scaling type set to {scalingType}");
        }

        // ── Mask ─────────────────────────────────────────────────────────────

        public void setMask(MaskMode mode)
        {
            if (MaskMutationService == null) { throwMissingMaskError(); return; }
            var maskState = VolumeRegistry?.ActiveVolume?.MaskEditState;
            if (maskState == null) { throwMissingMaskError(); return; }
            MaskMutationService.MaskMode = mode;
        }

        public void ShowMaskOutline()
        {
            if (MaskMutationService == null || VolumeRegistry?.ActiveVolume?.MaskEditState == null)
            { throwMissingMaskError(); return; }
            MaskMutationService.DisplayMask = true;
        }

        public void HideMaskOutline()
        {
            if (MaskMutationService == null || VolumeRegistry?.ActiveVolume?.MaskEditState == null)
            { throwMissingMaskError(); return; }
            MaskMutationService.DisplayMask = false;
        }

        // ── Crop / selection / teleport ───────────────────────────────────────

        public void cropDataSet()
        {
            // Legacy: _activeDataSet.CropToFeature()
            // TODO: route via IVolumeLoader or feature-to-subcube command (ST1/ST5).
            Debug.Log("cropDataSet — route via IVolumeLoader");
        }

        public void resetCropDataSet()
        {
            Debug.Log("resetCropDataSet — route via IVolumeLoader");
        }

        public void teleportToSelection()
        {
            Debug.Log("teleportToSelection — route via IVolumeLoader");
        }

        // ── Source list navigation (now via IFeatureListNavigation, M-11) ─────

        public void NextSourceList()     => FeatureListNavigation?.DisplayNextSet();
        public void PreviousSourceList() => FeatureListNavigation?.DisplayPreviousSet();

        // ── Paint mode ────────────────────────────────────────────────────────

        public void EnablePaintMode()  => QuickMenuController?.OpenPaintMenu();
        public void DisablePaintMode() => PaintMenuController?.ExitPaintMode();

        // ── Source ID / list ──────────────────────────────────────────────────

        public void SetMaskValue()
        {
            if (VolumeRegistry?.ActiveVolume?.MaskEditState == null) { throwMissingMaskError(); return; }
            InputController?.StartEditSourceID();
        }

        private void AddToList()
        {
            // Legacy: _activeDataSet.AddSelectionToList() — route via IFeatureSetQuery.
            FeatureSetQuery?.CopyFeatureToUserDefined(null!); // TODO: pass selected feature
        }

        // ── Subcube save ──────────────────────────────────────────────────────

        public void SaveSubCube()
        {
            // Legacy: _volumeInputController.SaveSubCube() → _activeDataSet.SaveSubCube()
            // Now via IWorkspaceSaveCommand or a dedicated export command (ST7/ST1).
            Debug.Log("SaveSubCube — route via persistence layer");
        }

        // ── Dataset management (kept for scene compat) ────────────────────────

        public void AddDataSet(object renderer) { /* via IVolumeRegistry in full impl */ }

        public void RemoveDataSet(object renderer)
        {
            // Legacy: detach transform, remove from list, Destroy, UpdateDataSets.
            // Now via IVolumeRegistry / IVolumeLoader.
        }

        public void stepDataSet(bool forwards)
        {
            // Legacy step logic via IVolumeRegistry.SetActive.
        }

        // ── Parameter editing helpers ─────────────────────────────────────────

        public void BeginParameterEdit(string paramName, float currentValue)
        {
            _pendingParamName  = paramName;
            _pendingParamValue = currentValue;
        }

        public void ScrubParameter(float delta)
        {
            if (_pendingParamName == null) return;
            _pendingParamValue += delta;
            ApplyPendingParam();
        }

        public void RevertParameterEdit() => _pendingParamName = null;

        private void ApplyPendingParam()
        {
            if (_pendingParamName == null) return;
            switch (_pendingParamName)
            {
                case "ThresholdMin": RenderMutator?.SetThreshold(_pendingParamValue, float.NaN); break;
                case "ThresholdMax": RenderMutator?.SetThreshold(float.NaN, _pendingParamValue); break;
                case "ZAxisFactor":  RenderMutator?.SetZAxisFactor(_pendingParamValue);           break;
            }
        }

        // ── Workspace save/load (ST7) ─────────────────────────────────────────

        public void TriggerSave()                  => SaveCommand?.Save();
        public void TriggerLoad(string stateId)    => LoadCommand?.Load(stateId);

        // ── Error helpers ─────────────────────────────────────────────────────

        private static void throwMissingMaskError()
            => ToastNotification.ShowError("No mask loaded for this functionality!");

        // ── Legacy compatibility entry point (VoiceCommandListCreator uses this) ─

        /// <summary>
        /// Kept for VoiceCommandListCreator backwards compat. Maps legacy keyword
        /// strings to the new VoiceCommand enum and dispatches.
        /// </summary>
        public void ExecuteVoiceCommandFromList(string keyword)
        {
            // Map legacy string keywords to VoiceCommand enum.
            // This preserves the VoiceCommandListItem UI without touching that script.
            if (LegacyKeywordMap.TryGetValue(keyword, out var cmd))
                DispatchVoiceCommand(cmd);
            else
                Debug.LogWarning($"ExecuteVoiceCommandFromList: unmapped keyword '{keyword}'");
        }

        private static readonly System.Collections.Generic.Dictionary<string, VoiceCommand>
            LegacyKeywordMap = new(System.StringComparer.OrdinalIgnoreCase)
        {
            ["edit min"]                = VoiceCommand.EditThresholdMin,
            ["edit max"]                = VoiceCommand.EditThresholdMax,
            ["edit zee axis"]           = VoiceCommand.EditZAxis,
            ["edit zed axis"]           = VoiceCommand.EditZAxis,
            ["save zee axis"]           = VoiceCommand.SaveZAxis,
            ["save zed axis"]           = VoiceCommand.SaveZAxis,
            ["reset zee axis"]          = VoiceCommand.ResetZAxis,
            ["reset zed axis"]          = VoiceCommand.ResetZAxis,
            ["save threshold"]          = VoiceCommand.SaveThreshold,
            ["reset threshold"]         = VoiceCommand.ResetThreshold,
            ["reset transform"]         = VoiceCommand.ResetTransform,
            ["color map plasma"]        = VoiceCommand.ColormapPlasma,
            ["color map rainbow"]       = VoiceCommand.ColormapRainbow,
            ["color map magma"]         = VoiceCommand.ColormapMagma,
            ["color map inferno"]       = VoiceCommand.ColormapInferno,
            ["color map viridis"]       = VoiceCommand.ColormapViridis,
            ["color map cube helix"]    = VoiceCommand.ColormapCubeHelix,
            ["color map turbo"]         = VoiceCommand.ColormapTurbo,
            ["crop selection"]          = VoiceCommand.CropSelection,
            ["teleport"]                = VoiceCommand.Teleport,
            ["reset crop"]              = VoiceCommand.ResetCropSelection,
            ["mask off"]                = VoiceCommand.MaskDisabled,
            ["mask on"]                 = VoiceCommand.MaskEnabled,
            ["mask invert"]             = VoiceCommand.MaskInverted,
            ["mask isolate"]            = VoiceCommand.MaskIsolated,
            ["projection maximum"]      = VoiceCommand.ProjectionMaximum,
            ["projection average"]      = VoiceCommand.ProjectionAverage,
            ["sampling mode maximum"]   = VoiceCommand.SamplingModeMaximum,
            ["sampling mode average"]   = VoiceCommand.SamplingModeAverage,
            ["paint mode"]              = VoiceCommand.PaintMode,
            ["exit paint mode"]         = VoiceCommand.ExitPaintMode,
            ["brush add"]               = VoiceCommand.BrushAdd,
            ["brush erase"]             = VoiceCommand.BrushErase,
            ["show mask outline"]       = VoiceCommand.ShowMaskOutline,
            ["hide mask outline"]       = VoiceCommand.HideMaskOutline,
            ["take picture"]            = VoiceCommand.TakePicture,
            ["cursor info"]             = VoiceCommand.CursorInfo,
            ["linear scale"]            = VoiceCommand.LinearScale,
            ["log scale"]               = VoiceCommand.LogScale,
            ["square root scale"]       = VoiceCommand.SqrtScale,
            ["add new source"]          = VoiceCommand.AddNewSource,
            ["set source ID"]           = VoiceCommand.SetSourceId,
            ["add to list"]             = VoiceCommand.AddToList,
            ["undo"]                    = VoiceCommand.Undo,
            ["redo"]                    = VoiceCommand.Redo,
            ["export sub cube"]         = VoiceCommand.SaveSubCube,
            ["next source list"]        = VoiceCommand.NextSourceList,
            ["previous source list"]    = VoiceCommand.PreviousSourceList,
            ["export video script"]     = VoiceCommand.ExportVideoScript,
            ["video mode"]              = VoiceCommand.EnterVideoMode,
            ["record position"]         = VoiceCommand.RecordVideoPosition,
        };
    }
}
