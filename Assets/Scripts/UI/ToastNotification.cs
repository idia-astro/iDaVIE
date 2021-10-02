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
        if (fadeOut)
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
        
        float targetDistance = 0.5f;
        var cameraTransform = Camera.main.transform;
        Vector3 cameraPosWorldSpace = cameraTransform.position;
        Vector3 cameraDirWorldSpace = cameraTransform.forward.normalized;
        Vector3 targetPosition = cameraPosWorldSpace + cameraDirWorldSpace * targetDistance;
        

        spawnedItem = GameObject.Instantiate(_volumeInputController.toastNotificationPrefab, targetPosition, Quaternion.identity, _volumeInputController.followHead.transform);
        spawnedItem.transform.localPosition = new Vector3(spawnedItem.transform.localPosition.x, -1f, spawnedItem.transform.localPosition.z);
        spawnedItem.transform.localScale = new Vector3(0.0005f, 0.0005f, 0.0005f);
        fadeIn = true;
    }

    public static void ShowToast(string message, Color32 color)
    {
        if (GameObject.FindGameObjectWithTag("ToastNotification") == null)
        {
            Initialize();
            spawnedItem.transform.Find("TopPanel").gameObject.GetComponent<Image>().color = color;
            spawnedItem.transform.Find("TopPanel").Find("Text").gameObject.GetComponent<TextMeshProUGUI>().text=message;
        }
    }

    public static void ShowError(string message)
    {
        ShowToast(message, new Color32(255, 0, 0, 255));
    }

    public static void ShowSuccess(string message)
    {
        ShowToast(message, new Color32(0, 255, 0, 255));
    }

    public static void ShowInfo(string message)
    {
            ShowToast(message, new Color32(155, 155, 155, 255));
    }

    public static void ShowWarning(string message)
    {
            ShowToast(message, new Color32(255, 155, 0, 255));
    }
}
