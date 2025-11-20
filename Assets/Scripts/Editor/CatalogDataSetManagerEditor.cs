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
using CatalogData;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CatalogDataSetManager))]
public class CatalogDataSetManagerEditor : Editor
{

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        CatalogDataSetManager catalogDataSetManager = (CatalogDataSetManager)target;

        /*
        for (int i = 1; i < catalogDataSetManager.NumberDataSets; i++)
        {
            catalogDataSetManager.SelectNextSet();
            catalogDataSetManager.SetActiveSetVisibility(false);
        }
        catalogDataSetManager.SelectNextSet();
        */

        EditorGUILayout.TextField("Active Data Set:", catalogDataSetManager.GetActiveSetName());


        if (GUILayout.Button("Select Next Set"))
        {
            catalogDataSetManager.SelectNextSet();
        }

        if (GUILayout.Button("Set Visibility On"))
        {
            catalogDataSetManager.SetActiveSetVisibility(true);
        }


        if (GUILayout.Button("Set Visibility Off"))
        {
            catalogDataSetManager.SetActiveSetVisibility(false);
        }

        if (GUILayout.Button("Reset Position"))
        {
            /*
            Transform[] children = catalogDataSetManager.transform.GetComponentsInChildren<Transform>();

            foreach (Transform t in children)
                t.position = catalogDataSetManager.transform.TransformPoint(new Vector3(0, 0, 0));
                */
            CatalogDataSetRenderer[] catalogDataSetRenderers = catalogDataSetManager.GetComponentsInChildren<CatalogDataSetRenderer>();
            foreach (CatalogDataSetRenderer catalogDataSetRenderer in catalogDataSetRenderers)
            {
                catalogDataSetRenderer.resetLocalPosition();
            }
        }

    }
}