using System.Collections;
using System.Collections.Generic;
using VolumeData;
using UnityEngine;


namespace DataFeatures
{
    public class FeatureSetManager : MonoBehaviour
    {

        public FeatureSetRenderer FeatureSetRendererPrefab;
        public string FeatureFileToLoad;
        public string FeatureMappingFile;

        // List containing the different FeatureSets (example: SofiaSet, CustomSet, VRSet, etc.)
        // UI will have tab for each Renderer containing the lists of Features
        private List<FeatureSetRenderer> _featureSetList;

        // Active Renderer will be "container" to add Features to if saving is desired.
        private FeatureSetRenderer _activeFeatureSetRenderer;


        // Start is called before the first frame update
        void Start()
        {
            _featureSetList = new List<FeatureSetRenderer>();
            _activeFeatureSetRenderer = null;
        }

        // Update is called once per frame
        void Update()
        {
        }


        // Creates new empty FeatureSetRenderer for adding Features
        public FeatureSetRenderer NewFeatureSet()
        {
            FeatureSetRenderer featureSetRenderer;
            featureSetRenderer = Instantiate(FeatureSetRendererPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            featureSetRenderer.transform.SetParent(transform, false);
            _featureSetList.Add(featureSetRenderer);
            if (_activeFeatureSetRenderer == null)
                _activeFeatureSetRenderer = featureSetRenderer;
            return featureSetRenderer;
        }

        // Creates FeatureSetRenderer filled with Features from file
        public FeatureSetRenderer ImportFeatureSet()
        {
            FeatureSetRenderer featureSetRenderer = null;
            if (FeatureFileToLoad == "")
            {
                Debug.Log("Please enter path to feature file.");
                return featureSetRenderer;
            }
            if (FeatureMappingFile == "")
            {
                Debug.Log("Please enter path to feature mapping file.");
                return featureSetRenderer;
            }
            FeatureSetRendererPrefab.FileName = FeatureFileToLoad;
            FeatureSetRendererPrefab.MappingFileName = FeatureMappingFile;
            featureSetRenderer = Instantiate(FeatureSetRendererPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            featureSetRenderer.transform.SetParent(transform, false);
            featureSetRenderer.SpawnFeaturesFromFile();
            _featureSetList.Add(featureSetRenderer);
            if (_activeFeatureSetRenderer == null)
                _activeFeatureSetRenderer = featureSetRenderer;
            return featureSetRenderer;
        }

        public void ExportFeatureSet(FeatureSetRenderer setToExport, string FileName)
        {

        }

        // Converts volume space to local space
        public Vector3 VolumePositionToLocalPosition(Vector3 volumePosition)
        {
            var parentVolume = GetComponentInParent<VolumeDataSetRenderer>();
            Vector3Int cubeDimensions = parentVolume.GetCubeDimensions();
            Vector3 localPosition = new Vector3(volumePosition.x / cubeDimensions.x - 0.5f, volumePosition.y / cubeDimensions.y - 0.5f, volumePosition.z / cubeDimensions.z - 0.5f);
            return localPosition;
        }
        
        // Converts local space to volume space
        public Vector3 LocalPositionToVolumePosition(Vector3 localPosition)
        {
            var parentVolume = GetComponentInParent<VolumeDataSetRenderer>();
            Vector3Int cubeDimensions = parentVolume.GetCubeDimensions();
            Vector3 volumePosition = new Vector3((localPosition.x + 0.5f) * cubeDimensions.x, (localPosition.y + 0.5f) * cubeDimensions.y, (localPosition.z + 0.5f) * cubeDimensions.z);
            return volumePosition;
        }

    }
}