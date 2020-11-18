using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPhysicsInteraction
{
    [RequireComponent(typeof(Rigidbody))]
    public class Grabbable : MonoBehaviour
    {
        public const string GrabbableLayer = "Grabbable";

        public bool ContinousPhsyics = true;
        public Material OutlineMat;

        public bool IsGrabbed { get; private set; }
        public Rigidbody Rigidbody { get; private set; }

        private GameObject Outline;

        private void Awake()
        {
            // Physics
            Rigidbody = GetComponent<Rigidbody>();
            Rigidbody.collisionDetectionMode = ContinousPhsyics ? CollisionDetectionMode.Continuous : CollisionDetectionMode.Discrete;
            // Layers
            gameObject.layer = LayerMask.NameToLayer(GrabbableLayer);
            // Outline
            CreateOutline();
            EnableOutline(false);
        }

        public void SetGrabbed(bool value)
        {
            IsGrabbed = value;
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
            Outline.transform.localScale = new Vector3(1.01f, 1.01f, 1.01f);
        }
    }
}
