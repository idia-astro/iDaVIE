using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SofiaListItem : MonoBehaviour
{

    public Text idTextField = null;
    public GameObject checkboxImg = null;
    public GameObject SofiaNewListPrefab = null;
    public GameObject SofiaNewListController = null;
    private bool isVisible = true;

    public void SetVisible()
    {
        
        if (!isVisible)
        {
            checkboxImg.SetActive(true);
        }
        else
        {
            checkboxImg.SetActive(false);
        }
        isVisible = !isVisible;
    }

    public void AddToNewList()
    {
        SofiaNewListController.GetComponent<SofiaNewListController>().CreateAndAddNewElement();
    }


  /*
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    */
}
