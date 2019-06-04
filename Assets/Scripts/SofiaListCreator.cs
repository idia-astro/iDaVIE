using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SofiaListCreator : MonoBehaviour
{

    [SerializeField]
    private Transform SpawnPoint = null;

    [SerializeField]
    private GameObject item = null;

    [SerializeField]
    private RectTransform content = null;

    [SerializeField]
    private int numberOfItems = 3;

    public GameObject SofiaNewListController = null;



    // Start is called before the first frame update
    void Start()
    {

        //setContent Holder Height;
        content.sizeDelta = new Vector2(0, numberOfItems * 100);
     

        for (int i = 0; i < numberOfItems; i++)
        {
            // 100 Height of item
            float spawnY = i * 100;
            //newSpawn Position
            Vector3 pos = new Vector3(SpawnPoint.position.x+300, -spawnY, SpawnPoint.position.z);
            //instantiate item
            GameObject SpawnedItem = Instantiate(item, pos, SpawnPoint.rotation);
            //setParent
            SpawnedItem.transform.SetParent(SpawnPoint, false);
            //get ItemDetails Component
            SofiaListItem itemDetails = SpawnedItem.GetComponent<SofiaListItem>();

            itemDetails.SofiaNewListController = SofiaNewListController;
            //set name
            itemDetails.idTextField.text = i.ToString();
            if(i%2!=0)
             itemDetails.GetComponent<Image>().color = new Color(0.4039216f, 0.5333334f, 0.5882353f, 1f);


           
        }
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
