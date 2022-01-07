﻿using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

struct Notification
{
    public string text;
    public Color textColor;
    public Color bgColor;

    public Notification( string t, Color bc, Color tc)
    {
        this.text = t;
        this.textColor = tc;
        this.bgColor = bc;
    }
}

public class ToastNotification 
{
    public class StaticToastNotification : MonoBehaviour { }


    //Variable reference for the class
    private static StaticToastNotification staticToastNotification;

    private static VolumeInputController _volumeInputController = null;
    private static GameObject spawnedItem =null;
    private static float fadeSpeed = 0.7f;
    private static float stayAlive = 4;

    private static List<Notification> notifications = new List<Notification>();

    public static void Update()
    {
        for (int i=0; i<notifications.Count;i++)
        {
            if (GameObject.FindGameObjectWithTag("ToastNotification") == null)
            {
                Initialize();

                spawnedItem.transform.Find("TopPanel").gameObject.GetComponent<Image>().color = notifications[i].bgColor;
                spawnedItem.transform.Find("TopPanel").Find("Text").gameObject.GetComponent<TextMeshProUGUI>().text = notifications[i].text;
                spawnedItem.transform.Find("TopPanel").Find("Text").gameObject.GetComponent<TextMeshProUGUI>().color = notifications[i].textColor;
               
                notifications.RemoveAt(0);
            }
        }
    }

    public static IEnumerator FadeInToast()
    {
        while (spawnedItem.transform.localPosition.y< -0.22f)
        {
            float fadeAmount = spawnedItem.transform.localPosition.y + (fadeSpeed * Time.deltaTime);

            spawnedItem.transform.localPosition = new Vector3(spawnedItem.transform.localPosition.x, fadeAmount, spawnedItem.transform.localPosition.z);
            yield return null;
        }
        
        yield return new WaitForSeconds(stayAlive);
        staticToastNotification.StopCoroutine(FadeInToast());
        staticToastNotification.StartCoroutine(FadeOutToast());

    }

    public static IEnumerator FadeOutToast()
    {
        while (spawnedItem.transform.localPosition.y > -1f)
        {
            float fadeAmount = spawnedItem.transform.position.y - (fadeSpeed * Time.deltaTime);

            spawnedItem.transform.position = new Vector3(spawnedItem.transform.position.x, fadeAmount, spawnedItem.transform.position.z);
            yield return null;
        }
        staticToastNotification.StopCoroutine(FadeOutToast());
        yield return new WaitForSeconds(0.1f);
        Object.Destroy(spawnedItem);
    }

    public static void Initialize()
    {
        //Hack to use Coroutine
        if (staticToastNotification == null)
        {
            //Create an empty object called StaticToastNotification
            GameObject gameObject = new GameObject("StaticToastNotification");
            //Add this script to the object
            staticToastNotification = gameObject.AddComponent<StaticToastNotification>();
        }

        if (_volumeInputController == null)
            _volumeInputController = GameObject.FindObjectOfType<VolumeInputController>();
       
        Vector3 playerPos = Camera.main.transform.position;
        Vector3 playerDirection = Camera.main.transform.forward;
        Quaternion playerRotation = Camera.main.transform.rotation;
        float spawnDistance = 0.5f;
        Vector3 spawnPos = playerPos + playerDirection * spawnDistance;

        spawnedItem = GameObject.Instantiate(_volumeInputController.toastNotificationPrefab, spawnPos, Quaternion.identity, _volumeInputController.followHead.transform);
        spawnedItem.transform.localRotation = new Quaternion(0, 0, 0,1);
        spawnedItem.transform.localPosition = new Vector3(spawnedItem.transform.localPosition.x, -1f, spawnedItem.transform.localPosition.z);
        spawnedItem.transform.localScale = new Vector3(0.0005f, 0.0005f, 0.0005f);

        spawnedItem.transform.Find("CounterContainer").Find("Counter").Find("Text").gameObject.GetComponent<TextMeshProUGUI>().text = notifications.Count.ToString();

        staticToastNotification.StartCoroutine(FadeInToast());
    }

    public static void ShowToast(string message, Color bgColor, Color textColor)
    {
        notifications.Add(new Notification(message, bgColor, textColor));
        if(spawnedItem)
            spawnedItem.transform.Find("CounterContainer").Find("Counter").Find("Text").gameObject.GetComponent<TextMeshProUGUI>().text = notifications.Count.ToString();
    }

    public static void ShowError(string message)
    {
        ShowToast(message, new Color(0.77f, 0, 0.14f, 1), Color.white);
    }

    public static void ShowSuccess(string message)
    {
        ShowToast(message, new Color(0.23f, 0.41f, 0.005f, 1), Color.white);
    }

    public static void ShowInfo(string message)
    {
        ShowToast(message, Color.grey, Color.white);
    }

    public static void ShowWarning(string message)
    {
        ShowToast(message, Color.yellow, Color.black );
    }
}
