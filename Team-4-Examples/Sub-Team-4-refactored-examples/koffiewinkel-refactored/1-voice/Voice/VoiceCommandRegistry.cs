/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 */
using System;
using System.Collections.Generic;
using UnityEngine;

namespace VolumeData.Voice
{
    public static class VoiceCommandRegistry
    {
        public static IReadOnlyDictionary<string, IVoiceCommand> Build(VolumeCommandController owner)
        {
            var commands = new Dictionary<string, IVoiceCommand>(StringComparer.Ordinal);

            void Add(string phrase, Action<IVoiceCommandContext> action) =>
                commands[phrase] = new DelegateVoiceCommand(action);

            var k = VolumeCommandController.Keywords;

            Add(k.EditThresholdMin, c => owner.startThresholdEditing(false));
            Add(k.EditThresholdMax, c => owner.startThresholdEditing(true));
            Add(k.SaveThreshold, c => owner.endThresholdEditing());
            Add(k.ResetThreshold, c => owner.resetThreshold());

            Add(k.EditZAxis, c => owner.startZAxisEditing());
            Add(k.EditZAxisAlt, c => owner.startZAxisEditing());
            Add(k.SaveZAxis, c => owner.endZAxisEditing());
            Add(k.SaveZAxisAlt, c => owner.endZAxisEditing());
            Add(k.ResetZAxis, c => owner.resetZAxis());
            Add(k.ResetZAxisAlt, c => owner.resetZAxis());

            Add(k.ResetTransform, c => owner.resetTransform());

            Add(k.ColormapPlasma, c => owner.setColorMap(ColorMapEnum.Plasma));
            Add(k.ColormapRainbow, c => owner.setColorMap(ColorMapEnum.Rainbow));
            Add(k.ColormapMagma, c => owner.setColorMap(ColorMapEnum.Magma));
            Add(k.ColormapInferno, c => owner.setColorMap(ColorMapEnum.Inferno));
            Add(k.ColormapViridis, c => owner.setColorMap(ColorMapEnum.Viridis));
            Add(k.ColormapCubeHelix, c => owner.setColorMap(ColorMapEnum.Cubehelix));
            Add(k.ColormapTurbo, c => owner.setColorMap(ColorMapEnum.Turbo));

            Add(k.CropSelection, c => owner.cropDataSet());
            Add(k.ResetCropSelection, c => owner.resetCropDataSet());
            Add(k.Teleport, c => owner.teleportToSelection());

            Add(k.MaskDisabled, c => owner.setMask(MaskMode.Disabled));
            Add(k.MaskEnabled, c => owner.setMask(MaskMode.Enabled));
            Add(k.MaskInverted, c => owner.setMask(MaskMode.Inverted));
            Add(k.MaskIsolated, c => owner.setMask(MaskMode.Isolated));

            Add(k.ProjectionMaximum, c => owner.setProjection(ProjectionMode.MaximumIntensityProjection));
            Add(k.ProjectionAverage, c => owner.setProjection(ProjectionMode.AverageIntensityProjection));

            Add(k.SamplingModeAverage, c => owner.SetSamplingMode(false));
            Add(k.SamplingModeMaximum, c => owner.SetSamplingMode(true));

            Add(k.PaintMode, c => owner.EnablePaintMode());
            Add(k.ExitPaintMode, c => owner.DisablePaintMode());
            Add(k.BrushAdd, c => owner.SetBrushAdditive());
            Add(k.BrushErase, c => owner.SetBrushSubtractive());

            Add(k.ShowMaskOutline, c => owner.ShowMaskOutline());
            Add(k.HideMaskOutline, c => owner.HideMaskOutline());

            Add(k.TakePicture, c => owner.TakePicture());
            Add(k.CursorInfo, c => owner.ToggleCursorInfo());

            Add(k.LinearScale, c => owner.ChangeScalingType(ScalingType.Linear));
            Add(k.LogScale, c => owner.ChangeScalingType(ScalingType.Log));
            Add(k.SqrtScale, c => owner.ChangeScalingType(ScalingType.Sqrt));

            Add(k.AddNewSource, c => owner.AddNewSource());
            Add(k.SetSourceId, c => owner.SetMaskValue());
            Add(k.AddToList, c => owner.AddSelectionToList());
            Add(k.Undo, c => owner.Undo());
            Add(k.Redo, c => owner.Redo());
            Add(k.SaveSubCube, c => owner.SaveSubCube());

            Add(k.PreviousSourceList, c => owner.PreviousSourceList());
            Add(k.NextSourceList, c => owner.NextSourceList());

            Add(k.EnterVideoMode, c =>
            {
                if (c.QuickMenu != null)
                {
                    c.QuickMenu.OpenVideoRecordingMenu();
                }
            });

            Add(k.RecordVideoPosition, c =>
            {
                if (c.VolumeInput != null)
                {
                    c.VolumeInput.AddNewLocation(c.VolumeInput.PrimaryHand);
                }
            });

            Add(k.ExportVideoScript, c =>
            {
                if (c.VideoRecordMenu != null)
                {
                    c.VideoRecordMenu.ExportToFile();
                }
            });

            return commands;
        }

        public static bool TryExecute(string phrase, IReadOnlyDictionary<string, IVoiceCommand> registry, IVoiceCommandContext context)
        {
            if (string.IsNullOrEmpty(phrase) || registry == null || context == null)
            {
                return false;
            }

            if (!registry.TryGetValue(phrase, out var command))
            {
                Debug.LogWarning($"Unknown voice command: {phrase}");
                return false;
            }

            command.Execute(context);
            return true;
        }
    }
}
