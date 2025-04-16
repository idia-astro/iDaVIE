//Assigned to the display which contains the raw image showing the user the slices of the cube
//Assigned there because the display of this image marks the entering of the paint mode in VR first and it makes manipulation of the texture easy
//Light must ignore slice indicator
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TMPro;
using Valve.Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
using VolumeData;
using Valve;
using Valve.VR;
using DataFeatures;
using VoTableReader;
using System.Linq;
using SFB;
using UnityEngine.EventSystems;
using Unity.Collections;
using Unity.Mathematics;

/*TODO

Button to enter paint mode
Brush for adding and removing

fix the deleting mask
Add threshholds (as found in render panel)

I need to get the _maskDataSet from the volumedatarenderer. i must be in paint mode first though.
*/

public class DesktopPaintController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{

    List<Vector2> selectionPolyList = new List<Vector2>{};  //List for the polygon selection
    public GameObject markerPrefab;  //marker prefab to show where the user clicks
    public GameObject volumeDatasetManager;   //for assigning the cameras (might have to do this in other class)
    private VolumeDataSetRenderer dataRenderer;
    private VolumeDataSet dataSet;  //the dataset that has been loaded fromm the file
    private VolumeDataSet maskSet;  //mask
    private Texture3D regionCube;  //texture3D cube of texture rfloat (used to set greyscale)
    private Texture2D currentRegionSlice;  //to find the coordinates of the selection
    private float[,] currentMaskSlice;  
    private Texture3D maskCube;  //texture3d cube of texture r16 (value of mask i think)
    //Actual VolumeDataSet mask cube - for writing mask
    public Dictionary<int, DataAnalysis.SourceStats> SourceStatsDict { get; private set; }

    public GameObject sliceCameraPrefab;
    private GameObject sliceCamera;
    public GameObject iDaVIELogo;
    public GameObject selectionContainer;
    public GameObject waitingContainer;
    private CameraTransform cameraX = new CameraTransform();
    private CameraTransform cameraY = new CameraTransform();
    private CameraTransform cameraZ = new CameraTransform();

    public Text sliceText;  //the text displaying the current slice
    private RawImage rawImage;  
    private int prevIndex = 0;
    private CanvassDesktop canvassDesktop;  //could be changed to public
    public GameObject colorMapDropdown;
    public GameObject sliceSlider;
    public GameObject axisDropdown;
    public GameObject selectionModeToggle;
    private Image selectionModeImage;
    private Text selectionModeText;

    private int axis;  //x = 0, y = 1, z = 0
    private int sliceIndex;  //of the region cube
    private float maxVal;  //max and min value of region cube
    private float minVal;
    private short sourceID = 1000;
    private short maxID = 1000;
    private List<Vector3Int> maskVoxels = new List<Vector3Int>{};
    private List<Vector3Int> lastMaskVoxels = new List<Vector3Int>{};
    private bool subtracted = false;
    private int maskCount = 0;  //no point in removing mask if there are no mask voxels
    private bool additive;
    private bool painted = false;

    //of the region cube. For ensuring slices are bound
    private int cubeWidth;
    private int cubeHeight;
    private int cubeDepth;

    private bool isDrawing = false;

    public Button clearAllButton;
    public Button resetButton;  //Reset temp selection button
    public Button selectionButton;  //make temp selection button
    public TextMeshProUGUI selectionButtonText;
    public GameObject saveMessage;
    public TMP_Dropdown sourceIDDropdown;

    public GameObject sliceIndicatorPrefab;
    private GameObject sliceIndicator;

    //Color Map
    private ColorMapEnum colorMapEnum;
    public Texture2D colormap;
    public int colormapWidth = 1080;
    public int colormapHeight = 800;
    public int colorMapRowHeight = 10;

    //Zooming
    public RectTransform imageRect;
    public float zoomSpeed = 0.1f;
    public float minZoom = 0.5f;
    public float maxZoom = 3f;

    private Rect originalUVRect;
    private float currentZoom = 1f;

    struct CameraTransform
    {
        public Vector3 position;
        public Quaternion rotation;
    }


    /*
    OnEnable we bring in the canvassDesktop object and get the rawimage of the display.
    That object is used to get the activedata set where the MaxVal and MinVal are fetched (of the voxel values for the cube).
    The region cube is fetched which is the raw cube without all the features and it's height, width and depth are fetched.
    The region cube sampling checks are already done so do not need to be repeated. (I think).
    Axis and sliceindex are set to 0 (and selecting to false) here in case a new file is loaded so they will then again be set to 0.
    */
    void OnEnable()
    {
        if(canvassDesktop == null)
        {
            canvassDesktop = FindObjectOfType<CanvassDesktop>();
        }

        rawImage = GetComponent<RawImage>();

        if (imageRect == null)
            imageRect = GetComponent<RectTransform>();

         originalUVRect = rawImage.uvRect;

        dataRenderer = canvassDesktop.activeDataSet();

        //dataSet = canvassDesktop.getActiveDataSet();
        dataSet = dataRenderer.Data;
        regionCube = dataSet.RegionCube;
        maxVal = dataSet.MaxValue;
        minVal = dataSet.MinValue;

        //maskSet = canvassDesktop.getActiveMaskSet();
        maskSet = dataRenderer.Mask;
        maskCube = maskSet.RegionCube;

        cubeWidth = regionCube.width;
        cubeHeight = regionCube.height;
        cubeDepth = regionCube.depth;

        axis = 2;
        sliceIndex = 0;
        additive = true;
        selectionModeImage = selectionModeToggle.transform.GetChild(0).gameObject.GetComponent<Image>();
        selectionModeImage.color = Color.green;
        selectionModeText = selectionModeToggle.transform.GetChild(1).gameObject.GetComponent<Text>();
        selectionModeText.text = "Additive";

        //colorMapEnum = dataRenderer.ColorMap;
        //Debug.Log("Color map is: " + colorMapEnum.GetHashCode() + " " + colorMapEnum);

        SpawnCameras();
        SetColorMap();
        SetSliceSlider();
        SpawnSliceIndicator();
        ResetSlice();  //Call texture straight away
        iDaVIELogo.SetActive(false);
        sourceIDDropdown.onValueChanged.AddListener(OnDropDownFieldValueChanged);

        var sourceArray = DataAnalysis.GetMaskedSourceArray(maskSet.FitsData, maskSet.XDim, maskSet.YDim, maskSet.ZDim);
        if(sourceArray.Count > 0) {
            sourceIDDropdown.options.Clear();
            foreach (var source in sourceArray) {
                sourceIDDropdown.options.Add(new TMP_Dropdown.OptionData(""+source.maskVal));
                if(source.maskVal > maxID) maxID = source.maskVal;
            }
            sourceIDDropdown.value = 0;
            sourceID = short.Parse(sourceIDDropdown.options[0].text);
            sourceIDDropdown.RefreshShownValue();
        }
    }

    void Update()
    {
        sliceIndex = (int)sliceSlider.GetComponent<Slider>().value;

        if(maskCount > 0) clearAllButton.interactable = true;
        else clearAllButton.interactable = false;

        if(dataRenderer.IsFullResolution) {
            selectionContainer.SetActive(true);
            waitingContainer.SetActive(false);
        }
        else {
            selectionContainer.SetActive(false);
            waitingContainer.SetActive(true);
            return;
        }

        //should be a second script to handle all of this (add timer so next slice goes faster when held for longer)
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            NextSlice();
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            PreviousSlice();
        }

        if(Input.GetKeyDown(KeyCode.Space))
        {
            if(IsPolygonClosed(selectionPolyList))
            {
                if(additive) ApplyMask(true);
                else SubtractiveSelection(true);
            }
            CompletePolygon();
        }

        if(Input.GetKeyDown(KeyCode.C))
        {
            ClearMaskButton();
        }

        if(Input.GetKeyDown(KeyCode.P))
        {
            GetPrevMask();
        }

        if(Input.GetKeyDown(KeyCode.R))
        {
            ResetSelectionButton();
        }

        if(Input.GetKeyDown(KeyCode.X))
        {
            ChangeAxis(0);
            axisDropdown.GetComponent<TMP_Dropdown>().value = 0;
        }

        if(Input.GetKeyDown(KeyCode.Y))
        {
            ChangeAxis(1);
            axisDropdown.GetComponent<TMP_Dropdown>().value = 1;
        }

        if(Input.GetKeyDown(KeyCode.Z))
        {
            ChangeAxis(2);
            axisDropdown.GetComponent<TMP_Dropdown>().value = 2;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            if (!IsMouseOverImage()) return;

            scroll = -scroll;
            Vector2 mousePosition = Input.mousePosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rawImage.rectTransform, mousePosition, null, out Vector2 localMousePos);

            // Normalize the mouse position to UV space (0 to 1 range)
            float uvMouseX = Mathf.InverseLerp(-rawImage.rectTransform.rect.width / 2, rawImage.rectTransform.rect.width / 2, localMousePos.x);
            float uvMouseY = Mathf.InverseLerp(-rawImage.rectTransform.rect.height / 2, rawImage.rectTransform.rect.height / 2, localMousePos.y);

            // Update zoom level while preventing zooming out beyond the original size
            float newZoom = Mathf.Clamp(currentZoom - scroll * zoomSpeed, 1f, maxZoom);
            float zoomFactor = newZoom / currentZoom;
            currentZoom = newZoom;

            // Adjust the UV Rect to zoom in at the mouse position
            float newWidth = rawImage.uvRect.width / zoomFactor;
            float newHeight = rawImage.uvRect.height / zoomFactor;

            // Prevent zooming out beyond the original image bounds
            if (newWidth > 1f) newWidth = 1f;
            if (newHeight > 1f) newHeight = 1f;

            float newX = rawImage.uvRect.x + (rawImage.uvRect.width - newWidth) * uvMouseX;
            float newY = rawImage.uvRect.y + (rawImage.uvRect.height - newHeight) * uvMouseY;

            // Ensure the UV rect stays within (0,0,1,1)
            newX = Mathf.Clamp(newX, 0f, 1f - newWidth);
            newY = Mathf.Clamp(newY, 0f, 1f - newHeight);

            rawImage.uvRect = new Rect(newX, newY, newWidth, newHeight);
        }

    }

    private void OnDropDownFieldValueChanged(int index)
    {
        sourceID = short.Parse(sourceIDDropdown.options[index].text);
        HighlightMask();
    }

    public void AddSource() {
        maxID++;
        sourceID = maxID;
        sourceIDDropdown.options.Add(new TMP_Dropdown.OptionData(""+maxID));
        sourceIDDropdown.value = sourceIDDropdown.options.Count - 1;
        sourceIDDropdown.RefreshShownValue();
        HighlightMask();
    }

    private bool IsMouseOverImage()
    {
        RectTransform rectTransform = rawImage.rectTransform;
        Vector2 localMousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, Input.mousePosition, null, out localMousePos);

        return rectTransform.rect.Contains(localMousePos);
    }

    void OnDisable()
    {
        Destroy(sliceIndicator);
        Destroy(sliceCamera);
        sliceSlider.GetComponent<Slider>().value = 0;
    }


//Texture updates

    //For initially creating the texture - called when tab is clicked
    public void StartRegionCubeTexture()
    {
        //Debug.Log("The min value is " + minVal);
        //Debug.Log("The max value is " + maxVal);
        //Debug.Log("The dimensions are " + regionCube.width + "x" + regionCube.height + "x" + regionCube.depth);

        /*
        Debug.Log("The min mask value is " + maskSet.MinValue);
        Debug.Log("The max mask value is " + maskSet.MaxValue);
        Debug.Log("The mask dimensions are " + maskSet.RegionCube.width + "x" + maskSet.RegionCube.height + "x" + maskSet.RegionCube.depth);
        */
        UpdateTexture();

    }

    //Updates the displayed texture with the current settings
    public void UpdateTexture()
    {
        currentRegionSlice = GetSlice(regionCube, axis, sliceIndex);
        currentMaskSlice = GetFloatSlice(maskCube, axis, sliceIndex);
        //Debug.Log("Current slice dimensions: " + currentRegionSlice.width + "x" + currentRegionSlice.height);
        rawImage.texture = currentRegionSlice;
        HighlightMask();
        sliceText.text = "" + (sliceIndex + 1); //+1 so it does not start on 0
    }

    //return the slice from a texture3d based on the selected axis and index
    public Texture2D GetSlice(Texture3D texture3D, int axis, int sliceIndex)
    {
        int width = texture3D.width;
        int height = texture3D.height;
        int depth = texture3D.depth;

        // Ensure the slice index is within the valid range
        if (sliceIndex < 0 || (axis == 0 && sliceIndex >= width) || (axis == 1 && sliceIndex >= height) || (axis == 2 && sliceIndex >= depth))
        {
            Debug.LogError("Slice index out of range");
            return null;
        }

        // Read the entire 3D texture data into a NativeArray<float>
        NativeArray<float> volumeData = texture3D.GetPixelData<float>(0);
        //Debug.Log("Native array (region) size: " + volumeData.Length);

        // Create the output texture
        Texture2D slice;;
        Color[] sliceData;
        int size;
        int indexMap = 80 - colorMapEnum.GetHashCode();

        switch (axis)
        {
            case 0: // x-axis
                slice = new Texture2D(height, depth, TextureFormat.RGBA32, false);
                size = height*depth;
                sliceData = new Color[size];
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        int index = sliceIndex + y * width + z * width * height;
                        float normalizedData = (volumeData[index] - minVal)/(maxVal - minVal);
                        //Color rgbColor = new Color(normalizedData, normalizedData, normalizedData);
                        //Debug.Log("The normalized value is: " + normalizedData);
                        Color rgbColor = GetColorFromColormap(indexMap, normalizedData); //colorMapEnum.GetHashCode(), normalizedData
                        sliceData[y + z * height] = rgbColor;
                    }
                }
                break;
            
            case 1: // y-axis
                slice = new Texture2D(width, depth, TextureFormat.RGBA32, false);
                size = width * depth;
                sliceData = new Color[size];
                for (int x = 0; x < width; x++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        int index = x + sliceIndex * width + z * width * height;
                        float normalizedData = (volumeData[index] - minVal)/(maxVal - minVal);
                        //Color rgbColor = new Color(normalizedData, normalizedData, normalizedData);
                        Color rgbColor = GetColorFromColormap(indexMap, normalizedData);
                        sliceData[x + z * width] = rgbColor;
                    }
                }
                break;

            case 2: // z-axis
                slice = new Texture2D(width, height, TextureFormat.RGBA32, false);
                size = width * height;
                sliceData = new Color[size];
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        int index = x + y * width + sliceIndex * width * height;
                        float normalizedData = (volumeData[index] - minVal)/(maxVal - minVal);
                        //Color rgbColor = new Color(normalizedData, normalizedData, normalizedData);
                        Color rgbColor = GetColorFromColormap(indexMap, normalizedData);
                        sliceData[x + y * width] = rgbColor;
                    }
                }
                break;
            
            default:
                Debug.LogError("Invalid axis specified. Use 0 for x-axis, 1 for y-axis, and 2 for z-axis.");
                return null;
        }

        // Dispose of the NativeArray to avoid memory leaks
        volumeData.Dispose();

        // Apply the pixel data to the slice texture
        slice.SetPixels(sliceData, 0);
    
        // Dispose of the sliceData NativeArray
        //sliceData.Dispose();

        // Apply the changes to the Texture2D
        slice.Apply();

        return slice;
    }

    public float[,] GetFloatSlice(Texture3D texture3D, int axis, int sliceIndex)
    {
        int width = texture3D.width;
        int height = texture3D.height;
        int depth = texture3D.depth;

        // Ensure the slice index is within the valid range
        if (sliceIndex < 0 || (axis == 0 && sliceIndex >= width) || (axis == 1 && sliceIndex >= height) || (axis == 2 && sliceIndex >= depth))
        {
            Debug.LogError("Slice index out of range");
            return null;
        }

        // Read the entire 3D texture data into a NativeArray<Half> for r16 format
        NativeArray<half> volumeData = texture3D.GetPixelData<half>(0);

        float[,] sliceData;

        switch (axis)
        {
            case 0: // x-axis
                sliceData = new float[height, depth];
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        int index = sliceIndex + y * width + z * width * height;
                        sliceData[y, z] = (float)volumeData[index];
                    }
                }
                break;

            case 1: // y-axis
                sliceData = new float[width, depth];
                for (int x = 0; x < width; x++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        int index = x + sliceIndex * width + z * width * height;
                        sliceData[x, z] = (float)volumeData[index];
                    }
                }
                break;

            case 2: // z-axis
                sliceData = new float[width, height];
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        int index = x + y * width + sliceIndex * width * height;
                        sliceData[x, y] = (float)volumeData[index];
                    }
                }
                break;

            default:
                Debug.LogError("Invalid axis specified. Use 0 for x-axis, 1 for y-axis, and 2 for z-axis.");
                return null;
        }

        // Dispose of the NativeArray to avoid memory leaks
        volumeData.Dispose();

        return sliceData;
    }

    public void HighlightMask()
    {
        int arrayX = 0;
        int arrayY = 0;

        if(axis == 0)
        {
            arrayX = cubeHeight;
            arrayY = cubeDepth;
        }

        if(axis == 1)
        {
            arrayX = cubeWidth;
            arrayY = cubeDepth;
        }

        if(axis == 2)
        {
            arrayX = cubeWidth;
            arrayY = cubeHeight;
        }

        maskCount = 0;
        for(int i = 0; i < arrayX; i++)
        {
            for(int j = 0; j < arrayY; j++)
            {
                if(currentMaskSlice[i,j] > 0)
                {
                    maskCount++;
                    //Assign prev mask here
                }
            }
        }

        //Debug.Log("Mask check done");

        if(maskCount > 0)
        {
            Texture2D overlayTexture = CreateOverlayTexture(currentRegionSlice, currentMaskSlice, currentRegionSlice.width, currentRegionSlice.height);
            currentRegionSlice = overlayTexture;
            rawImage.texture = currentRegionSlice;
        }
        
    }

    Texture2D CreateOverlayTexture(Texture2D baseTexture, float[,] overlaySource, int width, int height)
    {
        Texture2D overlayTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        // Initialize the overlay with the base texture
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color baseColor = baseTexture.GetPixel(x, y);
                overlayTexture.SetPixel(x, y, baseColor);
            }
        }

        bool[,] visited = new bool[width, height];
        List<List<Vector2Int>> regions = new List<List<Vector2Int>>();

        // Identify contiguous regions
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (overlaySource[x, y] > 0 && !visited[x, y])
                {
                    List<Vector2Int> region = new List<Vector2Int>();
                    FindRegion(overlaySource, x, y, visited, region);
                    regions.Add(region);
                }
            }
        }

        Color maskColor = new Color(0.8018868f, 0.5030705f, 0.5030705f);
        // Draw outlines and internal grid lines
        foreach (var region in regions)
        {
            DrawOutlineAndGrid(overlayTexture, region, overlaySource, maskColor);
        }

        overlayTexture.Apply();
        return overlayTexture;
    }

    void FindRegion(float[,] texture, int startX, int startY, bool[,] visited, List<Vector2Int> region)
    {
        int width = texture.GetLength(0);
        int height = texture.GetLength(1);
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(startX, startY));

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            int x = current.x;
            int y = current.y;

            if (x < 0 || x >= width || y < 0 || y >= height || visited[x, y] || texture[x, y] <= 0)
                continue;

            visited[x, y] = true;
            region.Add(current);

            queue.Enqueue(new Vector2Int(x + 1, y));
            queue.Enqueue(new Vector2Int(x - 1, y));
            queue.Enqueue(new Vector2Int(x, y + 1));
            queue.Enqueue(new Vector2Int(x, y - 1));
        }
    }

    void DrawOutlineAndGrid(Texture2D texture, List<Vector2Int> region, float[,] overlaySource, Color color)
    {
        int width = texture.width;
        int height = texture.height;
        HashSet<Vector2Int> borderPixels = new HashSet<Vector2Int>();
        Color currentSourceColor = Color.yellow;

        foreach (var pixel in region)
        {
            int x = pixel.x;
            int y = pixel.y;

            bool isBorder = false;

            if (x > 0 && overlaySource[x - 1, y] == 0) isBorder = true;
            if (x < width - 1 && overlaySource[x + 1, y] == 0) isBorder = true;
            if (y > 0 && overlaySource[x, y - 1] == 0) isBorder = true;
            if (y < height - 1 && overlaySource[x, y + 1] == 0) isBorder = true;

            if (isBorder)
            {
                borderPixels.Add(pixel);
            }
        }

        foreach (var pixel in borderPixels)
        {
            if(axis == 0) //x axis
            {
                if(maskSet.GetMaskValue2(sliceIndex,pixel.x,pixel.y) == sourceID) {
                    color = currentSourceColor;
                } 
            }

            if(axis == 1)
            {
                if(maskSet.GetMaskValue2(pixel.x,sliceIndex,pixel.y) == sourceID) {
                    color = currentSourceColor;
                } 
            }

            if(axis == 2)
            {
                if(maskSet.GetMaskValue2(pixel.x,pixel.y,sliceIndex) == sourceID){
                    color = currentSourceColor;
                } 
            }
            texture.SetPixel(pixel.x, pixel.y, color);
        }

        /*/ Draw internal grid lines at regular intervals
        int interval = 5;

        foreach (var pixel in region)
        {
            int x = pixel.x;
            int y = pixel.y;

            if (x % interval == 0 || y % interval == 0)
            {
                texture.SetPixel(x, y, color);
            }
        }
        */
    }

    public void GetPrevMask()
    {
        //have a prev slice variable (set to 0)
        float[,] prevMask = GetFloatSlice(maskCube, axis, prevIndex);
        for(int x = 0; x < currentRegionSlice.width; x++)
        {
            for(int y = 0; y < currentRegionSlice.height; y++)
            {
                if(prevMask[x,y] > 0)
                {
                    if(axis == 0) //x axis
                    {
                        Vector3Int pixel = new Vector3Int(sliceIndex, x, y); //Down the x axis - the actual x = slice, actual y = x, actual z = y 
                        maskSet.PaintMaskVoxel(pixel, maskSet.GetMaskValue2(prevIndex,x,y));  //set to 0 to remove mask
                    }

                    if(axis == 1)
                    {
                        Vector3Int pixel = new Vector3Int(x, sliceIndex, y);
                        maskSet.PaintMaskVoxel(pixel, maskSet.GetMaskValue2(x,prevIndex,y));
                    }

                    if(axis == 2)
                    {
                        Vector3Int pixel = new Vector3Int(x, y, sliceIndex); 
                        maskSet.PaintMaskVoxel(pixel, maskSet.GetMaskValue2(x,y,prevIndex));
                    }
                }
                
            }
        }

        //dataRenderer.FinishBrushStroke();
        maskSet.ConsolidateMaskEntries();

        ResetSlice();
    }

    public void UpdateSourceColours() {
        float[,] slice = GetFloatSlice(maskCube, axis, sliceIndex);
        Color maskColor = new Color(0.8018868f, 0.5030705f, 0.5030705f);
        Color currentSourceColor = Color.yellow;
        for(int x = 0; x < currentRegionSlice.width; x++)
        {
            for(int y = 0; y < currentRegionSlice.height; y++)
            {
                if(slice[x,y] > 0)
                {
                    if(axis == 0) //x axis
                    {
                       if(maskSet.GetMaskValue2(sliceIndex,x,y) == sourceID) {
                            currentRegionSlice.SetPixel(x,y,currentSourceColor);
                       } 
                       else currentRegionSlice.SetPixel(x,y,maskColor);
                    }

                    if(axis == 1)
                    {
                        if(maskSet.GetMaskValue2(x,sliceIndex,y) == sourceID) {
                            currentRegionSlice.SetPixel(x,y,currentSourceColor);
                        } 
                        else currentRegionSlice.SetPixel(x,y,maskColor);
                    }

                    if(axis == 2)
                    {
                        if(maskSet.GetMaskValue2(x,y,sliceIndex) == sourceID){
                            currentRegionSlice.SetPixel(x,y,currentSourceColor);
                        } 
                        else currentRegionSlice.SetPixel(x,y,maskColor);
                    }
                }
                
            }
        }

    }

     public Color GetColorFromColormap(int rowIndex, float value)
    {
        if (colormap == null)
        {
            Debug.LogError("Colormap texture is not assigned.");
            return Color.black;
        }

        if (value < 0f || value > 1f)
        {
            Debug.LogError("Value must be between 0 and 1.");
            return Color.black;
        }

        // Calculate the Y-coordinate of the specific row
        int y = rowIndex * colorMapRowHeight - 1;

        // Calculate the X-coordinate based on the value
        int x = Mathf.FloorToInt(value * (colormapWidth - 1));

        //Debug.Log("The x value is: " + x);

        // Get the color from the colormap at the calculated coordinates
        Color color = colormap.GetPixel(x, y);

        return color;
    }

    public void SpawnCameras()
    {
        if(sliceCamera != null)
        {
            return;
        }
        axis = 2; //remove after testing
        SetCameraTransforms();

        Transform parentTransform = volumeDatasetManager.transform; 
        Transform renderedCube = parentTransform.GetChild(0);  //assigns the parent of the object to the datacube

        sliceCamera = Instantiate(sliceCameraPrefab, renderedCube);
        ResetCamera();
    }

    public void SetCameraTransforms()
    {
        cameraZ.position = new Vector3(1.3f, 0, 0);
        cameraZ.rotation = Quaternion.Euler(0, -90f, 0);

        cameraX.position = new Vector3(0, 1.3f, 0);
        cameraX.rotation = Quaternion.Euler(90, 0, 0);

        cameraY.position = new Vector3(-1.3f, 0, 0);
        cameraY.rotation = Quaternion.Euler(0, 90f, 90f);
    }

    public void SpawnSliceIndicator()
    {
        Transform parentTransform = volumeDatasetManager.transform; 
        Transform renderedCube = parentTransform.GetChild(0);  //assigns the parent of the object to the datacube

        sliceIndicator = Instantiate(sliceIndicatorPrefab, renderedCube);
        ResetSliceIndicator();
    }

//User manipulation and feedback

    public void OnPointerDown(PointerEventData eventData)
    {
        resetButton.interactable = true;
        selectionButton.interactable = true;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (selectionPolyList.Count > 0)
            {
                UndoPoint();
            }
            return;
        }

        isDrawing = true;
        AddPoint(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDrawing = false;
    }

    //Adds points for polygon as user drags
    public void OnDrag(PointerEventData eventData)
    {
        if (isDrawing)
        {
            AddPoint(eventData);
            subtracted = false;
        }
    }

    //Gets the local cursor value and the local pixel value of point
    private void AddPoint(PointerEventData eventData)
    {
        // Transformation from cursor position to the correct pixel in the texture
        Vector2 localCursor;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rawImage.rectTransform, eventData.position, eventData.pressEventCamera, out localCursor))
            return;

        Rect rect = rawImage.rectTransform.rect;

        // Normalize localCursor to (0,1) range within the visible rect
        float normalizedX = (localCursor.x - rect.x) / rect.width;
        float normalizedY = (localCursor.y - rect.y) / rect.height;

        // Get the uvRect to adjust for zoom/panning
        Rect uvRect = rawImage.uvRect;

        // Map normalized coordinates to UV coordinates
        float uvX = uvRect.x + normalizedX * uvRect.width;
        float uvY = uvRect.y + normalizedY * uvRect.height;

        // Ensure we're within valid UV range
        if (uvX >= 0 && uvX <= 1 && uvY >= 0 && uvY <= 1)
        {
            int textureX = Mathf.FloorToInt(uvX * currentRegionSlice.width);
            int textureY = Mathf.FloorToInt(uvY * currentRegionSlice.height);
            
            // Ensure pixel is inside bounds
            textureX = Mathf.Clamp(textureX, 0, currentRegionSlice.width - 1);
            textureY = Mathf.Clamp(textureY, 0, currentRegionSlice.height - 1);

            Vector2 texturePoint = new Vector2(textureX, textureY);
            AddPointToList(localCursor, texturePoint);
        }
    }

    //Adds the marker to the image and the pixel location to the polygon list
    public void AddPointToList(Vector2 localPosition, Vector2 localPixel)
    {
        selectionPolyList.Add(localPixel);
        CheckForPolygonCompletion();
        GameObject circleInstance = Instantiate(markerPrefab, transform);
        circleInstance.transform.localPosition = localPosition;
    }

    //clear the mask at the current layer
    public void ClearMaskButton()
    {
        if(selectionPolyList.Count > 0) {
            ResetSlice();
            return;
        }

        if(subtracted) {
            maskVoxels = lastMaskVoxels;
            ApplyMask(false);
            painted = false;
            subtracted = false;
            return;
        }

        if(maskCount < 1)
        {
            return;
        }

        if(painted)
        {
            UpdateMaskVoxels(true);
            SubtractiveSelection(false);
            maskVoxels = lastMaskVoxels;
            ApplyMask(false);
            painted = false;
        }
        
        maskSet.ConsolidateMaskEntries();

        ResetSlice();
    }

    public void ClearAllButton() {
        if(maskCount < 1)
        {
            return;
        }
            //mask count > 0 then set all pixels to 0 source id and reset
        
        Debug.Log("Removing masks of number: " + maskCount);
        for(int x = 0; x < currentRegionSlice.width; x++)
        {
            for(int y = 0; y < currentRegionSlice.height; y++)
            {
                //can be more efficient by switch statement and having axis checked before loop
                if(axis == 0) //x axis
                {
                    Vector3Int pixel = new Vector3Int(sliceIndex, x, y); //Down the x axis - the actual x = slice, actual y = x, actual z = y 
                    maskSet.PaintMaskVoxel(pixel, 0);  //set to 0 to remove mask
                }

                if(axis == 1)
                {
                    Vector3Int pixel = new Vector3Int(x, sliceIndex, y); 
                    maskSet.PaintMaskVoxel(pixel, 0);
                }

                if(axis == 2)
                {
                    Vector3Int pixel = new Vector3Int(x, y, sliceIndex); 
                    maskSet.PaintMaskVoxel(pixel, 0);
                }

                maskCount--;

                if(maskCount % 20 == 0)
                {
                    //maskSet.FlushBrushStroke();
                }

                
            }
        }
        maskSet.ConsolidateMaskEntries();
        ResetSlice();
        
    }

    private void SubtractiveSelection(bool update) {
        if(update) UpdateLastMaskVoxels();
        Debug.Log("Mask Voxel Count (in clear mask button): " + maskVoxels.Count);
        if(maskVoxels.Count > 0)  //If a selection has been made then you can clear just that area. //Add the name change of the mask
        {
            for(int i = 0; i < maskVoxels.Count; i++)
            {
                maskSet.PaintMaskVoxel(maskVoxels[i], 0);
            }

            //dataRenderer.FinishBrushStroke();
            maskSet.ConsolidateMaskEntries();
            subtracted = true;
            ResetSlice();
            return;
        }
    }

    //clear the modifications made to the texture (not to the slice)
    public void ResetSelectionButton()
    {
        ResetSlice();
    }

    //Clear all markers
    public void RemoveMarkers()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }

    //Apply the selection as a mask
    public void ApplyMask(bool update)
    {
        if(update) UpdateLastMaskVoxels();
        //must make sure polygon is complete (list is populated)
        if(maskVoxels.Count == 0)
        {
            return;
        }

        Debug.Log("Mask voxels: " + maskVoxels.Count);
        for(int i = 0; i < maskVoxels.Count; i++)
        {
            Int16 maskVal = maskSet.GetMaskValue2(maskVoxels[i].x,maskVoxels[i].y,maskVoxels[i].z);
            if(maskVal == 0) maskSet.PaintMaskVoxel(maskVoxels[i], sourceID);
        }
        maskSet.ConsolidateMaskEntries();
        UpdateSourceColours();
        ResetSlice();
        painted = true;
    }

    //Removes the last point that was added (will need modifications if other children are added to the display)
    public void UndoPoint()
    {
        selectionPolyList.RemoveAt(selectionPolyList.Count - 1);
        Destroy(transform.GetChild(transform.childCount - 1).gameObject);
    }

    //Handles the resets when a new slice is selected (markers)
    private void ResetSlice()
    {
        maskVoxels = new List<Vector3Int>();
        UpdateTexture();  //go get the original slice without temp modifications (shading showing where masking would happen)
        RemoveMarkers();  
        ClearSelectionPoly();
        selectionButton.interactable = false;
        selectionButtonText.text = "Fill \n(Space)";
        //Debug.Log("Selection Poly list length: " + selectionPolyList.Count);
        //Debug.Log("Mask voxels list length: " + maskVoxels.Count);
        //Debug.Log("Is drawing: " + isDrawing);
    }

//Polygon creation and masking methods

    //As name suggests and if completed the calls FillPolygon to show where mask will be applied
    public void CheckForPolygonCompletion()
    {
        if (selectionPolyList.Count >= 3 && IsPolygonClosed(selectionPolyList))
        {
            //Debug.Log("Poly closed");
            //add pixels within to temp mask and colour
            RemoveMarkers();
            //Debug.Log("Markers removed");
            FillPolygon();  //change to either be adding removing mask
            //isDrawing = false;  //stop the user drawing
            //Debug.Log("Filling finished");
        }
    }

    //Shows where mask will be applied and stores those position in the mask list (by calling inside out method)
    public void FillPolygon()
    {
        if(selectionPolyList.Count > 10)  //stop the drawing if a polygone has been made (fill polygone is called too early due to first points being so close together)
        {
            isDrawing = false;
        } 

        if (currentRegionSlice == null || selectionPolyList == null || selectionPolyList.Count < 3)
        {
            Debug.LogError("Texture or polygon points not properly assigned.");
            return;
        }

        Color fillColor = new Color(0.6941177f, 0.7113449f, 0.8392157f, 0.75f);  //0.5f alpha for future layer blending
        if(additive) fillColor = Color.green;
        else fillColor = Color.red;

        // Calculate the bounding box of the polygon
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        foreach (var point in selectionPolyList)
        {
            if (point.x < minX) minX = point.x;
            if (point.y < minY) minY = point.y;
            if (point.x > maxX) maxX = point.x;
            if (point.y > maxY) maxY = point.y;
        }

        // Ensure the bounding box is within the texture bounds
        minX = Mathf.Clamp(minX, 0, currentRegionSlice.width - 1);
        minY = Mathf.Clamp(minY, 0, currentRegionSlice.height - 1);
        maxX = Mathf.Clamp(maxX, 0, currentRegionSlice.width - 1);
        maxY = Mathf.Clamp(maxY, 0, currentRegionSlice.height - 1);

        int pixelsChanged = 0;
        bool incorrectSourceCrossed = false;
        Color[] originalPixels = currentRegionSlice.GetPixels();

        for (int y = Mathf.FloorToInt(minY); y <= Mathf.CeilToInt(maxY); y++)
        {
            for (int x = Mathf.FloorToInt(minX); x <= Mathf.CeilToInt(maxX); x++)
            {
                if (IsPointInPolygon(new Vector2(x, y), selectionPolyList)) {
                    if(axis == 0) //x axis
                    {
                        Vector3Int pixel = new Vector3Int(sliceIndex, x, y); //Down the x axis - the actual x = slice, actual y = x, actual z = y 
                        if(maskSet.GetMaskValue2(pixel.x,pixel.y,pixel.z) != sourceID && maskSet.GetMaskValue2(pixel.x,pixel.y,pixel.z) != 0) {
                            incorrectSourceCrossed = true;
                            continue;
                        }
                        maskVoxels.Add(pixel);
                    }

                    if(axis == 1)
                    {
                        Vector3Int pixel = new Vector3Int(x, sliceIndex, y); 
                        if(maskSet.GetMaskValue2(pixel.x,pixel.y,pixel.z) != sourceID && maskSet.GetMaskValue2(pixel.x,pixel.y,pixel.z) != 0) {
                            incorrectSourceCrossed = true;
                            continue;
                        }
                        maskVoxels.Add(pixel);
                    }

                    if(axis == 2)
                    {
                        Vector3Int pixel = new Vector3Int(x, y, sliceIndex); 
                        if(maskSet.GetMaskValue2(pixel.x,pixel.y,pixel.z) != sourceID && maskSet.GetMaskValue2(pixel.x,pixel.y,pixel.z) != 0) {
                            incorrectSourceCrossed = true;
                            continue;
                        }
                        maskVoxels.Add(pixel);
                    }
                }
                if(selectionPolyList.Contains(new Vector2(x, y)))
                {
                    
                    currentRegionSlice.SetPixel(x, y, fillColor); //future improvement - make the colour layer separate and combine it witht his layer (so temp mask can be semi transparent)
                    pixelsChanged++;
                }
            }
        }
        if(incorrectSourceCrossed) {
            selectionPolyList = new List<Vector2>();
            maskVoxels.Clear();
            StartCoroutine(ShowMessage("\tCannot paint over mask of different source. Please change source ID", 4.0f));
            currentRegionSlice.SetPixels(originalPixels);
            currentRegionSlice.Apply();
            return;
        }
        currentRegionSlice.Apply();
        selectionButtonText.text = "Apply Mask \n(Space)";
    }

    //Inside out method to see if point is in the polygon
    public bool IsPointInPolygon(Vector2 point, List<Vector2> polygon)
    {
        int n = polygon.Count;
        bool inside = false;

        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            if (((polygon[i].y > point.y) != (polygon[j].y > point.y)) &&
                (point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x))
            {
                inside = !inside;
            }
        }

        return inside;
    }

    //adds a final point to the polygon equal to the first point and calls the check (so the correct logic can occur)
    public void CompletePolygon()
    {
        if(selectionPolyList.Count >= 3)
        {
            selectionPolyList.Add(selectionPolyList[0]);
        }
        CheckForPolygonCompletion();
    }

    //if the first and last point are within 5 pixels then close the polygon
    public bool IsPolygonClosed(List<Vector2> points)
    {
        Vector2 firstPoint = points[0];
        Vector2 lastPoint = points[points.Count - 1];
        float distance = Vector2.Distance(firstPoint, lastPoint);
        return distance < 1f; // if points are the same
    }

    public void UpdateMaskVoxels(bool matchID) {
        maskVoxels.Clear();
        float[,] slice = GetFloatSlice(maskCube, axis, sliceIndex);
        for(int x = 0; x < currentRegionSlice.width; x++)
        {
            for(int y = 0; y < currentRegionSlice.height; y++)
            {
                if(slice[x,y] > 0)
                {
                    if(axis == 0) //x axis
                    {
                        Vector3Int pixel = new Vector3Int(sliceIndex,x,y);
                        if(matchID) {
                            if(maskSet.GetMaskValue2(pixel.x,pixel.y,pixel.z) == sourceID) maskVoxels.Add(pixel);
                        }
                        else maskVoxels.Add(pixel);
                    }

                    if(axis == 1)
                    {
                        Vector3Int pixel = new Vector3Int(x,sliceIndex,y);
                       if(matchID) {
                            if(maskSet.GetMaskValue2(pixel.x,pixel.y,pixel.z) == sourceID) maskVoxels.Add(pixel);
                        }
                        else maskVoxels.Add(pixel);
                    }

                    if(axis == 2)
                    {
                        Vector3Int pixel = new Vector3Int(x,y,sliceIndex);
                        if(matchID) {
                            if(maskSet.GetMaskValue2(pixel.x,pixel.y,pixel.z) == sourceID) maskVoxels.Add(pixel);
                        }
                        else maskVoxels.Add(pixel);
                    }
                }
                
            }
        }
    }

     public void UpdateLastMaskVoxels() {
        lastMaskVoxels.Clear();
        float[,] slice = GetFloatSlice(maskCube, axis, sliceIndex);
        for(int x = 0; x < currentRegionSlice.width; x++)
        {
            for(int y = 0; y < currentRegionSlice.height; y++)
            {
                if(slice[x,y] > 0)
                {
                    if(axis == 0) //x axis
                    {
                        Vector3Int pixel = new Vector3Int(sliceIndex,x,y);
                        lastMaskVoxels.Add(pixel);
                    }

                    if(axis == 1)
                    {
                        Vector3Int pixel = new Vector3Int(x,sliceIndex,y);
                        lastMaskVoxels.Add(pixel);
                    }

                    if(axis == 2)
                    {
                        Vector3Int pixel = new Vector3Int(x,y,sliceIndex);
                        lastMaskVoxels.Add(pixel);
                    }
                }
                
            }
        }
    }
    
    //clear the polygon selection
    private void ClearSelectionPoly()
    {
        selectionPolyList.Clear();
    }


//User Settings and Navigation

    //select the previous slice or go to end
    public void PreviousSlice()
    {
        prevIndex = sliceIndex;
        if(sliceIndex == 0)
        {
            //Go to the final slice
            if(axis == 0)
            {
                sliceIndex = cubeWidth;  //-1 below
            }

            if(axis == 1)
            {
                sliceIndex = cubeHeight;
            }

            if(axis == 2)
            {
                sliceIndex = cubeDepth;
            }
        }

        sliceIndex -= 1;
        sliceSlider.GetComponent<Slider>().value = sliceIndex;
        //SliderIndicatorChange();
        UpdateSourceColours();
        UpdateMaskVoxels(false);
        UpdateLastMaskVoxels();
        ResetSlice();
        painted = false;
    }

    //select next slice or go to start
    public void NextSlice()
    {
        //if the slice is out of range reset it back to one
        prevIndex = sliceIndex;
        sliceIndex += 1;
        if (sliceIndex < 0 || (axis == 0 && sliceIndex >= cubeWidth) || (axis == 1 && sliceIndex >= cubeHeight) || (axis == 2 && sliceIndex >= cubeDepth))
        {
            sliceIndex = 0;
        }

        sliceSlider.GetComponent<Slider>().value = sliceIndex;
        //SliderIndicatorChange();
        UpdateSourceColours();
        UpdateMaskVoxels(false);
        UpdateLastMaskVoxels();
        ResetSlice();
        painted = false;
    }

    //change the axis (being looked down) and call the new slice
    public void ChangeAxis(int axisIndex)
    {
        axis = axisIndex;
        //Debug.Log("Axis changed to: " + axis);
        sliceIndex = 0;
        prevIndex = 0;
        SetSliceSlider();
        sliceSlider.GetComponent<Slider>().value = 0;
        ResetSlice();
        ResetCamera();
        ResetSliceIndicator();
    }
    
    //Change the color map
    public void ChangeColorMap()
    {
        colorMapEnum = ColorMapUtils.FromHashCode(colorMapDropdown.GetComponent<TMP_Dropdown>().value);
        ResetSlice();
    }

    private void SetColorMap()
    {
        colorMapDropdown.GetComponent<TMP_Dropdown>().options.Clear();

        foreach (var colorMap in Enum.GetValues(typeof(ColorMapEnum)))
        {

            colorMapDropdown.GetComponent<TMP_Dropdown>().options.Add(new TMP_Dropdown.OptionData() { text = colorMap.ToString() });
        }

        colorMapDropdown.GetComponent<TMP_Dropdown>().value = Config.Instance.defaultColorMap.GetHashCode();
    }

    private void SetSliceSlider()
    {
        if(axis == 0)
            {
                sliceSlider.GetComponent<Slider>().maxValue = cubeWidth - 1;
            }

            if(axis == 1)
            {
                sliceSlider.GetComponent<Slider>().maxValue = cubeHeight - 1;
            }

            if(axis == 2)
            {
                sliceSlider.GetComponent<Slider>().maxValue = cubeDepth - 1;
            }
    }

    public void SliceSliderChanged()
    {
        sliceIndex = (int) sliceSlider.GetComponent<Slider>().value;
        ResetSlice();
        SliderIndicatorChange();
    }

    public void ResetCamera()
    {
        if(axis == 0)
        {
            sliceCamera.transform.localPosition = cameraX.position;
            sliceCamera.transform.localRotation = cameraX.rotation;
        }

        if(axis == 1)
        {
            sliceCamera.transform.localPosition = cameraY.position;
            sliceCamera.transform.localRotation = cameraY.rotation;
        }

        if(axis == 2)
        {
            sliceCamera.transform.localPosition = cameraZ.position;
            sliceCamera.transform.localRotation = cameraZ.rotation;
        }
    }

    public void ResetSliceIndicator()
    {
        if(axis == 0)
        {
            sliceIndicator.transform.localPosition = new Vector3(-0.5f, 0, 0);
            sliceIndicator.transform.localRotation = Quaternion.Euler(0, -90f, 0f);
        }

        if(axis == 1)
        {
            sliceIndicator.transform.localPosition = new Vector3(0, -0.5f, 0);
            sliceIndicator.transform.localRotation = Quaternion.Euler(90f, 0, 0);
        }

        if(axis == 2)
        {
            sliceIndicator.transform.localPosition = new Vector3(0, 0, -0.5f);
            sliceIndicator.transform.localRotation = Quaternion.Euler(0, 0, 0);
        }
    }

    public void SliderIndicatorChange()
    {
        if(axis == 0)
        {;
            //float value = (float) sliceIndex / (cubeWidth - 1);
            float localizedValue = Mathf.Lerp(-0.5f, 0.5f, (float) sliceIndex / (cubeWidth - 1));
            sliceIndicator.transform.localPosition = new Vector3(localizedValue, 0, 0);
        }

        if(axis == 1)
        {
            float localizedValue = Mathf.Lerp(-0.5f, 0.5f, (float) sliceIndex / (cubeHeight - 1));
            sliceIndicator.transform.localPosition = new Vector3(0, localizedValue, 0);
        }

        if(axis == 2)
        {
            float localizedValue = Mathf.Lerp(-0.5f, 0.5f, (float) sliceIndex / (cubeDepth - 1));
            sliceIndicator.transform.localPosition = new Vector3(0, 0, localizedValue);
        }
    }
//Saving

    public void SaveMask(bool overwrite)
    {
        PaintMenuController _paintMenuController = FindObjectOfType<PaintMenuController>();
        //maskSet.FileName = "VRAstro Project";

        if(overwrite)
        {
            _paintMenuController.SaveOverwriteMask();
            StartCoroutine(ShowMessage("\tMask written to disk", 2.0f));
        }
        else
        {
            _paintMenuController.SaveNewMask();
            StartCoroutine(ShowMessage("\tNew Mask saved",2.0f));
        }
    }

    public IEnumerator ShowMessage(string message, float time) {
        saveMessage.GetComponent<TextMeshProUGUI>().text = message;
        saveMessage.SetActive(true);
        yield return new WaitForSeconds(time);
        saveMessage.SetActive(false);
    }

    public void OnToggleChanged(bool val) {
        additive = val;
        if(additive) {
            selectionModeImage.color = Color.green;
            selectionModeText.text = "Additive";
        }
        else {
            selectionModeImage.color = Color.red;
            selectionModeText.text = "Subtractive";
        }
    }

    public void SelectionButton() {
        if(IsPolygonClosed(selectionPolyList))
            {
                if(additive) ApplyMask(true);
                else SubtractiveSelection(true);
            }
        CompletePolygon();
    }
}
