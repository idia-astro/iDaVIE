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
using DataFeatures;
using UnityEditor;


[CustomEditor(typeof(FeatureSetRenderer))]
public class FeatureSetRendererEditor : Editor
{
    private int selectionTarget;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        FeatureSetRenderer featureSetRenderer = (FeatureSetRenderer) target;
        if (GUILayout.Button("Feature Set On"))
        {
            featureSetRenderer.SetVisibilityOn();
        }
        if (GUILayout.Button("Feature Set Off"))
        {
            featureSetRenderer.SetVisibilityOff();
        }
        selectionTarget = EditorGUILayout.IntField("Feature ID", selectionTarget);
        GUILayout.BeginHorizontal();
        GUI.enabled = (selectionTarget > 0 && selectionTarget <= featureSetRenderer.FeatureList.Count);
        if (GUILayout.Button("Select feature"))
        {
            var feature = featureSetRenderer.FeatureList[selectionTarget - 1];
            if (feature != null)
            {
                featureSetRenderer.SelectFeature(feature);
            }
        }

        GUILayout.EndHorizontal();
        GUI.enabled = true;
    }
}