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

    /*
    public GameObject volumeDatasetRendererObj = null;
    public GameObject SofiaNewListController = null;

    private FeatureSetManager featureSetManager;
    private FeatureSetRenderer[] featureSetRenderer;
    private int numberFeatureSetRenderers;

    private int numberOfItems;

    */
    // Start is called before the first frame update
    void Start()
    {
        /*

                if (volumeDatasetRendererObj != null)
                    _dataSets = volumeDatasetRendererObj.GetComponentsInChildren<VolumeDataSetRenderer>(true);


                var firstActive = getFirstActiveDataSet();
                if (firstActive && _activeDataSet != firstActive)
                {
                    _activeDataSet = firstActive;
                }

                if (_activeDataSet != null)
                {
                    featureSetManager = _activeDataSet.GetComponentInChildren<FeatureSetManager>();
                  //  featureSetManager.ImportFeatureSet();
                }

                featureSetRenderer = featureSetManager.GetComponentsInChildren<FeatureSetRenderer>();
                numberFeatureSetRenderers = featureSetRenderer.Length;
                numberOfItems = featureSetRenderer[numberFeatureSetRenderers - 1].FeatureList.Count;
                //Debug.Log($"Number of items: {numberOfItems}");

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
                    itemDetails.idTextField.text = (i+1).ToString();

                    itemDetails.feature = featureSetRenderer[numberFeatureSetRenderers - 1].FeatureList[i];

                    if (!itemDetails.feature.Visible)
                        itemDetails.ToggleVisibility();

                    itemDetails.sourceName.text = itemDetails.feature.Name;


                    if (i%2!=0)
                        itemDetails.GetComponent<Image>().color = new Color(0.4039216f, 0.5333334f, 0.5882353f, 1f);



                }
        */

        _volumeCommandController = FindObjectOfType<VolumeCommandController>();

        int i = 0;
        foreach (string keyword in VolumeCommandController.Keywords.All)
        {
            // 100 Height of item
            float spawnY = i * 60;
            //newSpawn Position
            Vector3 pos = new Vector3(SpawnPoint.position.x + 300, -spawnY, SpawnPoint.position.z);
            //instantiate item
            GameObject SpawnedItem = Instantiate(item, pos, SpawnPoint.rotation);
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
        }
       
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
