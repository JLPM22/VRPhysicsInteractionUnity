using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus;

namespace VRPhysicsInteraction
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(FixedJoint))]
    public class Hand : MonoBehaviour
    {
        [Header("General")]
        public OVRInput.Controller Controller;
        public Transform TrackingSpace;
        [Header("Palm")]
        [Tooltip("Transform representing the palm, blue axis pointing perpendicular to the plane defined by the palm")]
        public Transform Palm;
        public float PalmRadius;
        [Header("Fingers")]
        public FingersScriptableObject FingersSettings;
        public int JointsPerFinger = 3;
        public Transform[] Fingers;
        public float FingersInterpolationStep = 0.1f;

        private Rigidbody Rigidbody;
        private FixedJoint FixedJoint;
        private Rigidbody EmptyRigidbody;

        private List<Collider> GrabColliders = new List<Collider>();
        private int GrabbableLayer;

        private Grabbable CurrentGrab = null;

        private Finger[] FingersColliders;

        private void Awake()
        {
            // References
            Rigidbody = GetComponent<Rigidbody>();
            FixedJoint = GetComponent<FixedJoint>();
            FingersColliders = new Finger[Fingers.Length];
            for (int f = 0; f < Fingers.Length; ++f)
            {
                FingersColliders[f] = Fingers[f].GetComponentInChildren<Finger>();
                Debug.Assert(FingersColliders[f] != null, "Assign a Finger component to all fingers of your hand");
            }
            // Create Empty Rigidbody
            GameObject emptyRigidbodyGameObject = new GameObject("Empty Rigidbody");
            EmptyRigidbody = emptyRigidbodyGameObject.AddComponent<Rigidbody>();
            emptyRigidbodyGameObject.transform.SetParent(transform);
            emptyRigidbodyGameObject.transform.localPosition = Vector3.zero;
            FixedJoint.connectedBody = EmptyRigidbody;
            // Layers
            GrabbableLayer = LayerMask.NameToLayer(Grabbable.GrabbableLayer);
            // Reset Fingers
            for (int f = 0; f < Fingers.Length; ++f) SetFingersInterpolation(f, 0.0f);
        }

        private void Update()
        {
            if (GrabColliders.Count > 0 && OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, Controller))
            {
                CurrentGrab = GrabColliders[0].GetComponent<Grabbable>();
                if (FixCurrentGrabbed())
                {
                    CurrentGrab.SetGrabbed(true);
                }
                else
                {
                    CurrentGrab = null;
                }
            }
            else if (CurrentGrab != null && OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger, Controller))
            {
                ReleaseCurrentGrabbed();
                CurrentGrab.SetGrabbed(false);
                CurrentGrab = null;
            }
        }

        private void FixedUpdate()
        {
            Rigidbody.MovePosition(TrackingSpace.position);
            Rigidbody.MoveRotation(TrackingSpace.rotation);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == GrabbableLayer)
            {
                GrabColliders.Add(other);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.layer == GrabbableLayer)
            {
                GrabColliders.Remove(other);
            }
        }

        /// <summary>
        /// Return true if the palm was adjusted, false otherwise
        /// </summary>
        private bool AdjustPalm()
        {
            if (Physics.SphereCast(Palm.position - Palm.forward * PalmRadius * 2, PalmRadius, Palm.forward, out RaycastHit hit, PalmRadius * 3, 1 << GrabbableLayer))
            {
                Vector3 offset = hit.point - Palm.position;
                // TODO: Temporal
                CurrentGrab.Rigidbody.MovePosition(CurrentGrab.Rigidbody.position - offset);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Return true if the current grabbed was successfully grabbed, false otherwhise
        /// </summary>
        private bool FixCurrentGrabbed()
        {
            if (AdjustPalm())
            {
                StartCoroutine(AdjustFingers());
                StartCoroutine(FixJointRigidbody(CurrentGrab.Rigidbody));
                return true;
            }
            return false;
        }

        private IEnumerator AdjustFingers()
        {
            yield return WaitForFixedUpdate;
            for (int f = 0; f < Fingers.Length; ++f)
            {
                Finger finger = FingersColliders[f];
                for (float t = 0.0f; t <= 1.0f; t += FingersInterpolationStep)
                {
                    SetFingersInterpolation(f, t);
                    if (Physics.CheckSphere(finger.transform.position, finger.Radius, 1 << GrabbableLayer))
                    {
                        break;
                    }
                }
            }
        }

        private void ReleaseCurrentGrabbed()
        {
            for (int f = 0; f < Fingers.Length; ++f) SetFingersInterpolation(f, 0.0f);
            FixedJoint.connectedBody = EmptyRigidbody;
            EmptyRigidbody.position = Vector3.zero; // Bug: position at infinite ?
        }

        private void SetFingersInterpolation(int finger, float t)
        {
            Transform fingerTransform = Fingers[finger];
            for (int j = 0; j < JointsPerFinger; ++j)
            {
                fingerTransform.localRotation = Quaternion.Slerp(FingersSettings.OpenLocalRotations[finger * JointsPerFinger + j],
                                                        FingersSettings.CloseLocalRotations[finger * JointsPerFinger + j],
                                                        t);
                fingerTransform = fingerTransform.GetChild(0);
            }
        }


        private WaitForFixedUpdate WaitForFixedUpdate = new WaitForFixedUpdate();
        private IEnumerator FixJointRigidbody(Rigidbody rb)
        {
            yield return WaitForFixedUpdate;
            FixedJoint.connectedBody = rb;
        }
    }
}