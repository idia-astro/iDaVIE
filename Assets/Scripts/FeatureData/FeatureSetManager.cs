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

        private List<FeatureSetRenderer> _featureSetList;


        // Start is called before the first frame update
        void Start()
        {
            _featureSetList = new List<FeatureSetRenderer>();
        }

        // Update is called once per frame
        void Update()
        {
            //Vector3 objectSpacePosition = transform.TransformPoint(Vector3.zero);
            //PositionInCubetransform.position;
            //Debug.Log($"x: {objectSpacePosition.x}, y: {objectSpacePosition.y}, z: {objectSpacePosition}");
        }

        public FeatureSetRenderer NewFeatureSet()
        {
            FeatureSetRenderer featureSetRenderer;
            featureSetRenderer = Instantiate(FeatureSetRendererPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            featureSetRenderer.transform.parent = transform;
            featureSetRenderer.transform.localPosition = Vector3.zero;
            featureSetRenderer.transform.localRotation = Quaternion.identity;
            featureSetRenderer.transform.localScale = new Vector3(1, 1, 1);
            _featureSetList.Add(featureSetRenderer);
            return featureSetRenderer;
        }


        public FeatureSetRenderer ImportFeatureSet()
        {
            //var featureSet = new FeatureSet();
            //featureSet.FileName = FeatureSetToLoad;
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
            featureSetRenderer.transform.parent = transform;
            featureSetRenderer.transform.localPosition = Vector3.zero;
            featureSetRenderer.transform.localRotation = Quaternion.identity;
            featureSetRenderer.transform.localScale = new Vector3(1, 1, 1);
            featureSetRenderer.SpawnFeaturesFromFile();
            _featureSetList.Add(featureSetRenderer);
            return featureSetRenderer;
        }

        public void ExportFeatureSet(FeatureSetRenderer setToExport, string FileName)
        {

        }

        public Vector3 VolumePositionToWorldPosition(Vector3 volumePosition)
        {
            var parentVolume = GetComponentInParent<VolumeDataSetRenderer>();
            Vector3Int cubeDimensions = parentVolume.GetCubeDimensions();
            Vector3 localPosition = new Vector3(volumePosition.x / cubeDimensions.x - 0.5f, volumePosition.y / cubeDimensions.y - 0.5f, volumePosition.z / cubeDimensions.z - 0.5f);
            return localPosition;
            //return parentVolume.transform.TransformPoint(localPosition);
        }

        public Vector3 LocalPositionToVolumePosition(Vector3 localPosition)
        {
            var parentVolume = GetComponentInParent<VolumeDataSetRenderer>();
            Vector3Int cubeDimensions = parentVolume.GetCubeDimensions();
            Vector3 volumePosition = new Vector3((localPosition.x + 0.5f) * cubeDimensions.x, (localPosition.y + 0.5f) * cubeDimensions.y, (localPosition.z + 0.5f) * cubeDimensions.z);
            return volumePosition;
        }

    }
}