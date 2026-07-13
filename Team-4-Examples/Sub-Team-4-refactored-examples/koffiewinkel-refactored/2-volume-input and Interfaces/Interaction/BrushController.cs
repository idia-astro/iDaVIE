using System;
using Interaction.Interfaces;
using VolumeData;
using UnityEngine;

namespace Interaction
{
    public sealed class BrushController : IBrushController
    {
        private readonly Func<VolumeDataSetRenderer> _getActiveDataSet;
        private readonly Func<Vector3> _getPrimaryHandPosition;
        private readonly Action _requestStartEditSource;
        private readonly Func<Interaction.Interfaces.InteractionState> _getInteractionState;
        private readonly Action _requestCancelEditSource;
        private readonly Action<BrushHand> _onUndoRedoFeedback;

        public int BrushSize { get; private set; } = 1;
        public bool AdditiveBrush { get; private set; } = true;
        public short SourceId { get; private set; } = -1;

        public BrushController(
            Func<VolumeDataSetRenderer> getActiveDataSet,
            Func<Vector3> getPrimaryHandPosition,
            Action requestStartEditSource,
            Func<Interaction.Interfaces.InteractionState> getInteractionState,
            Action requestCancelEditSource,
            Action<BrushHand> onUndoRedoFeedback)
        {
            _getActiveDataSet = getActiveDataSet;
            _getPrimaryHandPosition = getPrimaryHandPosition;
            _requestStartEditSource = requestStartEditSource;
            _getInteractionState = getInteractionState;
            _requestCancelEditSource = requestCancelEditSource;
            _onUndoRedoFeedback = onUndoRedoFeedback;
        }

        public void IncreaseBrushSize()
        {
            BrushSize += 2;
            UpdatePaintCursor();
        }

        public void DecreaseBrushSize()
        {
            BrushSize = Math.Max(1, BrushSize - 2);
            UpdatePaintCursor();
        }

        public void ResetBrushSize()
        {
            BrushSize = 1;
            UpdatePaintCursor();
        }

        public void UndoBrushStroke(BrushHand hand)
        {
            var active = _getActiveDataSet?.Invoke();
            if (active?.Mask?.UndoBrushStroke() ?? false)
            {
                active.GetMomentMapRenderer()?.CalculateMomentMaps();
                _onUndoRedoFeedback?.Invoke(hand);
            }
        }

        public void RedoBrushStroke(BrushHand hand)
        {
            var active = _getActiveDataSet?.Invoke();
            if (active?.Mask?.RedoBrushStroke() ?? false)
            {
                active.GetMomentMapRenderer()?.CalculateMomentMaps();
                _onUndoRedoFeedback?.Invoke(hand);
            }
        }

        public void SetBrushAdditive()
        {
            AdditiveBrush = true;
            if (SourceId <= 0)
            {
                _requestStartEditSource?.Invoke();
            }
        }

        public void SetBrushSubtractive()
        {
            AdditiveBrush = false;
            if (_getInteractionState != null && _getInteractionState() == Interaction.Interfaces.InteractionState.EditingSourceId)
            {
                _requestCancelEditSource?.Invoke();
            }
        }

        public void AddNewSource()
        {
            AdditiveBrush = true;
            var active = _getActiveDataSet?.Invoke();
            if (active != null)
            {
                SourceId = active.Mask.NewSourceId;
                active.HighlightedSource = SourceId;
                active.Mask.NewSourceId++;
                _requestCancelEditSource?.Invoke();
            }
        }

        public void StartEditSourceID()
        {
            if (_getInteractionState == null)
            {
                return;
            }

            var current = _getInteractionState();
            if (current == Interaction.Interfaces.InteractionState.IdlePainting)
            {
                _requestStartEditSource?.Invoke();
            }
        }

        public void ResetSourceIdForPaintEntry()
        {
            SourceId = -1;
        }

        public void UpdateSourceIdFromCursor()
        {
            var active = _getActiveDataSet?.Invoke();
            if (active?.CursorSource != 0)
            {
                SourceId = active.CursorSource;
                active.HighlightedSource = SourceId;
                AdditiveBrush = true;
            }
        }

        private void UpdatePaintCursor()
        {
            var active = _getActiveDataSet?.Invoke();
            if (active != null)
            {
                active.SetCursorPosition(_getPrimaryHandPosition(), BrushSize);
            }
        }
    }
}
