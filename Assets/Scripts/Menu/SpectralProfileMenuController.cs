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
using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using VolumeData;

public class SpectralProfileMenuController : MonoBehaviour
{
    
    [SerializeField] private Image Img;
    public DataAnalysis.SourceStats SelectedSourceStats;
    
    [SerializeField] private GameObject VolumeDataSetManager;
    private VolumeDataSetRenderer _activeDataSet;
    
    void Start()
    {
        _activeDataSet = getFirstActiveDataSet();
    }
    
    public void UpdateUI(Sprite sprite)
    {
        if (Img != null)
        {
            Img.sprite = sprite;
        }
    }

    /// <summary>
    /// Function to export the spectral profile data to a CSV file.
    /// </summary>
    public void ExportProfileToCSV()
    {
        if (SelectedSourceStats.spectralProfilePtr == IntPtr.Zero)
        {
            ToastNotification.ShowError("No spectral profile data available!");
            return;
        }
        double[] spectralProfileArray = new double[SelectedSourceStats.spectralProfileSize];
        Marshal.Copy(SelectedSourceStats.spectralProfilePtr, spectralProfileArray, 0, SelectedSourceStats.spectralProfileSize);
        var directory = new DirectoryInfo(Application.dataPath);
        var directoryPath = Path.Combine(directory.Parent.FullName, "Outputs/SpectralProfiles");
        try
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var filename = string.Format("SpectralProfile_{0}.csv", DateTime.Now.ToString("yyyyMMdd_Hmmssf"));
            var path = Path.Combine(directoryPath, filename);
            using (StreamWriter writer = new StreamWriter(path))
            {
                writer.WriteLine($"{_activeDataSet.Data.GetAxisUnit(3)},{_activeDataSet.Data.GetPixelUnit()}");
                for (int i = 0; i < SelectedSourceStats.spectralProfileSize; i++)
                {
                    AstTool.Transform3D(_activeDataSet.AstFrame, 0, 0, i, 1,  out _, out _, out var spectralValue);
                    writer.WriteLine($"{spectralValue},{spectralProfileArray[i]}");
                }
            }
            ToastNotification.ShowSuccess($"Spectral profile saved as {filename}");
        }
        
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            ToastNotification.ShowError("Error saving spectral profile!");
        }
    }

    private VolumeDataSetRenderer getFirstActiveDataSet()
    {
        VolumeDataSetRenderer[] dataSets = VolumeDataSetManager.GetComponentsInChildren<VolumeDataSetRenderer>(true);
        foreach (VolumeDataSetRenderer dataSet in dataSets)
        {
            if (dataSet.gameObject.activeSelf)
            {
                return dataSet;
            }
        }

        return null;
    }
    
}
