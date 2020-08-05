using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CustomDragHandler : MonoBehaviour
{
    public GameObject spawnPoint;
    public int scrollSpeed;
    private RectTransform spawnPointPosition;
    private float spawn_initial_y;

    // Use this for initialization
    void Start()
    {
        spawnPointPosition = spawnPoint.gameObject.GetComponent<RectTransform>();
        spawn_initial_y = spawnPointPosition.localPosition.y;
        Debug.Log("init localPosition: " + spawnPointPosition.localPosition);

    }

    // Update is called once per frame
    void Update()
    {
    }

    public void MoveUp()
    {
        Debug.Log("Go up! "+ spawnPointPosition.localPosition.y+" was: "+ spawn_initial_y);

        spawnPointPosition.localPosition += Vector3.down * scrollSpeed;
        if (spawnPointPosition.localPosition.y < spawn_initial_y)
        {
            spawnPointPosition.localPosition = new Vector3 (1,spawn_initial_y);
        }
    }

    public void MoveDown()
    {


        Debug.Log("Go down! " + spawnPointPosition.localPosition.y + " target: " + (spawn_initial_y * 1.5f * -1 + spawn_initial_y));


        spawnPointPosition.localPosition += Vector3.up * scrollSpeed;
        if (spawnPointPosition.localPosition.y > 2000 )
        {
            spawnPointPosition.localPosition = new Vector3(1, 2000);
        }


    }
}
