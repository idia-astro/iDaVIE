using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace DataFeatures {
    public class FeatureSetRenderer : MonoBehaviour
    {

        public string FileName;
        public object SpawnObject;
        public FeatureSet FeatureSet { get; private set; }
        public int NumberFeatures { get; private set; }

    // Start is called before the first frame update
    void Start()
        {
            FeatureSet = FeatureSet.CreateSetFromAscii(FileName);
            NumberFeatures = FeatureSet.Features.Length;
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}