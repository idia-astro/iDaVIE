/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2026 IDIA, INAF-OACT
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
using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Copies the video script templates from Scripts/VideoScripts into the build's Outputs/VideoScripts/Templates folder,
/// so they are available from the video script file browser.
/// </summary>
public class CopyVideoScriptTemplates : IPostprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPostprocessBuild(BuildReport report)
    {
        var projectRoot = Directory.GetParent(Application.dataPath).FullName;
        var sourceDir = Path.Combine(projectRoot, "Scripts", "VideoScripts");
        var buildDir = Path.GetDirectoryName(report.summary.outputPath);
        var targetDir = Path.Combine(buildDir, "Outputs", "VideoScripts", "Templates");

        if (!Directory.Exists(sourceDir))
        {
            Debug.LogWarning($"Video script templates not found at {sourceDir}; skipping copy.");
            return;
        }

        Directory.CreateDirectory(targetDir);
        foreach (var file in Directory.GetFiles(sourceDir, "*.idvs"))
        {
            File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), overwrite: true);
        }
        Debug.Log($"Copied video script templates to {targetDir}.");
    }
}
