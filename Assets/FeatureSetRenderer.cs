using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace DataFeatures {
    public class FeatureSetRenderer : MonoBehaviour
    {
        public string FileName;
        public FeatureSet FeatureSet { get; private set; }

    // Start is called before the first frame update
    void Start()
        {
            FeatureSet = FeatureSet.CreateSetFromAscii(FileName);
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}