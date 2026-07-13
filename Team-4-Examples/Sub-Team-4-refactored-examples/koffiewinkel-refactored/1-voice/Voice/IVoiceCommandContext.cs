/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 */
using System.Collections.Generic;

namespace VolumeData.Voice
{
    /// <summary>
    /// Dependencies voice commands need. Keeps command classes free of scene lookups.
    /// </summary>
    public interface IVoiceCommandContext
    {
        VolumeDataSetRenderer ActiveDataSet { get; }
        IReadOnlyList<VolumeDataSetRenderer> DataSets { get; }
        VolumeInputController VolumeInput { get; }
        QuickMenuController QuickMenu { get; }
        PaintMenuController PaintMenu { get; }
        FeatureMenuController FeatureMenu { get; }
        VideoRecordMenuController VideoRecordMenu { get; }

        void RefreshActiveDataSet();
        bool RequireMask();
        void ShowMissingMaskError();
    }
}
