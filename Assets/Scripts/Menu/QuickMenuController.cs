using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuickMenuController : MonoBehaviour
{

    public GameObject mainMenuCanvas;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }



    public void Exit()
    {
        Application.Quit();
    }

    public void OpenMainMenu()
    {
        mainMenuCanvas.SetActive(!mainMenuCanvas.activeSelf);
    }
}
