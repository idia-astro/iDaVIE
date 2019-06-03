﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TabsManager : MonoBehaviour
{
    public GameObject[] tabs;
    public GameObject[] panels;

    private int activeTabIndex=0;
    private int old_activeTabIndex = 0;

    Color defaultColor= new Color(0.2901961f, 0.3960785f, 0.4470589f, 0.2f);
    Color selectedColor = new Color(0.2901961f, 0.3960785f, 0.4470589f, 1f);

    // Start is called before the first frame update
    void Start()
    {
        foreach (GameObject tab in tabs)
        {
            tab.GetComponent<Image>().color = defaultColor;
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        tabs[old_activeTabIndex].GetComponent<Image>().color = defaultColor;
        tabs[activeTabIndex].GetComponent<Image>().color = selectedColor;

        panels[old_activeTabIndex].SetActive(false);
        panels[activeTabIndex].SetActive(true);


    } 

    public void UpdateActiveTab(int newActiveTab)
    {
        old_activeTabIndex = activeTabIndex;
        activeTabIndex = newActiveTab;
    }
}

