using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class VideoCameraPath
{
    public abstract Vector3 GetPosition(float pathParam);
    public abstract Vector3 GetTangent(float pathParam);
}

public class LinePath : VideoCameraPath
{
    public Vector3 startPosition;

    public Vector3 endPosition;

    public LinePath(Vector3 startPosition, Vector3 endPosition) {
        this.startPosition = startPosition;
        this.endPosition = endPosition;
    }

    public override Vector3 GetPosition(float pathParam)
    {
        return startPosition * (1 - pathParam) + endPosition * pathParam;
    }

    public override Vector3 GetTangent(float pathParam)
    {
        return (endPosition - startPosition).normalized;
    }
}

public class VideoAction
{
    public float startTime;
    public float duration;

    public VideoCameraPath path;

    public VideoAction(float startTime, float duration, VideoCameraPath path)
    {
        this.startTime = startTime;
        this.duration = duration;
        this.path = path;
    }

    public Vector3 GetPosition(float time)
    {
        float pathParam = Math.Clamp((time - startTime) / duration, 0f, 1f);

        return path.GetPosition(pathParam);
    }

    public Vector3 GetLookDirection(float time)
    { 
        float pathParam = Math.Clamp((time - startTime) / duration, 0f, 0f);
        return path.GetTangent(pathParam);
    }
}


public class VideoCameraController : MonoBehaviour
{
    
    // private Array<VideoAction> _actionQueue = new Array<VideoAction>();
    private VideoAction _action = new VideoAction(
        0.0f,
        3.0f,
        new LinePath(new Vector3(0f, 0f, -1f), new Vector3(0f, 0f, -0.5f))
    );

    private float _actionTime = 0f;

    void Awake()
    {
        enabled = false;
    }

    void Update()
    {
        //TODO: For preview mode use Time.deltaTime, for recordings use the defined frame time of the VideoScript
        _actionTime += Time.deltaTime;

        UpdateTransform(_action.GetPosition(_actionTime), _action.GetLookDirection(_actionTime));

        if (_actionTime > _action.duration)
        {
            enabled = false;
        }
    }

    private void UpdateTransform(Vector3 position, Vector3 lookDirection)
    {
        gameObject.transform.position = position;
        gameObject.transform.LookAt(position + lookDirection);
    }

    public void OnPreviewClick()
    {
        UpdateTransform(_action.GetPosition(0f), _action.GetLookDirection(0f));

        enabled = true;
        _actionTime = 0f;
    }
}
