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

// Unity-side adapter — only layer permitted to reference UnityEngine.
// Reads from VolumeInputController. Must be called on the Unity main thread.

using iDaVIE.Persistence.Application.Interfaces;
using iDaVIE.Persistence.Domain.Dtos;
using UnityEngine;
using Valve.VR;

namespace iDaVIE.Persistence.Adapters
{
    /// <summary>
    /// Captures and restores VR interaction state.
    ///
    /// Two save scopes:
    ///   User preferences (survive crashes): PrimaryHand, InPlaceScaling, ScalingEnabled,
    ///                                        VignetteFadeSpeed, RotationAxisCutoff.
    ///   Session state (same session only): ActiveInteractionMode, LocomotionState,
    ///                                       BrushSize, SourceId, AdditiveBrush.
    ///
    /// NOT captured: SteamVR controller refs, grip positions, hovered/editing feature refs.
    /// LocomotionState is private in VolumeInputController — captured via GetLocomotionStateString().
    /// </summary>
    public class InteractionStateAdapter : IInteractionStateAdapter
    {
        private readonly VolumeInputController _controller;

        public InteractionStateAdapter(VolumeInputController controller)
        {
            _controller = controller;
        }

        public InteractionStateDto Capture()
        {
            return new InteractionStateDto
            {
                // Session state
                ActiveInteractionMode = _controller.CurrentInteractionState.ToString(),
                LocomotionState       = _controller.GetLocomotionStateString(),
                AdditiveBrush         = _controller.AdditiveBrush,
                BrushSize             = _controller.BrushSize,
                SourceId              = _controller.SourceId,

                // User preferences
                PrimaryHand         = _controller.PrimaryHand == SteamVR_Input_Sources.LeftHand
                                        ? "LeftHand" : "RightHand",
                InPlaceScaling      = _controller.InPlaceScaling,
                ScalingEnabled      = _controller.ScalingEnabled,
                VignetteFadeSpeed   = _controller.VignetteFadeSpeed,
                RotationAxisCutoff  = _controller.RotationAxisCutoff,
            };
        }

        public void Restore(InteractionStateDto dto)
        {
            // User preferences are always restored
            _controller.InPlaceScaling    = dto.InPlaceScaling;
            _controller.ScalingEnabled    = dto.ScalingEnabled;
            _controller.VignetteFadeSpeed = dto.VignetteFadeSpeed;
            _controller.RotationAxisCutoff = dto.RotationAxisCutoff;
            _controller.PrimaryHand = dto.PrimaryHand == "LeftHand"
                ? SteamVR_Input_Sources.LeftHand
                : SteamVR_Input_Sources.RightHand;

            // Session state
            // Note: ActiveInteractionMode == "VideoCamPosRecording" has already been reset
            // to "IdleSelecting" by WorkspaceValidator before Restore() is called.
            _controller.BrushSize = dto.BrushSize;
            _controller.SourceId  = dto.SourceId;
            // AdditiveBrush and ActiveInteractionMode are managed by the state machine;
            // they are set via the public API rather than direct assignment.
            // TODO: expose SetInteractionMode() and SetAdditiveBrush() on VolumeInputController.

            Debug.Log("[Persistence] InteractionState restored.");
        }
    }
}
