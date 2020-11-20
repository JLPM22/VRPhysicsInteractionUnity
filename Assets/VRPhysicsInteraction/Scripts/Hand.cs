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
            // Reset Fingers
            for (int f = 0; f < Fingers.Length; ++f) SetFingersInterpolation(f, 0.0f);
            // Physics
            Rigidbody.centerOfMass = Vector3.zero;
        }

        private void Update()
        {

            // Grab / Release
            if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, Controller))
            {
                Grab();
            }
            else if (CurrentGrab != null && OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger, Controller))
            {
                ReleaseCurrentGrabbed();
            }

            // Outline
            if (GrabColliders.Count > 0)
            {
                if (CurrentGrab == null && GrabColliders[0] != CurrentOutlineCollider)
                {
                    CurrentOutlineCollider = GrabColliders[0];
                    CurrentOutline = GrabColliders[0].GetComponentInParent<Grabbable>();
                }

                CurrentOutline?.EnableOutline(true);
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
            float dt = Time.fixedDeltaTime;
            Quaternion targetRot = TrackingSpace.rotation;
            // kp and kp constants
            float frequency = CurrentGrab?.Frecuency ?? 2.0f;
            float damping = CurrentGrab?.Damping ?? 2.0f;
            float kp = (6.0f * frequency) * (6.0f * frequency) * 0.25f * RotationStrength;
            float kd = 4.5f * frequency * damping;
            // Clamp maximum rotation
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
            int oppositeLayer = Controller == OVRInput.Controller.RTouch ? Grabbable.GrabbedLayerL : Grabbable.GrabbedLayerR;
            if (other.gameObject.layer == Grabbable.GrabbableLayer || other.gameObject.layer == oppositeLayer)
            {
                GrabColliders.Add(other);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            int test = other.gameObject.layer & (Grabbable.GrabbableLayer |
                                                 Grabbable.GrabbedLayerB |
                                                 Grabbable.GrabbedLayerL |
                                                 Grabbable.GrabbedLayerR);
            if (test != 0)
            {
                GrabColliders.Remove(other);
            }
        }

        private void Grab()
        {
            if (CurrentOutline != null)
            {
                CurrentGrab = CurrentOutline;
                if (FixCurrentGrabbed())
                {
                    CurrentGrab.SetGrabbed(true, Controller == OVRInput.Controller.RTouch);
                    CurrentOutline.EnableOutline(false);
                    CurrentOutlineCollider = null;
                    CurrentOutline = null;
                }
                else
                {
                    CurrentGrab = null;
                }
            }
        }

        /// <summary>
        /// Return true if the palm was adjusted, false otherwise
        /// </summary>
        private bool AdjustPalm()
        {
            int oppositeLayer = Controller == OVRInput.Controller.RTouch ? Grabbable.GrabbedLayerL : Grabbable.GrabbedLayerR;
            if (Physics.SphereCast(Palm.position - Palm.forward * PalmRadius * 2, PalmRadius, Palm.forward, out RaycastHit hit, PalmRadius * 3, 1 << Grabbable.GrabbableLayer | 1 << oppositeLayer))
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
                    if (Physics.CheckSphere(finger.transform.position, finger.Radius, 1 << Grabbable.GrabbableLayer))
                    {
                        break;
                    }
                }
            }
        }

        private void ReleaseCurrentGrabbed()
        {
            CurrentGrab.SetGrabbed(false, Controller == OVRInput.Controller.RTouch);
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
            for (int i = 0; i < 90; ++i) yield return WaitForFixedUpdate; // Wait some frames before applying break forces
            if (FixedJoint != null)
            {
                FixedJoint.breakForce = BreakForce;
                FixedJoint.breakTorque = BreakForce;
            }
        }

        private void OnJointBreak(float breakForce)
        {
            ReleaseCurrentGrabbed();
        }
    }
}