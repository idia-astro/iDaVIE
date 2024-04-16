using UnityEngine;

public class SpectatorMovement : MonoBehaviour
{
    public Transform targetCube;
    public float distanceFromCubeStart = 10f;
    public float distanceFromCubeEnd = 3f;

    public float movementSpeed1 = 2f;
    public float movementSpeed2 = 2f;

    public float rotationSpeed1 = 60f;
    public float rotationSpeed2 = 50f;

    public float centerPosition = 0.2f;

    
    private float totalRotation = 0f;
    private Vector3 cameraOffset;
    private bool movingTowardsCube = true;
    private bool rotatingCube = false;
    private bool movingIntoCube = false;
    private bool rotatingInCube = false;
    private bool movingOutOfCube = false;

    void Start()
    {
        // Get parent gameobject
        var parent = transform.parent.gameObject;

        // Find the CubePrefab in the parent's children
        foreach (Transform child in parent.transform)
        {
            if (child.name == "CubePrefab(Clone)")
            {
                targetCube = child;
                break;
            }
        }

        // Set camera's initial position
        transform.position = new Vector3(0f, 0f, -distanceFromCubeStart);
        transform.LookAt(targetCube);

        // Calculate camera's offset from the cube's center
        cameraOffset = transform.position - targetCube.position;
    }


    void Update()
    {
        if (movingTowardsCube)
        {
            // Move camera towards the cube
            float step = movementSpeed1 * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, targetCube.position, step);

            if (Vector3.Distance(transform.position, targetCube.position) <= distanceFromCubeEnd)
            {
                movingTowardsCube = false;
                rotatingCube = true;
            }
        }
        else if (rotatingCube)
        {
            // Rotate the cube around its z-axis
            float angle = rotationSpeed1 * Time.deltaTime;
            targetCube.Rotate(Vector3.up, angle);
            totalRotation += angle;
            //rotationDuration -= Time.deltaTime;
            if (totalRotation > 360f)
            {
                totalRotation = 0f;
                rotatingCube = false;
                movingIntoCube = true;
            }
        }
        else if (movingIntoCube)
        {
            // Move camera into the cube
            float step = movementSpeed2 * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, targetCube.position, step);

            float magnitudeDiff = (transform.position - targetCube.position).magnitude;
            
            if (magnitudeDiff < centerPosition)
            {
                movingIntoCube = false;
                rotatingInCube = true;
            }
        }
        else if (rotatingInCube)
        {
            // Rotate the cube around its z-axis
            float angle = rotationSpeed2 * Time.deltaTime;
            targetCube.Rotate(Vector3.up, angle);
            totalRotation += angle;
            //rotationDuration -= Time.deltaTime;
            if (totalRotation > 360f)
            {
                totalRotation = 0f;
                rotatingInCube = false;
                movingOutOfCube = true;
            }
        }
        else if (movingOutOfCube)
        {
            // Move camera out of the cube
            float step = movementSpeed2 * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, targetCube.position + cameraOffset, step);

            if (transform.position == targetCube.position + cameraOffset)
            {
                movingOutOfCube = false;
            }
        }
    }
}
