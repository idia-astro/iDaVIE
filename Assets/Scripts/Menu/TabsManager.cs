using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TabsManager : MonoBehaviour
{
    public GameObject[] tabs;
    public GameObject[] panels;

    public Color defaultColor;
    public Color selectedColor;

    private int activeTabIndex=0;
    private int old_activeTabIndex = -1;

    public int defaultTabIndex = 0;

    // Start is called before the first frame update
    void Start()
    {
        foreach (GameObject tab in tabs)
        {
            tab.GetComponent<Image>().color = defaultColor;
        }

        tabs[defaultTabIndex].GetComponent<Image>().color = selectedColor;
        panels[defaultTabIndex].SetActive(true);

    }

    // Update is called once per frame
    void Update()
    {

    } 

    public void UpdateActiveTab(int newActiveTab)
    {

       
        old_activeTabIndex = activeTabIndex;
        activeTabIndex = newActiveTab;

        if (old_activeTabIndex != activeTabIndex)
        {
            tabs[old_activeTabIndex].GetComponent<Image>().color = defaultColor;
            tabs[activeTabIndex].GetComponent<Image>().color = selectedColor;

            panels[old_activeTabIndex].SetActive(false);
            panels[activeTabIndex].SetActive(true);
        }

    }
}


