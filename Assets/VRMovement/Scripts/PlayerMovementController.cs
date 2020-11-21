using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRPhysicsInteraction;

namespace VRMovement
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovementController : MonoBehaviour
    {
        [Header("Movement Types")]
        public bool EnableJoystick;
        [Header("Joystick")]
        public float Speed = 1.0f;
        /// <summary>
        /// The rate of additional damping when moving sideways or backwards.
        /// </summary>
        public float BackAndSideDampen = 0.5f;
        /// <summary>
        /// Modifies the strength of gravity.
        /// </summary>
        public float GravityModifier = 1.0f;
        public float Acceleration = 2.0f;
        [Header("Hands")]
        public Hand HandRight;
        public Hand HandLeft;
        public Transform TrackinSpaceRightHand;
        public Transform TrackinSpaceLeftHand;

        private JoystickMovement JoystickMovement;
        private CharacterController CharacterController;
        private OVRCameraRig CameraRig;

        private float InitialYRotation;

        private void Awake()
        {
            CharacterController = GetComponent<CharacterController>();
            CameraRig = GetComponentInChildren<OVRCameraRig>();
            JoystickMovement = new JoystickMovement(CharacterController, CameraRig, transform, Speed, BackAndSideDampen, GravityModifier, Acceleration);
            InitialYRotation = transform.rotation.eulerAngles.y;
        }

        private void OnEnable()
        {
            OVRManager.display.RecenteredPose += ResetOrientation;
        }

        private void OnDisable()
        {
            OVRManager.display.RecenteredPose -= ResetOrientation;
        }

        private void Update()
        {
            if (EnableJoystick)
            {
                JoystickMovement.Update(Time.deltaTime);
                HandRight.Rigidbody.position = TrackinSpaceRightHand.position;
                HandLeft.Rigidbody.position = TrackinSpaceLeftHand.position;
            }
        }

        private void ResetOrientation()
        {
            Vector3 euler = transform.rotation.eulerAngles;
            euler.y = InitialYRotation;
            transform.rotation = Quaternion.Euler(euler);
        }
    }
}
