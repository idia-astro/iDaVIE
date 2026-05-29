// SPDX-License-Identifier: LGPL-3.0-or-later
// ControllerInputAdapter — ST4 Unity ACL MonoBehaviour. Realises
// IControllerEventStream (declared in IInteractionContracts.cs). Replaces
// Assets/Scripts/UI/LaserPointer.cs (205 LOC) and Assets/Scripts/UI/PointerController.cs
// (121 LOC). No SteamVR type crosses the IControllerEventStream boundary (MOD-01).

using System;
using UnityEngine;
using iDaVIE.Kernel.Contracts.Types;

namespace iDaVIE.Interaction
{
    internal sealed class ControllerInputAdapter : MonoBehaviour, IControllerEventStream
    {
        // ── IGripInput ───────────────────────────────────────────────────────
        public event Action<int> GripPressed;
        public event Action<int> GripReleased;
        public bool IsAnyGripHeld   => throw new NotImplementedException();
        public bool IsBothGripsHeld => throw new NotImplementedException();

        // ── IPointerInput ────────────────────────────────────────────────────
        public event Action<int>                 TriggerPressed;
        public event Action<int>                 TriggerReleased;
        public event Action<int, float>          TriggerHeld;
        public event Action<int, CartesianCoord> PointerEntered;
        public event Action<int>                 PointerExited;

        // ── IHapticsOutput ──────────────────────────────────────────────────
        public void Pulse(int controllerIndex, float durationSeconds, float amplitude)
            => throw new NotImplementedException();

        // ── IThumbstickInput ────────────────────────────────────────────────
        public event Action<int, float, float> ThumbstickMoved;
        public event Action<int>               ThumbstickClicked;

        // ── ITeleportInput ──────────────────────────────────────────────────
        public event Action<CartesianCoord> TeleportRequested;
    }
}
