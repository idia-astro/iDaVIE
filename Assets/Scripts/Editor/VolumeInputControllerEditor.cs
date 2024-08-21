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
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VolumeInputController))]
public class VolumeInputControllerEditor : Editor
{
    private Vector3Int _targetCenter;
    private Vector3Int _boundsMin;
    private Vector3Int _boundsMax;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        VolumeInputController volumeInputController = (VolumeInputController) target;
        if (volumeInputController)
        {
            GUILayout.Space(20);
            _targetCenter = EditorGUILayout.Vector3IntField("Point Voxel", _targetCenter);
            if (GUILayout.Button("Teleport"))
            {
                volumeInputController.Teleport(_targetCenter - (0.5f * Vector3.one), _targetCenter + (0.5f * Vector3.one));
            }

            GUILayout.Space(20);
            _boundsMin = EditorGUILayout.Vector3IntField("Bounds Min", _boundsMin);
            _boundsMax = EditorGUILayout.Vector3IntField("Bounds Max", _boundsMax);
            if (GUILayout.Button("Teleport"))
            {
                volumeInputController.Teleport(_boundsMin - (0.5f * Vector3.one), _boundsMax + (0.5f * Vector3.one));
            }
        }
    }
}