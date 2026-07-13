using DataFeatures;
using Stateless;

namespace Interaction.Interfaces
{
    public enum InteractionState
    {
        IdleSelecting,
        IdlePainting,
        EditingSourceId,
        Creating,
        Editing,
        Painting,
        VideoCamPosRecording
    }

    public enum InteractionEvent
    {
        InteractionStarted,
        InteractionEnded,
        PaintModeEnabled,
        PaintModeDisabled,
        StartEditSource,
        EndEditSource,
        CancelEditSource,
        StartVideoRecording,
        EndVideoRecording
    }

    public interface IInteractionController
    {
        InteractionState CurrentState { get; }
        StateMachine<InteractionState, InteractionEvent> StateMachine { get; }
        void Fire(InteractionEvent interactionEvent);
        void Update();
        void SetHoveredFeature(FeatureSetManager featureSetManager, FeatureAnchor featureAnchor);
        void ClearHoveredFeature(FeatureSetManager featureSetManager, FeatureAnchor featureAnchor);
        void EnterPaintMode();
        void ExitPaintMode();
    }
}
