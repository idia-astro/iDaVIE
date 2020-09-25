using UnityEngine;

namespace DataFeatures
{
    public class FeatureAnchor : MonoBehaviour
    {
        private Material _material;
        // Start is called before the first frame update
        void Start()
        {
            _material = GetComponent<Renderer>().material;
            _material.color = Color.gray;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            Debug.Log(other);
            if (other.CompareTag("cursor"))
            {
                Debug.Log($"Cursor entered {name}");
                _material.color = Color.white;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("cursor"))
            {
                Debug.Log($"Cursor exited {name}");
                _material.color = Color.gray;
            } 
        }
    }
}