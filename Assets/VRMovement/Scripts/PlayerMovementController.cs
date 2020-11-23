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
        public bool EnableTeleport;
        [Header("Joystick")]
        public float Speed = 2.0f;
        /// <summary>
        /// The rate of additional damping when moving sideways or backwards.
        /// </summary>
        public float BackAndSideDampen = 0.5f;
        /// <summary>
        /// Modifies the strength of gravity.
        /// </summary>
        public float GravityModifier = 1.0f;
        public float Acceleration = 10.0f;
        [Header("Hands")]
        public Hand HandRight;
        public Hand HandLeft;
        public Transform TrackingSpaceRightHand;
        public Transform TrackingSpaceLeftHand;
        [Header("Teleport")]
        public OVRInput.Controller TeleportController = OVRInput.Controller.RTouch;
        public OVRInput.Button TeleportButton = OVRInput.Button.PrimaryThumbstick;
        public float MaxTeleportDistance = 20.0f;
        public Material LineRendererMat;
        public LineRenderer LineRenderer;
        public bool SmoothTeleport;

        private JoystickMovement JoystickMovement;
        private CharacterController CharacterController;
        private OVRCameraRig CameraRig;
        private TeleportMovement TeleportMovement;

        private float InitialYRotation;
        private bool IsTeleportButtonDown;
        private bool IsTeleportValid;
        private Vector3 LastValidPoint;
        private int TeleportLayerMask;
        private bool BlockJoystick;
        private float TeleportTime = -1.0f;

        private void Awake()
        {
            CharacterController = GetComponent<CharacterController>();
            CameraRig = GetComponentInChildren<OVRCameraRig>();
            JoystickMovement = new JoystickMovement(CharacterController, CameraRig, transform, Speed, BackAndSideDampen, GravityModifier, Acceleration);
            TeleportMovement = new TeleportMovement(LineRendererMat, LineRenderer);
            InitialYRotation = transform.rotation.eulerAngles.y;
        }

        private void Start()
        {
            TeleportLayerMask = ~(1 << Hand.HandLeftLayer | 1 << Hand.HandRightLayer | 1 << Hand.GrabbableLayer | 1 << Hand.PlayerLayer |
                                  1 << Hand.GrabbedLayerB | 1 << Hand.GrabbedLayerL | 1 << Hand.GrabbedLayerR);
        }

        private void OnEnable()
        {
            OVRManager.display.RecenteredPose += ResetOrientation;
        }

        private void OnDisable()
        {
            OVRManager.display.RecenteredPose -= ResetOrientation;
        }

        private void OnDestroy()
        {
            if (TeleportMovement != null) TeleportMovement.Destroy();
        }

        private void Update()
        {
            Transform root = CameraRig.trackingSpace;
            Transform centerEye = CameraRig.centerEyeAnchor;

            // Rotate Y Axis
            Vector3 prevPos = root.position;
            Quaternion prevRot = root.rotation;

            transform.position = new Vector3(CameraRig.trackingSpace.position.x, transform.position.y, CameraRig.trackingSpace.position.z);
            transform.rotation = Quaternion.Euler(0.0f, centerEye.rotation.eulerAngles.y, 0.0f);

            root.position = prevPos;
            root.rotation = prevRot;

            // Adjust Height
            Vector3 p = CameraRig.transform.localPosition;
            if (OVRManager.instance.trackingOriginType == OVRManager.TrackingOrigin.FloorLevel)
            {
                p.y = -(0.5f * CharacterController.height) + CharacterController.center.y;
            }
            CameraRig.transform.localPosition = p;

            // Teleport
            if (EnableTeleport && TeleportTime == -1.0f)
            {
                if (OVRInput.Get(TeleportButton, TeleportController))
                {
                    if (Physics.Raycast(TrackingSpaceRightHand.position, TrackingSpaceRightHand.forward, out RaycastHit hit, MaxTeleportDistance, TeleportLayerMask))
                    {
                        TeleportMovement.Enable();
                        TeleportMovement.SetPositions(TrackingSpaceRightHand.position, hit.point);
                        if (Vector3.Dot(hit.normal, Vector3.up) > 0.5f)
                        {
                            TeleportMovement.SetColor(new Color(202.0f / 255, 255.0f / 255, 173.0f / 255, 180.0f / 255));
                            LastValidPoint = hit.point;
                            IsTeleportValid = true;
                        }
                        else
                        {
                            TeleportMovement.SetColor(new Color(228.0f / 255, 141.0f / 255, 141.0f / 255, 180.0f / 255));
                            IsTeleportValid = false;
                        }
                    }
                    else
                    {
                        IsTeleportValid = false;
                        TeleportMovement.Disable();
                    }
                    BlockJoystick = true;
                    IsTeleportButtonDown = true;
                }
                else if (OVRInput.GetUp(TeleportButton, TeleportController))
                {
                    if (IsTeleportButtonDown && IsTeleportValid)
                    {
                        TeleportTime = 0.0f;
                    }
                    else BlockJoystick = false;
                    TeleportMovement.Disable();
                    IsTeleportButtonDown = false;
                }
            }
        }

        private void FixedUpdate()
        {
            // Joystick
            if (EnableJoystick && !BlockJoystick)
            {
                Vector3 movement = JoystickMovement.Update(Time.fixedDeltaTime);
                if (movement != Vector3.zero)
                {
                    HandRight.Rigidbody.position = TrackingSpaceRightHand.position;
                    HandLeft.Rigidbody.position = TrackingSpaceLeftHand.position;
                }
            }

            // Teleport
            if (TeleportTime > -1.0f)
            {
                Teleport(Time.fixedDeltaTime);
            }
        }

        private Vector3 TargetPositionTeleport;
        private Vector3 StartPositionTeleport;
        private Vector3 NextPositionTeleport;
        private void Teleport(float deltaTime)
        {
            if (TeleportTime == 0.0f) TargetPositionTeleport = LastValidPoint + Vector3.up * (CharacterController.height * 0.5f);

            if (SmoothTeleport)
            {
                if (TeleportTime == 0.0f) StartPositionTeleport = transform.position;
                float targetDistance = Vector3.Distance(TargetPositionTeleport, StartPositionTeleport);

                float rotation = Mathf.Lerp(TeleportMovement.InitRotation, TeleportMovement.EndRotation, targetDistance == 0.0f ? 0.0f : Mathf.Clamp01(targetDistance / TeleportMovement.DistanceEndRotation));

                float initVelocity = targetDistance / Mathf.Sin(2.0f * rotation * Mathf.Deg2Rad);

                float vx = Mathf.Sqrt(initVelocity) * Mathf.Cos(rotation * Mathf.Deg2Rad);
                float vy = Mathf.Sqrt(initVelocity) * Mathf.Sin(rotation * Mathf.Deg2Rad);

                float flightDuration = targetDistance / vx;

                if (TeleportTime == 0.0f) NextPositionTeleport = StartPositionTeleport;
                Quaternion rot = Quaternion.LookRotation(TargetPositionTeleport - StartPositionTeleport, Vector3.up);
                if (TeleportTime < flightDuration)
                {
                    NextPositionTeleport = NextPositionTeleport + rot * (new Vector3(0.0f, (vy - TeleportTime) * deltaTime, vx * deltaTime));

                    transform.position = NextPositionTeleport;
                    HandRight.Rigidbody.position = TrackingSpaceRightHand.position;
                    HandLeft.Rigidbody.position = TrackingSpaceLeftHand.position;

                    TeleportTime += deltaTime;
                }
                else
                {
                    BlockJoystick = false;
                    TeleportTime = -1.0f;
                }
            }
            else
            {
                transform.position = TargetPositionTeleport;
                HandRight.Rigidbody.position = TrackingSpaceRightHand.position;
                HandLeft.Rigidbody.position = TrackingSpaceLeftHand.position;
                BlockJoystick = false;
                TeleportTime = -1.0f;
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
