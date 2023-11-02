using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using Kitware.VTK;
using UnityEngine.Serialization;
using VolumeData;


public enum CalculationMethod {ContourFilter, MarchingBlocks} ;

public class SurfaceGenerator : MonoBehaviour
{

    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;
    public VolumeDataSetRenderer volumeDataSetRenderer;
    public double isovalue = 100.0f;
    public Color colorMesh = Color.red;
    public CalculationMethod calculationMethod = CalculationMethod.ContourFilter;

    public GameObject meshHolder;


    void OnEnable()
    {
        //meshRenderer = GetComponent<MeshRenderer>();
        IntPtr data = volumeDataSetRenderer.Data.FitsData;
        var dimensions = volumeDataSetRenderer.CubeDimensions;
        
        vtkImageData volumeData = Output3DDataToVTKImageData(data, dimensions[0], dimensions[1], dimensions[2]);
        Mesh mesh = MeshFromVTKImage(volumeData);
        meshFilter.mesh = mesh;
        meshRenderer.material = new Material(Shader.Find("Standard"));
        meshRenderer.material.color = colorMesh;
        meshRenderer.material.SetColor("_EmissionColor", colorMesh);
        meshRenderer.material.EnableKeyword("_EMISSION");
        meshRenderer.material.SetColor("_Color", colorMesh);
        meshRenderer.material.SetColor("_EmissionColor", Color.black);
        meshRenderer.material.SetColor("_SpecColor", colorMesh);
        meshFilter.mesh.RecalculateNormals();


    }



    /*
    // Create synthetic 3D volume data for demonstration
    vtkImageData volumeData = CreateSyntheticVolumeData();

    // Create a contour filter to extract isosurfaces
    vtkContourFilter contourFilter = new vtkContourFilter();
    contourFilter.SetInputConnection();
    contourFilter.SetInputData(volumeData);
    contourFilter.GenerateValues(1, 100.0, 100.0); // Set the isovalue (threshold) here
*/
    public unsafe vtkImageData CreateVTKImageDataFromIntPtr(IntPtr data, int x, int y, int z)
    {
        vtkImageData imageData = vtkImageData.New();
        int[] dimensions = { x, y, z };

        // Set the dimensions of the grid
        imageData.SetExtent(0, dimensions[0] - 1, 0, dimensions[1] - 1, 0, dimensions[2] - 1);


        // Set the scalar type (adjust as needed, e.g., VTK_UNSIGNED_CHAR, VTK_FLOAT, etc.)
        imageData.SetScalarTypeToFloat();

        // Set the number of scalar components (1 for grayscale)
        imageData.SetNumberOfScalarComponents(1);

        // Allocate memory for the scalars
        imageData.AllocateScalars();

        // Get a pointer to the scalars in the VTKImageData
        IntPtr vtkScalars = imageData.GetScalarPointer();

        // Copy data from the provided IntPtr to VTK's memory
        //Marshal.Copy(data, vtkScalars, 0, x * y * z);
        Buffer.MemoryCopy(data.ToPointer(), vtkScalars.ToPointer(), x * y * z, x * y * z);

        // Set the spacing and origin (you can adjust these as needed)
        imageData.SetSpacing(1.0, 1.0, 1.0);
        imageData.SetOrigin(0.0, 0.0, 0.0);

        // You can set other properties of the vtkImageData as needed


        OutputVTKImageDataToConsole(imageData);

        return imageData;
    }

    void OutputVTKImageDataToConsole(vtkImageData imageData)
    {
        int[] dimensions = imageData.GetDimensions();
        int numScalars = dimensions[0] * dimensions[1] * dimensions[2];

        for (int z = 0; z < dimensions[2]; z++)
        {
            for (int y = 0; y < dimensions[1]; y++)
            {
                for (int x = 0; x < dimensions[0]; x++)
                {
                    float scalarValue = imageData.GetScalarComponentAsFloat(x, y, z, 0);
                    Debug.Log("Scalar at (" + x + ", " + y + ", " + z + "): " + scalarValue);
                }
            }
        }
    }

    public Mesh MeshFromVTKImage(vtkImageData volumeData)
    {

        vtkPolyData isosurface;
        
        if (calculationMethod == CalculationMethod.ContourFilter)
        {
            vtkContourFilter contourFilter = vtkContourFilter.New();
            Debug.Log("Using ContourFilter");
            contourFilter.SetInput(volumeData); // Set your 3D volume data as input
            contourFilter.ComputeNormalsOn();
            contourFilter.SetValue(0, isovalue);
            contourFilter.Update();
            isosurface = contourFilter.GetOutput();
        }
        else
        {
            vtkMarchingCubes marchingCubes = vtkMarchingCubes.New();
            Debug.Log("Using MarchingBlocks");
            marchingCubes.SetInput(volumeData); // Set your 3D volume data as input
            marchingCubes.ComputeNormalsOn();
            marchingCubes.SetValue(0, isovalue);
            marchingCubes.Update();
            isosurface = marchingCubes.GetOutput();
        }

        // Create a Unity Mesh
        Mesh unityMesh = new Mesh();

        // Vertices
        vtkPoints points = isosurface.GetPoints();
        Vector3[] vertices = new Vector3[points.GetNumberOfPoints()];
        
        
        for (int i = 0; i < points.GetNumberOfPoints(); i++)
        {
            double[] coords = points.GetPoint(i);
            vertices[i] = new Vector3((float)coords[0], (float)coords[1], (float)coords[2]);
        }

        unityMesh.vertices = vertices;

        // Triangles
        vtkCellArray cells = isosurface.GetPolys();
        long numCells = cells.GetNumberOfCells();
        int[] triangles = new int[numCells * 3];
        int idx = 0;
        vtkIdList cellIds = new vtkIdList();
        cells.InitTraversal();
        while (cells.GetNextCell(cellIds) != 0)
        {
            if (cellIds.GetNumberOfIds() != 3)
            {
                // Handle non-triangular cells if needed
            }

            for (int i = 0; i < 3; i++)
            {
                triangles[idx] = (int)cellIds.GetId(i);
                idx++;
            }
        }

        unityMesh.triangles = triangles;
        return unityMesh;
        // Assign the Unity Mesh to a Unity MeshFilter or MeshRenderer component
    }

    public vtkImageData Output3DDataToVTKImageData(IntPtr cubeData, int xDim, int yDim, int zDim)
    {
    // Create a VTKImageData instance
        vtkImageData imageData = vtkImageData.New();

    // Set the dimensions (extent) of the image data
        int width = xDim;/* Your width */;
        int height = yDim;/* Your height */;
        int depth = zDim; /* Your depth */;
        imageData.SetDimensions(width, height, depth);
        imageData.SetScalarTypeToFloat();

// Set the origin and spacing (physical dimensions) of the image data
        double[] origin = { 0.0, 0.0, 0.0 };
        double[] spacing = { 1.0, 1.0, 1.0 };
        imageData.SetOrigin(origin[0], origin[1], origin[2]);
        imageData.SetSpacing(spacing[0], spacing[1], spacing[2]);
        //imageData.SetOrigin(origin);
        //imageData.SetSpacing(spacing);

        // Create a vtkFloatArray to store the voxel values
        vtkFloatArray voxelData = vtkFloatArray.New();

        float[] arrayHolder = new float[xDim * yDim * zDim];
        Marshal.Copy(cubeData, arrayHolder, 0, xDim * yDim * zDim);
        
        // Populate the voxelData array with your 3D data values (flattened to a 1D array)
        for (int z = 0; z < depth; z++) {
            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    int index = z * width * height + y * width + x;
                    float value = arrayHolder[index]/* Your 3D data value at (x, y, z) */;
                    voxelData.InsertNextValue(value);
                }
            }
        }

        // Set the voxelData as the PointData for imageData
        imageData.GetPointData().SetScalars(voxelData);

        // Optionally, set the data range, data type, and other properties for imageData
        imageData.GetPointData().GetScalars().SetNumberOfComponents(1); // Assuming single-component data
        imageData.GetPointData().GetScalars().SetName("VoxelData"); // Set the array name
        
       // Debug.Log("Calculated VTKImageData: " + imageData.Getsca);
        
        return imageData;
        
    }
}