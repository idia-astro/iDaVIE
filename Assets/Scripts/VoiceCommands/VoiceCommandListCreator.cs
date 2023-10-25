using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VolumeData;
 

public class VoiceCommandListCreator : MonoBehaviour
{
  
    private VolumeDataSetRenderer _activeDataSet;
    private VolumeDataSetRenderer[] _dataSets;
    
    [SerializeField]
    private Transform SpawnPoint = null;
    
    [SerializeField]
    private GameObject item = null;

    [SerializeField]
    private RectTransform content = null;

    private VolumeCommandController _volumeCommandController;
    private float _maxYPosition = 0;    // Use to set the size of content to encapsulate all spawned items
  
    // Start is called before the first frame update
    void Start()
    {
       

        _volumeCommandController = FindObjectOfType<VolumeCommandController>();

        int i = 0;
        foreach (string keyword in VolumeCommandController.Keywords.All)
        {
            // 100 Height of item
            float spawnY = i * 60;
            //newSpawn Position
            Vector3 pos = new Vector3(SpawnPoint.position.x + 300, -spawnY, SpawnPoint.position.z);
            //instantiate item
            GameObject SpawnedItem = Instantiate(item, pos, Quaternion.identity);
            //setParent
            SpawnedItem.transform.SetParent(SpawnPoint, false);

            //get ItemDetails Component
            VoiceCommandListItem itemDetails = SpawnedItem.GetComponent<VoiceCommandListItem>();
          
            itemDetails.executeCommand.GetComponent<Button>().onClick.RemoveAllListeners();
            itemDetails.executeCommand.GetComponent<Button>().onClick.AddListener(delegate { _volumeCommandController.ExecuteVoiceCommandFromList(keyword); });
            
            //set name
            itemDetails.commandName.text = keyword;
           // itemDetails...= ExecuteVoiceCommandFromList()
            if (i % 2 != 0)
                itemDetails.GetComponent<Image>().color = new Color(0.4039216f, 0.5333334f, 0.5882353f, 1f);
            i++;
            _maxYPosition = spawnY;
        }
        content.sizeDelta = new Vector2(content.offsetMin.x, _maxYPosition);        //Encapsulate all spawned items with content size
        
}


    // Update is called once per frame
    void Update()
    {
      
    }

    private VolumeDataSetRenderer getFirstActiveDataSet()
    {

        foreach (var dataSet in _dataSets)
        {

            if (dataSet.isActiveAndEnabled)
            {
                return dataSet;
            }
        }
        return null;

    }

  
}
