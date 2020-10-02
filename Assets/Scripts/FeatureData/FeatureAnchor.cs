using UnityEngine;

namespace DataFeatures
{
    public class FeatureAnchor : MonoBehaviour
    {
        private Material _material;
        
        void Start()
        {
            _material = GetComponent<Renderer>().material;
            _material.color = Color.gray;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("cursor"))
            {
                var featureSetManager = GetComponentInParent<FeatureSetManager>();
                var inputController = FindObjectOfType<VolumeInputController>();
                _material.color = Color.white;
                inputController?.SetHoveredFeature(featureSetManager, this);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("cursor"))
            {
                var featureSetManager = GetComponentInParent<FeatureSetManager>();
                var inputController = FindObjectOfType<VolumeInputController>();
                _material.color = Color.gray;
                inputController?.ClearHoveredFeature(featureSetManager, this);
            } 
        }
    }
}