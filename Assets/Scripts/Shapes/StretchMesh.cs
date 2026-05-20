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
using UnityEngine;

/// <summary>
/// This class is just used to resize the mesh of the cuboid shape to be correct
/// </summary>
public class StretchMesh : MonoBehaviour
{
    public GameObject boundingBox; // Reference to the bounding box GameObject

    void Start()
    {
        // Get the MeshFilter component
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        BoxCollider boxCollider = GetComponent<BoxCollider>();


            Mesh mesh = meshFilter.mesh;
            Vector3[] vertices = mesh.vertices;
            Vector3 currentScale = transform.localScale;

            // Reset the scale to (1,1,1) to avoid double scaling
            transform.localScale = Vector3.one;

            // Adjust vertices to the current scale
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = Vector3.Scale(vertices[i], currentScale);
            }

            // Apply the modified vertices to the mesh
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();


        // Adjust the BoxCollider size
        boxCollider.size = currentScale;


        // Adjust the bounding box size

            boundingBox.transform.localScale = currentScale * 1.05f; // Slightly bigger than the prism
            boundingBox.transform.localPosition = Vector3.zero; // Ensure it's centered
    }
}