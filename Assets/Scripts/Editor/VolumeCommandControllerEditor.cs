/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 Inter-University Institute for Data Intensive Astronomy
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
using UnityEngine;
using System.Collections;
using VolumeData;
using UnityEditor;


[CustomEditor(typeof(VolumeCommandController))]
public class VolumeCommandControllerEditor : Editor
{

    public ColorMapEnum colormap;

    public override void OnInspectorGUI()
    {

        DrawDefaultInspector();
        VolumeCommandController volumeCommandController = (VolumeCommandController)target;

        EditorGUILayout.LabelField("Transform");
        if (GUILayout.Button("Reset Transform"))
        {
            volumeCommandController.resetTransform();
        }
        EditorGUILayout.LabelField("Threshold");
        if (GUILayout.Button("Reset Threshold"))
        {
            volumeCommandController.resetThreshold();
        }

        if (GUILayout.Button("Edit Min Threshold"))
        {
            volumeCommandController.startThresholdEditing(false);
        }

        if (GUILayout.Button("Edit Max Threshold"))
        {
            volumeCommandController.startThresholdEditing(true);
        }

        if (GUILayout.Button("Save Threshold"))
        {
            volumeCommandController.endThresholdEditing();
        }

        EditorGUILayout.LabelField("Color Map");

        colormap = (ColorMapEnum)EditorGUILayout.EnumPopup("Color:", colormap);

        if (GUILayout.Button("Change ColorMap"))
        {
            volumeCommandController.setColorMap(colormap);
        }

        EditorGUILayout.LabelField("Cropping");

        if (GUILayout.Button("Crop to Region"))
        {
            volumeCommandController.cropDataSet();
        }

        if (GUILayout.Button("Reset Crop"))
        {
            volumeCommandController.resetCropDataSet();
        }

        EditorGUILayout.LabelField("Masking");

        if (GUILayout.Button("Mask On"))
        {
            volumeCommandController.setMask(MaskMode.Enabled);
        }

        if (GUILayout.Button("Mask Off"))
        {
            volumeCommandController.setMask(MaskMode.Disabled);
        }

        if (GUILayout.Button("Mask Invert"))
        {
            volumeCommandController.setMask(MaskMode.Inverted);
        }

        if (GUILayout.Button("Mask Isolate"))
        {
            volumeCommandController.setMask(MaskMode.Isolated);
        }
        
        if (GUILayout.Button("Projection Maximum"))
        {
            volumeCommandController.setProjection(ProjectionMode.MaximumIntensityProjection);
        }
        
        if (GUILayout.Button("Projection Average"))
        {
            volumeCommandController.setProjection(ProjectionMode.AverageIntensityProjection);
        }
        
        if (GUILayout.Button("Sampling mode Maximum"))
        {
            volumeCommandController.SetSamplingMode(true);
        }
        
        if (GUILayout.Button("Sampling mode Average"))
        {
            volumeCommandController.SetSamplingMode(false);
        }
    }
}