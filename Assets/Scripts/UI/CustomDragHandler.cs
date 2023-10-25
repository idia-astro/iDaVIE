using System;
using System.Collections;
using System.Collections.Generic;
using DataFeatures;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class CustomDragHandler : MonoBehaviour
{
    [FormerlySerializedAs("spawnPoint")] public GameObject anchorPoint;       // The parent object of the objects to be dragged.
                                        // Set this to Content object when using ScrollRect to allow clamps to work properly.
                                        // This requires adjusting the Content size if spawning objects at runtime.
    public int scrollSpeed;
    private RectTransform anchorPointPosition;
    public float Spawn_initial_y {get; private set;}


    // Use this for initialization
    void Start()
    {
        anchorPointPosition = anchorPoint.gameObject.GetComponent<RectTransform>();
    }


    public void MoveUp()
    {
        anchorPointPosition.localPosition += Vector3.down * scrollSpeed;
    }

    public void MoveDown()
    {
        anchorPointPosition.localPosition += Vector3.up * scrollSpeed;
    }
}
