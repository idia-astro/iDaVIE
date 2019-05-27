using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace DataFeatures {
    public class FeatureSetRenderer : MonoBehaviour
    {
        public string FileName;
        public GameObject PrefabToSpawn;
        public FeatureSet FeatureSet { get; private set; }

    // Start is called before the first frame update
    void Start()
        {
            FeatureSet = FeatureSet.CreateSetFromAscii(FileName);
            SpawnFeatureMarkers();
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void SpawnFeatureMarkers()
        {
            for (int i = 0; i < FeatureSet.NumberFeatures; i++)
            {
                GameObject spawningObject;
                Vector3 featurePosition = FeatureSet.FeaturePositions[i];
                Vector3 spawnPosition = GetComponentInParent<FeatureSetManager>().VolumePositionToLocalPosition(featurePosition);
                spawningObject = Instantiate(PrefabToSpawn, spawnPosition, Quaternion.identity);
                spawningObject.transform.parent = transform;
            }
        }

    }
}