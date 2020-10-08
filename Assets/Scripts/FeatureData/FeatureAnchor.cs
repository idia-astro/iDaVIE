using UnityEngine;

namespace DataFeatures
{
    public class FeatureAnchor : MonoBehaviour
    {
        private Material _material;
        private static readonly Color DefaultColor = Color.red;
        private static readonly Color HoverColor = new Color(0, 0.8f, 0.4f);
        private static readonly int EmissionProperty = Shader.PropertyToID("_EmissionColor");

        void Start()
        {
            _material = GetComponent<Renderer>().material;
            _material.EnableKeyword("_EMISSION");
            _material.SetColor(EmissionProperty, DefaultColor);
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("cursor"))
            {
                var featureSetManager = GetComponentInParent<FeatureSetManager>();
                var inputController = FindObjectOfType<VolumeInputController>();
                _material.SetColor(EmissionProperty, HoverColor);
                inputController?.SetHoveredFeature(featureSetManager, this);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("cursor"))
            {
                var featureSetManager = GetComponentInParent<FeatureSetManager>();
                var inputController = FindObjectOfType<VolumeInputController>();
                _material.SetColor(EmissionProperty, DefaultColor);
                inputController?.ClearHoveredFeature(featureSetManager, this);
            } 
        }
    }
}