using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRMovement
{
    public class JoystickMovement
    {
        private CharacterController CharacterController;
        private OVRCameraRig CameraRig;
        private Transform Transform;

        private Vector3 PreviousMovementDir;
        private float Acceleration;
        private float MaxSpeed = 0.0f;
        private float FallSpeed = 0.0f;
        private float Speed;
        private float BackAndSideDampen;
        private float GravityModifier;

        public JoystickMovement(CharacterController characterController, OVRCameraRig cameraRig, Transform transform,
                                float speed, float backAndSideDampen, float gravityModifier, float acceleration)
        {
            CharacterController = characterController;
            CameraRig = cameraRig;
            Transform = transform;
            Speed = speed;
            BackAndSideDampen = backAndSideDampen;
            GravityModifier = gravityModifier;
            Acceleration = acceleration;
        }

        public Vector3 Update(float deltaTime)
        {
            // Movement
            Vector3 moveDirection = UpdateMovement(deltaTime);

            if (moveDirection == Vector3.zero && MaxSpeed > 0.0f) moveDirection = PreviousMovementDir;
            PreviousMovementDir = moveDirection;
            moveDirection *= Speed * deltaTime * MaxSpeed;

            // Gravity
            if (CharacterController.isGrounded)
                FallSpeed = 0.0f;
            else
                FallSpeed += Physics.gravity.y * GravityModifier * deltaTime;

            moveDirection.y += FallSpeed * deltaTime;

            Vector3 returnMoveDirection = moveDirection;

            if (CharacterController.isGrounded)
            {
                // Offset correction for uneven ground
                float bumpUpOffset = Mathf.Max(CharacterController.stepOffset, new Vector3(moveDirection.x, 0, moveDirection.z).magnitude);
                moveDirection -= bumpUpOffset * Vector3.up;
            }

            // Move contoller
            CharacterController.Move(moveDirection);

            return returnMoveDirection;
        }

        private Vector3 UpdateMovement(float deltaTime)
        {
            Vector3 direction = Vector3.zero;

            float moveScale = 1.0f;
            if (!CharacterController.isGrounded)
                moveScale = 0.0f;

            // Position
            Vector2 primaryAxis = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);

            Quaternion ort = Transform.rotation;
            Vector3 ortEuler = ort.eulerAngles;
            ortEuler.z = ortEuler.x = 0f;
            ort = Quaternion.Euler(ortEuler);

            if (primaryAxis.y > 0.0f)
                direction += ort * (primaryAxis.y * moveScale * Vector3.forward);

            if (primaryAxis.y < 0.0f)
                direction += ort * (Mathf.Abs(primaryAxis.y) * moveScale *
                                       BackAndSideDampen * Vector3.back);

            if (primaryAxis.x < 0.0f)
                direction += ort * (Mathf.Abs(primaryAxis.x) * moveScale *
                                       BackAndSideDampen * Vector3.left);

            if (primaryAxis.x > 0.0f)
                direction += ort * (primaryAxis.x * BackAndSideDampen * moveScale *
                                       Vector3.right);

            if (direction == Vector3.zero)
            {
                MaxSpeed -= Acceleration * deltaTime;
                if (MaxSpeed < 0.0f) MaxSpeed = 0.0f;
            }
            else if (MaxSpeed < 1.0f)
            {
                MaxSpeed += Acceleration * deltaTime;
                if (MaxSpeed > 1.0f) MaxSpeed = 1.0f;
            }

            return direction;
        }
    }
}