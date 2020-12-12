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
    public float Spawn_initial_y {get; private set;}
    private GameObject _previousSelectedListItem = null;
    private Color _previousListItemColor;

    // Use this for initialization
    void Start()
    {
        spawnPointPosition = spawnPoint.gameObject.GetComponent<RectTransform>();
        Spawn_initial_y = spawnPointPosition.localPosition.y;
        Debug.Log("init localPosition: " + spawnPointPosition.localPosition);

    }

    public void FocusOnFeature(int featureIndex)
    {
        if (featureIndex < 0)
            return;
        if (_previousSelectedListItem != null)
        {
            _previousSelectedListItem.GetComponent<Image>().color = _previousListItemColor;
        }
        var _spawnPoint = transform.Find("Viewport").gameObject.transform.Find("Content").gameObject.transform.Find("SpawnPoint").gameObject;
        var featureListItem = GetComponent<SofiaListCreator>().SofiaObjectsList[featureIndex];
        float verticalPosition = Spawn_initial_y - featureListItem.transform.localPosition.y;
        _spawnPoint.GetComponent<RectTransform>().localPosition = new Vector3 (1, verticalPosition);
        _previousSelectedListItem = featureListItem;
        _previousListItemColor = featureListItem.GetComponent<Image>().color;
        featureListItem.GetComponent<Image>().color = Color.red;
    }

    public void MoveUp()
    {
        Debug.Log("Go up! "+ spawnPointPosition.localPosition.y+" was: "+ Spawn_initial_y);

        spawnPointPosition.localPosition += Vector3.down * scrollSpeed;
        if (spawnPointPosition.localPosition.y < Spawn_initial_y)
        {
            spawnPointPosition.localPosition = new Vector3 (1,Spawn_initial_y);
        }
    }

    public void MoveDown()
    {


        Debug.Log("Go down! " + spawnPointPosition.localPosition.y + " target: " + (Spawn_initial_y * 1.5f * -1 + Spawn_initial_y));


        spawnPointPosition.localPosition += Vector3.up * scrollSpeed;
        if (spawnPointPosition.localPosition.y > 2000 )
        {
            spawnPointPosition.localPosition = new Vector3(1, 2000);
        }


    }
}
