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
 */

namespace iDaVIE.Persistence.Domain.Dtos
{
    /// <summary>
    /// Persistent state for the VR interaction sub-system.
    /// Source: VolumeInputController public fields + GetLocomotionStateString().
    /// NOT persisted: SteamVR controller refs, grip positions, hovered/editing feature refs.
    /// </summary>
    public class InteractionStateDto
    {
        /// <summary>
        /// VolumeInputController.InteractionState enum as string (e.g. "IdleSelecting", "IdlePainting").
        /// WorkspaceValidator resets "VideoCamPosRecording" → "IdleSelecting" on load.
        /// </summary>
        [PersistField]
        public string ActiveInteractionMode { get; set; } = "IdleSelecting";

        /// <summary>VolumeInputController.LocomotionState (private enum) as string.</summary>
        [PersistField]
        public string LocomotionState { get; set; } = "Idle";

        // ── Session state ────────────────────────────────────────────────────────
        [PersistField] public bool  AdditiveBrush { get; set; } = true;
        [PersistField] public int   BrushSize     { get; set; } = 1;
        [PersistField] public short SourceId      { get; set; } = -1;

        // ── User preferences ─────────────────────────────────────────────────────
        /// <summary>"LeftHand" or "RightHand".</summary>
        [PersistField] public string PrimaryHand      { get; set; } = "RightHand";
        [PersistField] public bool   InPlaceScaling   { get; set; } = true;
        [PersistField] public bool   ScalingEnabled   { get; set; } = true;

        /// <summary>Range [0.1, 5.0] — clamped by WorkspaceValidator on load.</summary>
        [PersistField] public float VignetteFadeSpeed    { get; set; } = 2.0f;

        /// <summary>Must be > 0 to function as angle threshold.</summary>
        [PersistField] public float RotationAxisCutoff  { get; set; } = 5.0f;
    }
}
