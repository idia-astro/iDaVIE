/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 */
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VolumeData.Voice
{
    /// <summary>
    /// Isolates legacy desktop UI hierarchy access for threshold/ratio sync from voice commands.
    /// </summary>
    public static class VoiceDesktopUiSync
    {
        public static void SyncThresholdSliders(GameObject mainCanvassDesktop, VolumeDataSetRenderer dataSet)
        {
            if (mainCanvassDesktop == null || dataSet == null)
            {
                return;
            }

            var settings = FindSettings(mainCanvassDesktop);
            if (settings == null)
            {
                return;
            }

            var thresholdContainer = settings.Find("Threshold_container");
            if (thresholdContainer == null)
            {
                return;
            }

            var minSlider = thresholdContainer.Find("Threshold_min")?.Find("Slider")?.GetComponent<Slider>();
            var maxSlider = thresholdContainer.Find("Threshold_max")?.Find("Slider")?.GetComponent<Slider>();
            if (minSlider != null)
            {
                minSlider.value = dataSet.ThresholdMin;
            }

            if (maxSlider != null)
            {
                maxSlider.value = dataSet.ThresholdMax;
            }
        }

        public static void ResetRatioDropdown(GameObject mainCanvassDesktop)
        {
            if (mainCanvassDesktop == null)
            {
                return;
            }

            var settings = FindSettings(mainCanvassDesktop);
            var ratioDropdown = settings?.Find("Ratio_container")?.Find("Ratio_Dropdown")?.GetComponent<TMP_Dropdown>();
            if (ratioDropdown != null)
            {
                ratioDropdown.value = 0;
            }
        }

        private static Transform FindSettings(GameObject mainCanvassDesktop)
        {
            return mainCanvassDesktop.transform
                .Find("RightPanel/Panel_container/RenderingPanel/Rendering_container/Viewport/Content/Settings");
        }
    }
}
