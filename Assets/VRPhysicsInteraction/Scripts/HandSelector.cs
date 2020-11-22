using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPhysicsInteraction
{
    public class HandSelector : MonoBehaviour
    {
        public OVRInput.Controller Controller;
        public Hand Hand;
        public GameObject CubeSelector;
        public GameObject SpherePointer;
        public float MaxSelectionDistance = 20.0f;

        private bool SelectorReady;
        private int RaycastLayer;

        private GameObject CurrentSelected;
        private Grabbable CurrentGrabbableSelected;

        private void Start()
        {
            RaycastLayer = ~(1 << Hand.HandLeftLayer | 1 << Hand.HandRightLayer | 1 << Hand.PlayerLayer |
                             1 << Grabbable.GrabbedLayerB | 1 << Grabbable.GrabbedLayerL | 1 << Grabbable.GrabbedLayerR);
        }

        private void Update()
        {
            if (Hand.IsGrabbing) return;

            // Open/Close Ray
            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, Controller))
            {
                SelectorReady = true;
                EnableSelector(true);
            }
            else if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, Controller))
            {
                SelectorReady = false;
                EnableSelector(false);
            }

            // Perform Ray
            if (SelectorReady)
            {
                bool foundGrabbable = false;
                if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, MaxSelectionDistance, RaycastLayer))
                {
                    // Pointer
                    if (!SpherePointer.activeSelf) SpherePointer.SetActive(true);
                    SpherePointer.transform.position = hit.point;
                    // Grabbable
                    if (hit.collider.gameObject.layer == Grabbable.GrabbableLayer)
                    {
                        if (CurrentSelected != hit.collider.gameObject)
                        {
                            // Disable old
                            if (CurrentSelected != null && Hand.CurrentOutline?.gameObject != CurrentSelected)
                            {
                                DisableCurrentSelected();
                            }
                            // Update new
                            CurrentSelected = hit.collider.gameObject;
                            CurrentGrabbableSelected = CurrentSelected.GetComponentInParent<Grabbable>();
                            CurrentGrabbableSelected.EnableOutline(true);
                        }
                        foundGrabbable = true;
                    }
                }
                else
                {
                    if (SpherePointer.activeSelf) SpherePointer.SetActive(false);
                }

                if (!foundGrabbable)
                {
                    DisableCurrentSelected();
                }
            }

            // Grab
            if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, Controller))
            {
                if (CurrentGrabbableSelected != null)
                {
                    if (Hand.GrabDistance(transform.position, transform.forward, CurrentGrabbableSelected, MaxSelectionDistance))
                    {
                        DisableCurrentSelected();
                        EnableSelector(false);
                    }
                }
            }
        }

        private void DisableCurrentSelected()
        {
            if (CurrentSelected != null && Hand.CurrentOutline?.gameObject != CurrentSelected) CurrentGrabbableSelected.EnableOutline(false);
            CurrentSelected = null;
            CurrentGrabbableSelected = null;
        }

        private void EnableSelector(bool value)
        {
            CubeSelector.SetActive(value);
            if (!value) SpherePointer.SetActive(false);
            DisableCurrentSelected();
        }
    }
}