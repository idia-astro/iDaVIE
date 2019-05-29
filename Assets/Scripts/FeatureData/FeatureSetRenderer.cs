using System.Collections;
using System.Collections.Generic;
using System.IO;
using VolumeData;


using UnityEngine;

namespace DataFeatures {
    public class FeatureSetRenderer : MonoBehaviour
    {
        public string FileName;
        public string MappingFileName;
        public Feature FeaturePrefab;
        private FeatureSetImporter _importer;
        public List<Feature> _featureList;


        // Start is called before the first frame update
        void Start()
        {
            _featureList = new List<Feature>();
        }

        // Update is called once per frame
        void Update()
        {

        }

        // Add feature to Renderer as container
        public void AddFeature(Feature featureToAdd)
        {
            _featureList.Add(featureToAdd);
            featureToAdd.transform.parent = transform;
        }

        // Spawn Feature objects intro world from FileName
        public void SpawnFeaturesFromFile()
        {
            _importer = FeatureSetImporter.CreateSetFromAscii(FileName, MappingFileName);
            for (int i = 0; i < _importer.NumberFeatures; i++)
            {
                Feature spawningObject;
                Vector3 featurePosition = _importer.FeaturePositions[i];
                Vector3 spawnPosition = GetComponentInParent<FeatureSetManager>().VolumePositionToLocalPosition(featurePosition);
                spawningObject = Instantiate(FeaturePrefab, Vector3.zero, Quaternion.identity);
                spawningObject.transform.SetParent(transform, false);
                spawningObject.transform.localPosition = spawnPosition;
                spawningObject.name = _importer.FeatureNames[i];
                _featureList.Add(spawningObject);
                
            }
        }

        // Output the features to File
        public void OutputFeaturesToFile(string FileName)
        {
            VolumeDataSet parentVolume = GetComponentInParent<VolumeDataSet>();
            string volumeName = Path.GetFileName(parentVolume.FileName);
            string[] featureData = new string[2 + _featureList.Count];
            featureData[0] = "#VR Features from Cube: " + volumeName;
            featureData[1] = "    x    y    z";
            for (int i=0; i < _featureList.Count; i++)
            {
                Vector3 featurePosition = _featureList[i].transform.position;
                Vector3 featureVolPosition = GetComponentInParent<FeatureSetManager>().LocalPositionToVolumePosition(featurePosition);
                featureData[i + 2] = $"    {featureVolPosition.x}    {featureVolPosition.y}    {featureVolPosition.z}";
            }
            System.IO.File.WriteAllLines(FileName, featureData);
        }
    }
}