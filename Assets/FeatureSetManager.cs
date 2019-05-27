using System.Collections;
using System.Collections.Generic;
using VolumeData;
using UnityEngine;


namespace DataFeatures
{
    public class FeatureSetManager : MonoBehaviour
    {

        public FeatureSetRenderer FeatureSetRendererPrefab;
        private FeatureSetRenderer featureSetRenderer;
        public string FeatureSetToLoad;


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
            FeatureSetRendererPrefab.FileName = FeatureSetToLoad;
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
            return new Vector3(volumePosition.x / cubeDimensions.x - 0.5f, volumePosition.y / cubeDimensions.y - 0.5f, volumePosition.z / cubeDimensions.z - 0.5f);
        }

    }
}