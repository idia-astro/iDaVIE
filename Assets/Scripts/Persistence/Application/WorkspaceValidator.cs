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

using System;
using System.Collections.Generic;
using iDaVIE.Persistence.Domain;
using iDaVIE.Persistence.Domain.Dtos;
using iDaVIE.Persistence.Domain.Serialization;

namespace iDaVIE.Persistence.Application
{
    /// <summary>
    /// All per-field validation, clamping, and default-application rules.
    /// Applied during load before any subsystem adapter sees the data.
    /// </summary>
    public class WorkspaceValidator
    {
        public ValidationResult Validate(WorkspaceSnapshot snapshot)
        {
            var result = new ValidationResult();

            ValidateDataIo(snapshot.DataIo, result);
            ValidateRendering(snapshot.Rendering, result);
            ValidateInteraction(snapshot.Interaction, result);
            if (snapshot.Features != null)
                ValidateFeatures(snapshot.Features, result);
            if (snapshot.Gui != null)
                ValidateGui(snapshot.Gui, result);

            return result;
        }

        // ── Data I/O ────────────────────────────────────────────────────────────

        private static void ValidateDataIo(DataIoStateDto dto, ValidationResult result)
        {
            if (string.IsNullOrEmpty(dto.FileName))
            {
                result.DatasetUnavailable = true;
                result.AddWarning("DataIo.FileName is missing — entering disconnected mode.");
            }

            if (dto.SelectedHdu <= 0)
            {
                result.AddWarning($"DataIo.SelectedHdu ({dto.SelectedHdu}) invalid; reset to 1.");
                dto.SelectedHdu = 1;
            }

            if (string.IsNullOrEmpty(dto.StandardOfRest))
            {
                result.AddWarning("DataIo.StandardOfRest missing; defaulting to 'Heliocentric'.");
                dto.StandardOfRest = "Heliocentric";
            }

            if (dto.Index2.HasValue && dto.Index2 != 2 && dto.Index2 != 3)
            {
                result.AddWarning($"DataIo.Index2 ({dto.Index2}) invalid; cleared (must be 2 or 3).");
                dto.Index2 = null;
            }
        }

        // ── Rendering ───────────────────────────────────────────────────────────

        private static void ValidateRendering(RenderingStateDto dto, ValidationResult result)
        {
            if (!IsValidEnumName<KnownColorMap>(dto.ColorMap))
            {
                result.AddWarning($"Rendering.ColorMap '{dto.ColorMap}' invalid; defaulting to 'Inferno'.");
                dto.ColorMap = "Inferno";
            }

            if (!IsValidEnumName<KnownScalingType>(dto.ScalingType))
            {
                result.AddWarning($"Rendering.ScalingType '{dto.ScalingType}' invalid; defaulting to 'Linear'.");
                dto.ScalingType = "Linear";
            }

            if (dto.ThresholdMin > dto.ThresholdMax)
            {
                result.AddWarning($"Rendering.ThresholdMin ({dto.ThresholdMin}) > ThresholdMax ({dto.ThresholdMax}); reset to [0,1].");
                dto.ThresholdMin = 0f;
                dto.ThresholdMax = 1f;
            }

            if (dto.MaxSteps < 16 || dto.MaxSteps > 512)
            {
                int clamped = Math.Max(16, Math.Min(512, dto.MaxSteps));
                result.AddWarning($"Rendering.MaxSteps ({dto.MaxSteps}) outside [16,512]; clamped to {clamped}.");
                dto.MaxSteps = clamped;
            }

            if (dto.Scale != null && dto.Scale.HasNonPositiveComponent())
            {
                result.AddWarning($"Rendering.Scale {dto.Scale} has non-positive component; reset to (1,1,1).");
                dto.Scale = SerializableVector3.One;
            }

            if (dto.Rotation != null && dto.Rotation.IsNotNormalised())
            {
                result.AddWarning("Rendering.Rotation is not normalised; normalising.");
                dto.Rotation = dto.Rotation.Normalised();
            }

            if (dto.Foveation == null)
            {
                dto.Foveation = new FoveationStateDto { FoveatedRendering = false };
            }
            else
            {
                dto.Foveation.FoveatedStepsLow  = Math.Max(16, Math.Min(512, dto.Foveation.FoveatedStepsLow));
                dto.Foveation.FoveatedStepsHigh = Math.Max(16, Math.Min(512, dto.Foveation.FoveatedStepsHigh));
            }

            if (dto.Mask == null)
            {
                dto.Mask = new MaskStateDto { MaskMode = "Disabled", DisplayMask = false };
            }
        }

        // ── Interaction ─────────────────────────────────────────────────────────

        private static readonly HashSet<string> ValidInteractionModes = new HashSet<string>
        {
            "IdleSelecting", "IdlePainting", "EditingSourceId",
            "Creating", "Editing", "Painting", "VideoCamPosRecording"
        };

        private static readonly HashSet<string> ValidLocomotionStates = new HashSet<string>
        {
            "Idle", "Moving", "Scaling", "EditingThresholdMin", "EditingThresholdMax", "EditingZAxis"
        };

        private static void ValidateInteraction(InteractionStateDto dto, ValidationResult result)
        {
            if (dto.ActiveInteractionMode == "VideoCamPosRecording")
            {
                result.AddWarning("Interaction.ActiveInteractionMode was 'VideoCamPosRecording'; reset to 'IdleSelecting'.");
                dto.ActiveInteractionMode = "IdleSelecting";
            }
            else if (!ValidInteractionModes.Contains(dto.ActiveInteractionMode ?? ""))
            {
                result.AddWarning($"Interaction.ActiveInteractionMode '{dto.ActiveInteractionMode}' invalid; defaulting to 'IdleSelecting'.");
                dto.ActiveInteractionMode = "IdleSelecting";
            }

            if (!ValidLocomotionStates.Contains(dto.LocomotionState ?? ""))
            {
                result.AddWarning($"Interaction.LocomotionState '{dto.LocomotionState}' invalid; defaulting to 'Idle'.");
                dto.LocomotionState = "Idle";
            }

            if (dto.BrushSize < 1)
            {
                result.AddWarning($"Interaction.BrushSize ({dto.BrushSize}) < 1; clamped to 1.");
                dto.BrushSize = 1;
            }

            if (dto.SourceId < -1)
            {
                result.AddWarning($"Interaction.SourceId ({dto.SourceId}) < -1; reset to -1.");
                dto.SourceId = -1;
            }

            if (dto.VignetteFadeSpeed < 0.1f || dto.VignetteFadeSpeed > 5.0f)
            {
                float clamped = Math.Max(0.1f, Math.Min(5.0f, dto.VignetteFadeSpeed));
                result.AddWarning($"Interaction.VignetteFadeSpeed ({dto.VignetteFadeSpeed}) outside [0.1,5.0]; clamped to {clamped}.");
                dto.VignetteFadeSpeed = clamped;
            }

            if (dto.PrimaryHand != "LeftHand" && dto.PrimaryHand != "RightHand")
            {
                result.AddWarning($"Interaction.PrimaryHand '{dto.PrimaryHand}' invalid; defaulting to 'RightHand'.");
                dto.PrimaryHand = "RightHand";
            }

            if (dto.RotationAxisCutoff <= 0f)
            {
                result.AddWarning($"Interaction.RotationAxisCutoff ({dto.RotationAxisCutoff}) <= 0; defaulting to 5.0.");
                dto.RotationAxisCutoff = 5.0f;
            }
        }

        // ── Features ─────────────────────────────────────────────────────────────

        private static void ValidateFeatures(FeatureStateDto dto, ValidationResult result)
        {
            var setsToRemove = new List<FeatureSetDto>();

            foreach (var set in dto.FeatureSets)
            {
                var toExclude = new List<FeatureDto>();
                foreach (var feature in set.Features)
                {
                    bool hasMin = feature.CornerMin != null;
                    bool hasMax = feature.CornerMax != null;

                    // Temporary features in progress are allowed to have incomplete bounds.
                    if (!feature.IsTemporary && hasMin != hasMax)
                    {
                        result.AddWarning($"Feature '{feature.Name}' (Id={feature.Id}) has incomplete CornerMin/Max pair; excluded.");
                        toExclude.Add(feature);
                        result.ExcludedFeatureIds.Add(feature.Id);
                    }
                }

                foreach (var f in toExclude)
                    set.Features.Remove(f);
            }

            foreach (var s in setsToRemove)
                dto.FeatureSets.Remove(s);
        }

        // ── GUI ──────────────────────────────────────────────────────────────────

        private static void ValidateGui(GuiStateDto dto, ValidationResult result)
        {
            if (dto.CubeDepthAxis < 0 || dto.CubeDepthAxis > 2)
            {
                result.AddWarning($"Gui.CubeDepthAxis ({dto.CubeDepthAxis}) outside [0,2]; defaulting to 2.");
                dto.CubeDepthAxis = 2;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static bool IsValidEnumName<T>(string value) where T : struct
        {
            if (string.IsNullOrEmpty(value)) return false;
            return Enum.IsDefined(typeof(T), value);
        }

        // Local mirror enums (must stay in sync with Unity ColorMapEnum and ScalingType)
        private enum KnownColorMap { None, Inferno, Plasma, Turbo, Viridis, Magma, Greys, Hot, Rainbow, Jet, Coolwarm }
        private enum KnownScalingType { Linear, Log, Sqrt, Square, Power, Gamma }
    }

    /// <summary>Result of a validation pass.</summary>
    public class ValidationResult
    {
        public bool DatasetUnavailable { get; set; }
        public List<string> Warnings { get; } = new List<string>();
        public List<int> ExcludedFeatureIds { get; } = new List<int>();
        public bool HasWarnings => Warnings.Count > 0;

        public void AddWarning(string message) => Warnings.Add(message);
    }
}
