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

        public bool IsGrabbed { get; private set; }
        public Rigidbody Rigidbody { get; private set; }

        private void Awake()
        {
            Rigidbody = GetComponent<Rigidbody>();
            Rigidbody.collisionDetectionMode = ContinousPhsyics ? CollisionDetectionMode.Continuous : CollisionDetectionMode.Discrete;
            gameObject.layer = LayerMask.NameToLayer(GrabbableLayer);
        }

        public void SetGrabbed(bool value)
        {
            IsGrabbed = value;
        }
    }
}
