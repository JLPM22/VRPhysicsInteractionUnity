using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus;

namespace VRPhysicsInteraction
{
    [RequireComponent(typeof(Rigidbody))]
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
        [Header("Physics")]
        public float RotationStrength = 20.0f;
        public float VelocityStrength = 0.2f;
        public float BreakForce = 2000.0f;

        private Rigidbody Rigidbody;
        private FixedJoint FixedJoint;

        private List<Collider> GrabColliders = new List<Collider>();
        private int GrabbableLayer;

        private Collider CurrentOutlineCollider = null;
        private Grabbable CurrentOutline = null;
        private Grabbable CurrentGrab = null;

        private Finger[] FingersColliders;

        private void Awake()
        {
            // References
            Rigidbody = GetComponent<Rigidbody>();
            FingersColliders = new Finger[Fingers.Length];
            for (int f = 0; f < Fingers.Length; ++f)
            {
                FingersColliders[f] = Fingers[f].GetComponentInChildren<Finger>();
                Debug.Assert(FingersColliders[f] != null, "Assign a Finger component to all fingers of your hand");
            }
            // Layers
            GrabbableLayer = LayerMask.NameToLayer(Grabbable.GrabbableLayer);
            // Reset Fingers
            for (int f = 0; f < Fingers.Length; ++f) SetFingersInterpolation(f, 0.0f);
            // Physics
            Rigidbody.centerOfMass = Vector3.zero;
        }

        private void Update()
        {
            if (GrabColliders.Count > 0)
            {
                if (CurrentGrab == null && GrabColliders[0] != CurrentOutlineCollider)
                {
                    CurrentOutlineCollider = GrabColliders[0];
                    CurrentOutline = GrabColliders[0].GetComponent<Grabbable>();
                }

                CurrentOutline?.EnableOutline(true);

                if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, Controller))
                {
                    CurrentGrab = CurrentOutline;
                    if (FixCurrentGrabbed())
                    {
                        CurrentGrab.SetGrabbed(true);
                        CurrentOutline.EnableOutline(false);
                        CurrentOutlineCollider = null;
                        CurrentOutline = null;
                    }
                    else
                    {
                        CurrentGrab = null;
                    }
                }
                else if (CurrentGrab != null && OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger, Controller))
                {
                    ReleaseCurrentGrabbed();
                }
            }
            else
            {
                CurrentOutline?.EnableOutline(false);
                CurrentOutlineCollider = null;
                CurrentOutline = null;
            }
        }

        private void FixedUpdate()
        {
            Move();
            Rotate();
        }

        /* https://digitalopus.ca/site/pd-controllers/ */
        private void Move()
        {
            Vector3 targetPos = TrackingSpace.position;
            float distanceToTarget = Vector3.Distance(targetPos, transform.position);

            const float epsilon = 0.01f;
            if (distanceToTarget < epsilon)
            {
                transform.position = targetPos;
                distanceToTarget = 0.0f;
            }

            // This is an impulse, we are not calculating an acceleration but a velocity
            float dt = Time.fixedDeltaTime / VelocityStrength;
            Vector3 force = Rigidbody.mass * (targetPos - transform.position - Rigidbody.velocity * dt) / dt;
            Rigidbody.AddForce(force, ForceMode.Impulse);
        }

        /* https://digitalopus.ca/site/pd-controllers/ */
        private void Rotate()
        {
            Quaternion targetRot = TrackingSpace.rotation;
            const float frequency = 3.0f;
            const float damping = 3.5f;
            const float kpConst = (6.0f * frequency) * (6.0f * frequency) * 0.25f;
            float kp = kpConst * RotationStrength;
            const float kd = 4.5f * frequency * damping;
            float dt = Time.fixedDeltaTime;
            float diff = Quaternion.Angle(Rigidbody.rotation, targetRot);
            targetRot = Quaternion.Lerp(Rigidbody.rotation, targetRot, Mathf.Clamp(diff, 0.0f, 2.5f) / 3.0f);
            Quaternion q = targetRot * Quaternion.Inverse(Rigidbody.rotation);

            // Q can be the-long-rotation-around-the-sphere eg. 350 degrees
            // We want the equivalant short rotation eg. -10 degrees
            // Check if rotation is greater than 190 degees == q.w is negative
            if (q.w < 0)
            {
                // Convert the quaterion to eqivalent "short way around" quaterion
                q.x = -q.x;
                q.y = -q.y;
                q.z = -q.z;
                q.w = -q.w;
            }

            q.ToAngleAxis(out float mag, out Vector3 axis);
            axis.Normalize();
            // w is the angular velocity we need to achieve
            Vector3 w = kp * axis * mag * Mathf.Deg2Rad - kd * Rigidbody.angularVelocity;
            Quaternion rotIntertiaToWorld = Rigidbody.inertiaTensorRotation * Rigidbody.rotation;
            w = Quaternion.Inverse(rotIntertiaToWorld) * w;
            w.Scale(Rigidbody.inertiaTensor);
            w = rotIntertiaToWorld * w;
            Rigidbody.AddTorque(w);
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
            CurrentGrab.SetGrabbed(false);
            CurrentGrab = null;
            for (int f = 0; f < Fingers.Length; ++f) SetFingersInterpolation(f, 0.0f);
            if (FixedJoint != null) GameObject.Destroy(FixedJoint);
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
            FixedJoint = gameObject.AddComponent<FixedJoint>();
            FixedJoint.connectedBody = rb;
            FixedJoint.breakForce = float.PositiveInfinity;
            FixedJoint.breakTorque = float.PositiveInfinity;
            FixedJoint.connectedMassScale = 1;
            FixedJoint.massScale = 1;
            FixedJoint.enableCollision = false;
            FixedJoint.enablePreprocessing = false;
            yield return WaitForFixedUpdate;
            FixedJoint.breakForce = BreakForce;
            FixedJoint.breakTorque = BreakForce;

            // FixedJoint f = rb.gameObject.AddComponent<FixedJoint>();
            // f.connectedBody = Rigidbody;
            // f.breakForce = float.PositiveInfinity;
            // f.breakTorque = float.PositiveInfinity;
            // f.connectedMassScale = 1;
            // f.massScale = 1;
            // f.enableCollision = false;
            // f.enablePreprocessing = false;
        }

        private void OnJointBreak(float breakForce)
        {
            ReleaseCurrentGrabbed();
        }
    }
}