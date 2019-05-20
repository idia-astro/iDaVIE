using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SofiaNewListController : MonoBehaviour
{

    public List<GameObject> sofiaNewElement;

    [SerializeField]
    private Transform SpawnPoint = null;

    [SerializeField]
    private GameObject item = null;

    [SerializeField]
    private GameObject menu = null;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CreateAndAddNewElement()
    {


        Debug.Log(menu.transform.rotation);
            // 100 Height of item
            float spawnY = sofiaNewElement.Count * 100;
            //newSpawn Position
            Vector3 pos = new Vector3(SpawnPoint.position.x + 300, -spawnY, SpawnPoint.position.z);
        //instantiate item
        // GameObject SpawnedItem = Instantiate(item, pos, SpawnPoint.rotation);
        GameObject SpawnedItem = Instantiate(item, pos, new Quaternion(0, 0, 0, 0));
            //setParent
            SpawnedItem.transform.SetParent(SpawnPoint, false);

            sofiaNewElement.Add(SpawnedItem);

          
            //get ItemDetails Component
            SofiaNewListItem itemDetails = SpawnedItem.GetComponent<SofiaNewListItem>();
            //set name
            itemDetails.idTextField.text = sofiaNewElement.Count.ToString();
            if (sofiaNewElement.Count % 2 != 0)
                itemDetails.GetComponent<Image>().color = new Color(0.4039216f, 0.5333334f, 0.5882353f, 1f);
      

            Debug.Log("Add Element to new list");
    }

}
