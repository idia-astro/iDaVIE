using System.Collections;
using System.Collections.Generic;
using Vectrosity;
using UnityEngine;

// Feature is the basic unit of marking up the volume
public class Feature : MonoBehaviour
{
    private VectorLine _boundingBox;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void MoveToPosition(Vector3 position)
    {
        transform.localPosition = position;
    }

}
