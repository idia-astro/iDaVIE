//This class is just used to resize the mesh of the cuboid shape to be correct
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StretchMesh : MonoBehaviour
{
    /// <summary>
    /// Reference to the bounding box <see cref="GameObject"/> used for visualizing or calculating bounds.
    /// </summary>
    public GameObject boundingBox; // Reference to the bounding box GameObject

    /// <summary>
    /// Initializes the mesh and bounding box for the shape by scaling vertices and adjusting colliders.
    /// </summary>
    /// <remarks>
    /// - Resets the transform scale to (1,1,1) temporarily to avoid double scaling.
    /// - Scales mesh vertices according to the original transform scale.
    /// - Recalculates mesh bounds and normals for accurate rendering.
    /// - Adjusts the BoxCollider size to match the current scale.
    /// - Sets the bounding box to slightly larger than the mesh (scaled by 1.05) and centers it.
    /// </remarks>
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