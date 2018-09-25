// Adapted from the SteamVR Unity plugin v1.0 SteamVR_LaserPointer code

using UnityEngine;
using UnityEngine.EventSystems;
using Valve.VR;
using Valve.VR.InteractionSystem;

public struct PointerEventArgs
{
    public SteamVR_Input_Sources HandType;
    public uint Flags;
    public float Distance;
    public Transform Target;
}

public delegate void PointerEventHandler(object sender, PointerEventArgs e);

public class LaserPointer : MonoBehaviour
{
    public Color Color;

    // Default ray thickness: 1 mm
    public float Thickness = 0.001f;

    // Default ray search length: 10 m
    public float RayDistance = 10f;

    // Hide the ray when inactive
    public bool HideWhenInactive = true;
    public bool AddRigidBody = false;
    public Transform Reference;
    public event PointerEventHandler PointerIn;
    public event PointerEventHandler PointerOut;

    private bool _isActive = false;
    private Transform _previousContact = null;
    private GameObject _holder;
    private GameObject _pointer;
    private Hand _hand;
    private MeshRenderer _meshRenderer;

    private void Start()
    {
        _holder = new GameObject();
        _holder.transform.parent = transform;
        _holder.transform.localPosition = Vector3.zero;
        _holder.transform.localRotation = Quaternion.identity;

        _pointer = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _pointer.transform.parent = _holder.transform;
        _pointer.transform.localScale = new Vector3(Thickness, Thickness, RayDistance);
        _pointer.transform.localPosition = new Vector3(0f, 0f, RayDistance / 2.0f);
        _pointer.transform.localRotation = Quaternion.identity;
        BoxCollider collider = _pointer.GetComponent<BoxCollider>();
        if (AddRigidBody)
        {
            if (collider)
            {
                collider.isTrigger = true;
            }

            Rigidbody rigidBody = _pointer.AddComponent<Rigidbody>();
            rigidBody.isKinematic = true;
        }
        else
        {
            if (collider)
            {
                Object.Destroy(collider);
            }
        }

        _meshRenderer = _pointer.GetComponent<MeshRenderer>();
        Material newMaterial = new Material(Shader.Find("Unlit/Color"));
        newMaterial.SetColor("_Color", Color);
        _meshRenderer.material = newMaterial;
        _hand = GetComponentInParent<Hand>();
    }

    public virtual void OnPointerIn(PointerEventArgs e)
    {
        if (PointerIn != null)
            PointerIn(this, e);
    }

    public virtual void OnPointerOut(PointerEventArgs e)
    {
        if (PointerOut != null)
            PointerOut(this, e);
    }


    // Update is called once per frame
    void Update()
    {
        if (!_isActive)
        {
            _isActive = true;
            this.transform.GetChild(0).gameObject.SetActive(true);
        }

        float hitDistance = RayDistance;

        Ray raycast = new Ray(transform.position, transform.forward);
        RaycastHit hit;
        bool bHit = Physics.Raycast(raycast, out hit);

        if (_previousContact && _previousContact != hit.transform)
        {
            PointerEventArgs args = new PointerEventArgs();
            if (_hand != null)
            {
                args.HandType = _hand.handType;
            }

            args.Distance = 0f;
            args.Flags = 0;
            args.Target = _previousContact;
            OnPointerOut(args);
            _previousContact = null;
        }

        if (bHit && _previousContact != hit.transform)
        {
            PointerEventArgs argsIn = new PointerEventArgs();
            if (_hand != null)
            {
                argsIn.HandType = _hand.handType;
            }

            argsIn.Distance = hit.distance;
            argsIn.Flags = 0;
            argsIn.Target = hit.transform;
            OnPointerIn(argsIn);
            _previousContact = hit.transform;
        }

        if (!bHit)
        {
            _previousContact = null;
        }

        if (bHit && hit.distance < RayDistance)
        {
            hitDistance = hit.distance;
            _meshRenderer.enabled = true;
        }
        else if (HideWhenInactive)
        {
            _meshRenderer.enabled = false;
        }

        if (_hand != null && _hand.uiInteractAction.GetState(_hand.handType))
        {
            _pointer.transform.localScale = new Vector3(Thickness * 2f, Thickness * 2f, hitDistance);
        }
        else
        {
            _pointer.transform.localScale = new Vector3(Thickness, Thickness, hitDistance);
        }

        _pointer.transform.localPosition = new Vector3(0f, 0f, hitDistance / 2f);
        _meshRenderer.material.SetColor("_Color", Color);
    }
}