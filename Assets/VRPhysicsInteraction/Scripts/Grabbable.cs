using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPhysicsInteraction
{
    [RequireComponent(typeof(Rigidbody))]
    public class Grabbable : MonoBehaviour
    {
        [Header("General Settings")]
        public bool ContinuousPhsyics = true;
        public Material OutlineMat;
        [Header("Object Physics Properties")]
        [Tooltip(@"Frequency is the speed of convergence. If damping is 1,
            frequency is the 1 / time taken to reach ~95 % of the target value.
            i.e.a frequency of 6 will bring you very close to your target
            within 1 / 6 seconds.")]
        public float Frecuency = 2.0f;
        [Tooltip(@"damping = 1, the system is critically damped
            damping > 1 the system is over damped (sluggish)
            damping is < 1 the system is under damped (it will oscillate a little)")]
        public float Damping = 2.0f;

        public bool IsGrabbed { get; private set; }
        public Rigidbody Rigidbody { get; private set; }
        public bool IsGrabbedRight { get; private set; }
        public bool IsGrabbedLeft { get; private set; }
        public int GrabCount { get { return (IsGrabbedRight ? 1 : 0) + (IsGrabbedLeft ? 1 : 0); } }

        private GameObject Outline;

        private void Awake()
        {
            // Physics
            Rigidbody = GetComponent<Rigidbody>();
            Rigidbody.collisionDetectionMode = ContinuousPhsyics ? CollisionDetectionMode.Continuous : CollisionDetectionMode.Discrete;
            // Outline
            CreateOutline();
            EnableOutline(false);
        }

        private void Start()
        {
            Utils.SetLayerRecursively(gameObject, Hand.GrabbableLayer);
        }

        public void SetGrabbed(bool value, bool isHandRight)
        {
            IsGrabbed = value;
            if (value)
            {
                if (isHandRight)
                {
                    if (IsGrabbedLeft) Utils.SetLayerRecursively(gameObject, Hand.GrabbedLayerB);
                    else Utils.SetLayerRecursively(gameObject, Hand.GrabbedLayerR);
                    IsGrabbedRight = true;
                }
                else
                {
                    if (IsGrabbedRight) Utils.SetLayerRecursively(gameObject, Hand.GrabbedLayerB);
                    else Utils.SetLayerRecursively(gameObject, Hand.GrabbedLayerL);
                    IsGrabbedLeft = true;
                }
            }
            else
            {
                if (isHandRight)
                {
                    IsGrabbedRight = false;
                    if (IsGrabbedLeft) Utils.SetLayerRecursively(gameObject, Hand.GrabbedLayerL);
                    else Utils.SetLayerRecursively(gameObject, Hand.GrabbableLayer);
                }
                else
                {
                    IsGrabbedLeft = false;
                    if (IsGrabbedRight) Utils.SetLayerRecursively(gameObject, Hand.GrabbedLayerR);
                    else Utils.SetLayerRecursively(gameObject, Hand.GrabbableLayer);
                }

            }
        }

        public void EnableOutline(bool value)
        {
            if ((!value || !Outline.activeSelf) &&
                (value || Outline.activeSelf)) Outline.SetActive(value);
        }

        private void CreateOutline()
        {
            const string outlineName = "Outline";
            Outline = new GameObject(outlineName);
            Outline.transform.SetParent(transform);
            Outline.AddComponent<MeshFilter>().sharedMesh = GetComponent<MeshFilter>().sharedMesh;
            Outline.AddComponent<MeshRenderer>().sharedMaterial = OutlineMat;
            Outline.transform.localPosition = Vector3.zero;
            Outline.transform.localRotation = Quaternion.identity;
            Outline.transform.localScale = new Vector3(1.01f, 1.01f, 1.01f);
        }
    }
}
