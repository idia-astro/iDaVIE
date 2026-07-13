/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 */
using System.Collections.Generic;
using UnityEngine;

namespace VolumeData.Voice
{
    public sealed class VoiceCommandContext : IVoiceCommandContext
    {
        private readonly VolumeCommandController _owner;
        private readonly List<VolumeDataSetRenderer> _dataSets;

        public VoiceCommandContext(VolumeCommandController owner, List<VolumeDataSetRenderer> dataSets)
        {
            _owner = owner;
            _dataSets = dataSets;
        }

        public VolumeDataSetRenderer ActiveDataSet => _owner.GetActiveDataSetInternal();
        public IReadOnlyList<VolumeDataSetRenderer> DataSets => _dataSets;
        public VolumeInputController VolumeInput => _owner.VolumeInput;
        public QuickMenuController QuickMenu => _owner.QuickMenuController;
        public PaintMenuController PaintMenu => _owner.PaintMenuController;
        public FeatureMenuController FeatureMenu => _owner.featureMenuController;
        public VideoRecordMenuController VideoRecordMenu => _owner.VideoRecordMenu;

        public void RefreshActiveDataSet()
        {
            _owner.RefreshActiveDataSetInternal();
        }

        public bool RequireMask()
        {
            if (ActiveDataSet?.Mask != null)
            {
                return true;
            }

            ShowMissingMaskError();
            return false;
        }

        public void ShowMissingMaskError()
        {
            ToastNotification.ShowError("No mask loaded for this functionality!");
        }
    }
}
