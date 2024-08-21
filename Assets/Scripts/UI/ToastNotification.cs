/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 Inter-University Institute for Data Intensive Astronomy
 *
 * This file is part of the iDaVIE project.
 *
 * iDaVIE is free software: you can redistribute it and/or modify it under the terms 
 * of the GNU Lesser General Public License (LGPL) as published by the Free Software 
 * Foundation, either version 3 of the License, or (at your option) any later version.
 *
 * iDaVIE is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR 
 * PURPOSE. See the GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License along with 
 * iDaVIE in the LICENSE file. If not, see <https://www.gnu.org/licenses/>.
 *
 * Additional information and disclaimers regarding liability and third-party 
 * components can be found in the DISCLAIMER and NOTICE files included with this project.
 *
 */
using System.Collections;
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
    private static GameObject spawnedItem = null;
    private static float fadeSpeed = 1.0f;
    private static float stayAlive = 3;

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
        var canvasGroup = spawnedItem.GetComponent<CanvasGroup>();
        while (canvasGroup.alpha < 1.0f)
        {
            canvasGroup.alpha += fadeSpeed * Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = 1.0f;
        
        yield return new WaitForSeconds(stayAlive);
        staticToastNotification.StopCoroutine(FadeInToast());
        staticToastNotification.StartCoroutine(FadeOutToast());

    }

    public static IEnumerator FadeOutToast()
    {
        var canvasGroup = spawnedItem.GetComponent<CanvasGroup>();
        while (canvasGroup.alpha > 0.0f)
        {
            canvasGroup.alpha -= fadeSpeed * Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 0.0f;
        staticToastNotification.StopCoroutine(FadeOutToast());
        yield return new WaitForSeconds(0.01f);
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
        float spawnDistance = 0.5f;
        Vector3 spawnPos = playerPos + playerDirection * spawnDistance;

        spawnedItem = GameObject.Instantiate(_volumeInputController.toastNotificationPrefab, spawnPos, Quaternion.identity, _volumeInputController.followHead.transform);
        spawnedItem.transform.localRotation = new Quaternion(0, 0, 0,1);
        spawnedItem.transform.localPosition = new Vector3(-0.10f, 0.08f, spawnedItem.transform.localPosition.z);
        spawnedItem.transform.localScale = new Vector3(0.0005f, 0.0005f, 0.0005f);

        spawnedItem.transform.Find("CounterContainer").Find("Counter").Find("Text").gameObject.GetComponent<TextMeshProUGUI>().text = notifications.Count.ToString();
        var canvasGroup = spawnedItem.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0;
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
        Debug.LogError(message);
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
        Debug.LogWarning(message);
    }
}
