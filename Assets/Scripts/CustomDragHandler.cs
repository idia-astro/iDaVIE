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
    private GameObject _previousSelectedListItem = null;
    private Color _previousListItemColor;

    // Use this for initialization
    void Start()
    {
        spawnPointPosition = spawnPoint.gameObject.GetComponent<RectTransform>();
        Spawn_initial_y = spawnPointPosition.localPosition.y;
    }

    public void FocusOnFeature(Feature feature, bool scrollTo)
    {
        if (_previousSelectedListItem != null)
        {
            _previousSelectedListItem.GetComponent<Image>().color = _previousListItemColor;
        }
        var _spawnPoint = transform.Find("Viewport").gameObject.transform.Find("Content").gameObject.transform.Find("SpawnPoint").gameObject;
        var featureListItem = feature.LinkedListItem;
        if (scrollTo)
        {
            float verticalPosition = Spawn_initial_y - featureListItem.transform.localPosition.y;
            _spawnPoint.GetComponent<RectTransform>().localPosition = new Vector3 (1, verticalPosition);
        }
        _previousSelectedListItem = featureListItem;
        _previousListItemColor = featureListItem.GetComponent<Image>().color;
        featureListItem.GetComponent<Image>().color = feature.FeatureSetParent.FeatureColor;
    }

    public void MoveUp()
    {
        spawnPointPosition.localPosition += Vector3.down * scrollSpeed;
        if (spawnPointPosition.localPosition.y < Spawn_initial_y)
        {
            spawnPointPosition.localPosition = new Vector3 (1,Spawn_initial_y);
        }
    }

    public void MoveDown()
    {
        spawnPointPosition.localPosition += Vector3.up * scrollSpeed;
        float yLimit = GetComponent<SofiaListCreator>().NumberOfFeatures * 100f;
        if (spawnPointPosition.localPosition.y > yLimit )
        {
            spawnPointPosition.localPosition = new Vector3(1, yLimit);
        }
    }
}
