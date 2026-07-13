using System;
using System.Diagnostics;
using DataFeatures;
using Interaction.Interfaces;
using Stateless;
using TMPro;
using UnityEngine;
using VolumeData;
using Debug = UnityEngine.Debug;

namespace Interaction
{
    public sealed class InteractionController : IInteractionController
    {
        private readonly Func<VolumeDataSetRenderer> _getActiveDataSet;
        private readonly Func<Vector3> _getPrimaryHandPosition;
        private readonly Func<int> _getPrimaryHandIndex;
        private readonly Func<bool> _showCursorInfo;
        private readonly Func<bool> _isLocomotionIdle;
        private readonly Action _startPaintModeAction;
        private readonly Action _exitPaintModeAction;
        private readonly Action _startSelectionAction;
        private readonly Action _endSelectionAction;
        private readonly Action _startVideoRecordingAction;
        private readonly Action _endVideoRecordingAction;
        private readonly Func<int> _brushSize;
        private readonly Func<bool> _additiveBrush;
        private readonly Func<short> _sourceId;
        private readonly ICursorInfoFormatter _cursorFormatter;
        private readonly TextMeshPro[] _handInfoComponents;
        private readonly Func<bool> _displayCursorInfoOutsideCube;
        private readonly BrushController _brushController;

        private readonly Stopwatch _selectionStopwatch = new Stopwatch();
        private Feature _hoveredFeature;
        private Feature _editingFeature;
        private FeatureAnchor _hoveredAnchor;
        private FeatureAnchor _editingAnchor;

        public InteractionState CurrentState => StateMachine.State;
        public StateMachine<InteractionState, InteractionEvent> StateMachine { get; }

        public InteractionController(
            Func<VolumeDataSetRenderer> getActiveDataSet,
            Func<Vector3> getPrimaryHandPosition,
            Func<int> getPrimaryHandIndex,
            Func<bool> showCursorInfo,
            Func<bool> isLocomotionIdle,
            Action startPaintModeAction,
            Action exitPaintModeAction,
            Action startSelectionAction,
            Action endSelectionAction,
            Action startVideoRecordingAction,
            Action endVideoRecordingAction,
            Func<int> brushSize,
            Func<bool> additiveBrush,
            Func<short> sourceId,
            ICursorInfoFormatter cursorFormatter,
            TextMeshPro[] handInfoComponents,
            Func<bool> displayCursorInfoOutsideCube,
            BrushController brushController)
        {
            _getActiveDataSet = getActiveDataSet;
            _getPrimaryHandPosition = getPrimaryHandPosition;
            _getPrimaryHandIndex = getPrimaryHandIndex;
            _showCursorInfo = showCursorInfo;
            _isLocomotionIdle = isLocomotionIdle;
            _startPaintModeAction = startPaintModeAction;
            _exitPaintModeAction = exitPaintModeAction;
            _startSelectionAction = startSelectionAction;
            _endSelectionAction = endSelectionAction;
            _startVideoRecordingAction = startVideoRecordingAction;
            _endVideoRecordingAction = endVideoRecordingAction;
            _brushSize = brushSize;
            _additiveBrush = additiveBrush;
            _sourceId = sourceId;
            _cursorFormatter = cursorFormatter;
            _handInfoComponents = handInfoComponents;
            _displayCursorInfoOutsideCube = displayCursorInfoOutsideCube;
            _brushController = brushController;
            StateMachine = CreateStateMachine();
        }

        public void Fire(InteractionEvent interactionEvent)
        {
            StateMachine.Fire(interactionEvent);
        }

        public void Update()
        {
            var dataSet = _getActiveDataSet();
            if (!dataSet)
            {
                return;
            }

            var currentState = StateMachine.State;
            var cursorPosWorldSpace = _getPrimaryHandPosition();
            var activeBrushSize = (currentState == InteractionState.Painting || currentState == InteractionState.IdlePainting) ? _brushSize() : 1;
            dataSet.SetCursorPosition(cursorPosWorldSpace, activeBrushSize);

            if (currentState == InteractionState.Painting)
            {
                dataSet.PaintCursor(_additiveBrush() ? _sourceId() : (short)0);
            }
            else if (currentState == InteractionState.Creating)
            {
                dataSet.SetRegionPosition(cursorPosWorldSpace, false);
            }
            else if (currentState == InteractionState.Editing && HasEditingAnchor && _editingFeature.FeatureSetParent.FeatureSetType != FeatureSetType.Mask)
            {
                UpdateRegionEdit(dataSet, cursorPosWorldSpace);
            }

            string cursorString = BuildCursorString(dataSet, currentState);
            UpdateHandInfo(dataSet, cursorString, currentState);
        }

        public void SetHoveredFeature(FeatureSetManager featureSetManager, FeatureAnchor featureAnchor)
        {
            _hoveredFeature = featureSetManager?.SelectedFeature;
            _hoveredAnchor = featureAnchor;
        }

        public void ClearHoveredFeature(FeatureSetManager featureSetManager, FeatureAnchor featureAnchor)
        {
            var hoveredFeature = featureSetManager?.SelectedFeature;
            if (_hoveredFeature == hoveredFeature && _hoveredAnchor == featureAnchor)
            {
                _hoveredFeature = null;
                _hoveredAnchor = null;
            }
        }

        public void EnterPaintMode()
        {
            _startPaintModeAction?.Invoke();
        }

        public void ExitPaintMode()
        {
            _exitPaintModeAction?.Invoke();
        }

        public bool HasHoverAnchor => (_hoveredAnchor && _hoveredFeature != null);
        public bool HasEditingAnchor => (_editingAnchor && _editingFeature != null);

        private StateMachine<InteractionState, InteractionEvent> CreateStateMachine()
        {
            var machine = new StateMachine<InteractionState, InteractionEvent>(InteractionState.IdleSelecting);
            machine.Configure(InteractionState.IdleSelecting)
                .OnEntryFrom(InteractionEvent.PaintModeDisabled, ExitPaintMode)
                .Permit(InteractionEvent.PaintModeEnabled, InteractionState.IdlePainting)
                .Permit(InteractionEvent.StartVideoRecording, InteractionState.VideoCamPosRecording)
                .PermitIf(InteractionEvent.InteractionStarted, InteractionState.Creating, () => !HasHoverAnchor)
                .PermitIf(InteractionEvent.InteractionStarted, InteractionState.Editing, () => HasHoverAnchor);

            machine.Configure(InteractionState.IdlePainting)
                .OnEntryFrom(InteractionEvent.PaintModeEnabled, EnterPaintMode)
                .OnEntryFrom(InteractionEvent.EndEditSource, () => _brushController.UpdateSourceIdFromCursor())
                .Permit(InteractionEvent.StartEditSource, InteractionState.EditingSourceId)
                .Permit(InteractionEvent.PaintModeDisabled, InteractionState.IdleSelecting)
                .PermitIf(InteractionEvent.InteractionStarted, InteractionState.Painting, () => _isLocomotionIdle() && (_getActiveDataSet()?.IsFullResolution ?? false));

            machine.Configure(InteractionState.EditingSourceId)
                .IgnoreIf(InteractionEvent.EndEditSource, () => _sourceId() <= 0 && _getActiveDataSet()?.CursorSource == 0)
                .PermitIf(InteractionEvent.EndEditSource, InteractionState.IdlePainting, () => _getActiveDataSet()?.CursorSource != 0 || _sourceId() > 0)
                .Permit(InteractionEvent.CancelEditSource, InteractionState.IdlePainting);

            machine.Configure(InteractionState.Painting)
                .OnExit(() => _getActiveDataSet()?.FinishBrushStroke())
                .Permit(InteractionEvent.InteractionEnded, InteractionState.IdlePainting);

            machine.Configure(InteractionState.Creating)
                .OnEntry(StartSelection)
                .OnExit(EndSelection)
                .Permit(InteractionEvent.InteractionEnded, InteractionState.IdleSelecting);

            machine.Configure(InteractionState.Editing)
                .OnEntry(StartRegionEditing)
                .OnExit(EndRegionEditing)
                .Permit(InteractionEvent.InteractionEnded, InteractionState.IdleSelecting);

            machine.Configure(InteractionState.VideoCamPosRecording)
                .OnEntry(() => _startVideoRecordingAction?.Invoke())
                .OnExit(() => _endVideoRecordingAction?.Invoke())
                .Permit(InteractionEvent.EndVideoRecording, InteractionState.IdleSelecting);

            return machine;
        }

        private void StartSelection()
        {
            _startSelectionAction?.Invoke();
            _selectionStopwatch.Reset();
            _selectionStopwatch.Start();
            Debug.Log("Entering selecting state");
        }

        private void EndSelection()
        {
            _endSelectionAction?.Invoke();
            var activeDataSet = _getActiveDataSet();
            if (!activeDataSet)
            {
                return;
            }

            var endPosition = _getPrimaryHandPosition();
            _selectionStopwatch.Stop();
            activeDataSet.ClearRegion();
            activeDataSet.ClearMeasure();
            var featureSetManager = activeDataSet.GetComponentInChildren<FeatureSetManager>();
            if (_selectionStopwatch.ElapsedMilliseconds < 200)
            {
                activeDataSet.SelectFeature(endPosition);
            }
            else if (featureSetManager)
            {
                featureSetManager.CreateSelectionFeature(activeDataSet.RegionStartVoxel, activeDataSet.RegionEndVoxel);
            }
        }

        private void StartRegionEditing()
        {
            _editingFeature = _hoveredFeature;
            _editingAnchor = _hoveredAnchor;
        }

        private void EndRegionEditing()
        {
            _editingFeature = null;
            _editingAnchor = null;
        }

        private void UpdateRegionEdit(VolumeDataSetRenderer dataSet, Vector3 cursorPosWorldSpace)
        {
            var voxelPosition = dataSet.GetVoxelPositionWorldSpace(cursorPosWorldSpace);
            var newCornerMin = _editingFeature.CornerMin;
            var newCornerMax = _editingFeature.CornerMax;

            if (_editingAnchor.name.Contains("front")) newCornerMax.z = voxelPosition.z;
            else if (_editingAnchor.name.Contains("back")) newCornerMin.z = voxelPosition.z;
            if (_editingAnchor.name.Contains("right")) newCornerMax.x = voxelPosition.x;
            else if (_editingAnchor.name.Contains("left")) newCornerMin.x = voxelPosition.x;
            if (_editingAnchor.name.Contains("top")) newCornerMax.y = voxelPosition.y;
            else if (_editingAnchor.name.Contains("bottom")) newCornerMin.y = voxelPosition.y;

            _editingFeature.SetBounds(newCornerMin, newCornerMax);
            dataSet.SetRegionBounds(Vector3Int.RoundToInt(newCornerMin), Vector3Int.RoundToInt(newCornerMax), true);
        }

        private string BuildCursorString(VolumeDataSetRenderer dataSet, InteractionState state)
        {
            if (state == InteractionState.Creating || state == InteractionState.Editing)
            {
                return _cursorFormatter.FormatSelectionInfo(dataSet);
            }

            if (state == InteractionState.EditingSourceId)
            {
                if (dataSet.CursorSource != 0)
                {
                    return $"Press trigger to update{Environment.NewLine}source ID to {dataSet.CursorSource}";
                }

                var text = "Place hand in desired source";
                if (_sourceId() >= 0)
                {
                    text += $"{Environment.NewLine}Press trigger to cancel";
                }

                return text;
            }

            return _cursorFormatter.FormatCursorInfo(dataSet, _displayCursorInfoOutsideCube());
        }

        private void UpdateHandInfo(VolumeDataSetRenderer dataSet, string cursorString, InteractionState state)
        {
            if (_handInfoComponents == null)
            {
                return;
            }

            int primary = _getPrimaryHandIndex();
            _handInfoComponents[primary].enabled = true;
            _handInfoComponents[1 - primary].enabled = false;
            _handInfoComponents[primary].text = (_showCursorInfo() || state == InteractionState.EditingSourceId) ? cursorString : string.Empty;
            _handInfoComponents[primary].color = IsCursorOutsideCube(dataSet) ? new Color(0.86f, 0.078f, 0.235f) : Color.white;
        }

        private static bool IsCursorOutsideCube(VolumeDataSetRenderer dataSetRenderer)
        {
            return dataSetRenderer.CursorVoxel.x < 1 ||
                   dataSetRenderer.CursorVoxel.y < 1 ||
                   dataSetRenderer.CursorVoxel.z < 1 ||
                   dataSetRenderer.CursorVoxel.x > dataSetRenderer.Data.XDim ||
                   dataSetRenderer.CursorVoxel.y > dataSetRenderer.Data.YDim ||
                   dataSetRenderer.CursorVoxel.z > dataSetRenderer.Data.ZDim;
        }
    }
}
