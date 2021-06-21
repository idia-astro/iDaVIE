using System;
using System.Collections;
using System.Collections.Generic;
using DataFeatures;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CustomDragHandler : MonoBehaviour
{
    public GameObject spawnPoint;
    public int scrollSpeed;
    private RectTransform spawnPointPosition;
    public float Spawn_initial_y {get; private set;}


    // Use this for initialization
    void Start()
    {
        spawnPointPosition = spawnPoint.gameObject.GetComponent<RectTransform>();
    }


    public void MoveUp()
    {
        spawnPointPosition.localPosition += Vector3.down * scrollSpeed;
    }

    public void MoveDown()
    {
        spawnPointPosition.localPosition += Vector3.up * scrollSpeed;
    }
}
