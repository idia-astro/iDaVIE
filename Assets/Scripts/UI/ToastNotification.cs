using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ToastNotification 
{
    public class StaticToastNotification : MonoBehaviour { }


    //Variable reference for the class
    private static StaticToastNotification staticToastNotification;

    private static VolumeInputController _volumeInputController = null;
    private static GameObject spawnedItem =null;
    private static bool fadeIn=false;
    private static bool fadeOut =false;
    private static float fadeSpeed = 0.05f;
    private static float stayAlive = 3;

    public static void Update()
    {
        if (fadeIn)
        {
            staticToastNotification.StartCoroutine(FadeInToast());
        }
        else if (fadeOut)
        {
            staticToastNotification.StartCoroutine(FadeOutToast());
        }
    }

    public static IEnumerator FadeInToast()
    {
        while (spawnedItem.transform.localPosition.y<0f)
        {
            float fadeAmount = spawnedItem.transform.localPosition.y + (fadeSpeed * Time.deltaTime);

            spawnedItem.transform.localPosition = new Vector3(spawnedItem.transform.localPosition.x, fadeAmount, spawnedItem.transform.localPosition.z);
            yield return null;
        }
        
        fadeIn = false;
        yield return new WaitForSeconds(stayAlive);
        fadeOut = true;
        staticToastNotification.StopCoroutine(FadeInToast());
    }

    public static IEnumerator FadeOutToast()
    {
        while (spawnedItem.transform.localPosition.y > -1f)
        {
            float fadeAmount = spawnedItem.transform.position.y - (fadeSpeed * Time.deltaTime);

            spawnedItem.transform.position = new Vector3(spawnedItem.transform.position.x, fadeAmount, spawnedItem.transform.position.z);
            yield return null;
        }
        fadeOut = false;
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

        spawnedItem = GameObject.Instantiate(_volumeInputController.toastNotificationPrefab, spawnPos, Quaternion.LookRotation(new Vector3(spawnPos.x - playerPos.x, 0, spawnPos.z - playerPos.z)), _volumeInputController.followHead.transform);
        spawnedItem.transform.localPosition = new Vector3(spawnedItem.transform.localPosition.x, -1f, spawnedItem.transform.localPosition.z);
        spawnedItem.transform.localScale = new Vector3(0.0005f, 0.0005f, 0.0005f);

        fadeIn = true;
    }

    public static void ShowToast(string message, Color bgColor, Color textColor)
    {
        if (GameObject.FindGameObjectWithTag("ToastNotification") == null)
        {
            Initialize();
            spawnedItem.transform.Find("TopPanel").gameObject.GetComponent<Image>().color = bgColor;
            spawnedItem.transform.Find("TopPanel").Find("Text").gameObject.GetComponent<TextMeshProUGUI>().text=message;
            spawnedItem.transform.Find("TopPanel").Find("Text").gameObject.GetComponent<TextMeshProUGUI>().color= textColor;
        }
    }

    public static void ShowError(string message)
    {
        ShowToast(message, Color.red, Color.white);
    }

    public static void ShowSuccess(string message)
    {
        ShowToast(message, Color.green, Color.white);
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
