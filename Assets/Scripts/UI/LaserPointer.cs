/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 *
 * This file is part of the iDaVIE project.
 *
 * iDaVIE is free software: you can redistribute it and/or modify it under the terms 
 * of the GNU Lesser General Public License (LGPL) as published by the Free Software 
 * Foundation, either version 3 of the License, or (at your option) any later version.
 *
 * iDaVIE is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR 
 * PURPOSE. See the GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License along with 
 * iDaVIE in the LICENSE file. If not, see <https://www.gnu.org/licenses/>.
 *
 * Additional information and disclaimers regarding liability and third-party 
 * components can be found in the DISCLAIMER and NOTICE files included with this project.
 *
 */
// Adapted from the SteamVR Unity plugin v1.0 SteamVR_LaserPointer code

using UnityEngine;
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

    private VolumeInputController _volumeInputController;

    private void Start()
    {

        _volumeInputController = FindObjectOfType<VolumeInputController>();

        _holder = new GameObject();
        _holder.transform.parent = transform;
        _holder.transform.localPosition = Vector3.zero;
        _holder.transform.localRotation = Quaternion.identity;

        _pointer = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _pointer.transform.parent = _holder.transform;
        _pointer.transform.localScale = new Vector3(Thickness, Thickness, RayDistance);
        _pointer.transform.localPosition = new Vector3(0f, 0f, RayDistance / 2.0f);
        _pointer.transform.localRotation = Quaternion.identity;
        BoxCollider boxCollider = _pointer.GetComponent<BoxCollider>();
        if (AddRigidBody)
        {
            if (boxCollider)
            {
                boxCollider.isTrigger = true;
            }

            Rigidbody rigidBody = _pointer.AddComponent<Rigidbody>();
            rigidBody.isKinematic = true;
        }
        else
        {
            if (boxCollider)
            {
                Object.Destroy(boxCollider);
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
        {
            if (e.Target.tag == "ScrollView" && e.HandType == _volumeInputController.PrimaryHand)
            {
                _volumeInputController.scrollSelected = true;
                _volumeInputController.ScrollObject = e.Target.gameObject;
            }
            PointerIn(this, e);
        }
    }

    public virtual void OnPointerOut(PointerEventArgs e)
    {
        if (PointerOut != null)
        {
            if (e.Target.tag == "ScrollView" && e.HandType == _volumeInputController.PrimaryHand)
            {
                _volumeInputController.scrollSelected = false;
                _volumeInputController.scrollUp = false;
                _volumeInputController.scrollDown = false;
                _volumeInputController.ScrollObject = null;
            }
            PointerOut(this, e);
        }
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