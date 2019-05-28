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


        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
            //Vector3 objectSpacePosition = transform.TransformPoint(Vector3.zero);
            //PositionInCubetransform.position;
            //Debug.Log($"x: {objectSpacePosition.x}, y: {objectSpacePosition.y}, z: {objectSpacePosition}");
        }

        public void ImportFeatureSet()
        {
            //var featureSet = new FeatureSet();
            //featureSet.FileName = FeatureSetToLoad;
            FeatureSetRenderer featureSetRenderer;
            if (FeatureFileToLoad == "")
            {
                Debug.Log("Please enter path to feature file.");
                return;
            }
            if (FeatureMappingFile == "")
            {
                Debug.Log("Please enter path to feature mapping file.");
                return;
            }
            FeatureSetRendererPrefab.FileName = FeatureFileToLoad;
            FeatureSetRendererPrefab.MappingFileName = FeatureMappingFile;
            featureSetRenderer = Instantiate(FeatureSetRendererPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            featureSetRenderer.transform.parent = transform;
        }

        public void ExportFeatureSet(FeatureSetRenderer setToExport, string FileName)
        {

        }

        public Vector3 VolumePositionToLocalPosition(Vector3 volumePosition)
        {
            var parentVolume = GetComponentInParent<VolumeDataSetRenderer>();
            Vector3Int cubeDimensions = parentVolume.GetCubeDimensions();
            Vector3 worldPosition = new Vector3(volumePosition.x / cubeDimensions.x - 0.5f, volumePosition.y / cubeDimensions.y - 0.5f, volumePosition.z / cubeDimensions.z - 0.5f);
            return parentVolume.transform.TransformPoint(worldPosition);
        }

    }
}