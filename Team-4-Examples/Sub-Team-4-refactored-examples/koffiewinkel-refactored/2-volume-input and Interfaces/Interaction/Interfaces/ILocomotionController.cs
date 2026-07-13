using UnityEngine;

namespace Interaction.Interfaces
{
    public enum LocomotionState
    {
        Idle,
        Moving,
        Scaling,
        EditingThresholdMin,
        EditingThresholdMax,
        EditingZAxis
    }

    public interface ILocomotionController
    {
        LocomotionState CurrentState { get; }
        void TransitionToMoving();
        void TransitionToIdle();
        void TransitionToScaling();
        void Update(float deltaTime);
        void StartThresholdEditing(bool editingMax);
        void StartZAxisEditing();
        void EndEditing();
    }
}
