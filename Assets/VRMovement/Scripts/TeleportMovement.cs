using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRMovement
{
    public class TeleportMovement
    {
        public float SegmentLength = 1.0f;
        public float GapPercentage = 0.25f;
        public float InitRotation = 0.0f;
        public float EndRotation = 25.0f;
        public float DistanceEndRotation = 15.0f;
        public float TimeStep = 0.1f;
        public float Width = 0.01f;
        public float Speed = 2.5f;

        private LineRenderer LineRenderer;
        private Vector3 InitPosition, EndPosition;
        private Material InstanceMaterial;

        public TeleportMovement(Material lineRendererMat, LineRenderer lineRenderer)
        {
            LineRenderer = lineRenderer;
            LineRenderer.widthMultiplier = Width;
            LineRenderer.sharedMaterial = lineRendererMat;
            InstanceMaterial = LineRenderer.material;
            LineRenderer.enabled = false;
        }

        public void Destroy()
        {
            if (InstanceMaterial != null) GameObject.Destroy(InstanceMaterial);
        }

        public void SetColor(Color color)
        {
            InstanceMaterial.SetColor("_Color", color);
        }

        public void SetInitialPosition(Vector3 pos)
        {
            InitPosition = pos;
            ComputeLineRenderer();
        }

        public void SetEndPosition(Vector3 pos)
        {
            EndPosition = pos;
            ComputeLineRenderer();
        }

        public void SetPositions(Vector3 init, Vector3 end)
        {
            InitPosition = init;
            EndPosition = end;
            ComputeLineRenderer();
        }

        public void Enable()
        {
            if (!LineRenderer.enabled) LineRenderer.enabled = true;
        }

        public void Disable()
        {
            if (LineRenderer.enabled) LineRenderer.enabled = false;
        }

        private void ComputeLineRenderer()
        {
            float targetDistance = Vector3.Distance(EndPosition, InitPosition);

            float rotation = Mathf.Lerp(InitRotation, EndRotation, targetDistance == 0.0f ? 0.0f : Mathf.Clamp01(targetDistance / DistanceEndRotation));

            float initVelocity = targetDistance / Mathf.Sin(2.0f * rotation * Mathf.Deg2Rad);

            float vx = Mathf.Sqrt(initVelocity) * Mathf.Cos(rotation * Mathf.Deg2Rad);
            float vy = Mathf.Sqrt(initVelocity) * Mathf.Sin(rotation * Mathf.Deg2Rad);

            float flightDuration = targetDistance / vx;

            float time = 0;
            int i = 0;
            Vector3 nextPos = InitPosition;
            Quaternion rot = Quaternion.LookRotation(EndPosition - InitPosition, Vector3.up);
            float length = 0.0f;

            while (time < flightDuration)
            {
                LineRenderer.positionCount = i + 1;
                LineRenderer.SetPosition(i++, nextPos);

                Vector3 previousPos = nextPos;
                nextPos = nextPos + rot * (new Vector3(0.0f, (vy - time) * TimeStep, vx * TimeStep));
                length += Vector3.Distance(nextPos, previousPos);

                time += TimeStep;
            }

            InstanceMaterial.SetFloat("_NumberSegments", Mathf.CeilToInt(length / SegmentLength));
            InstanceMaterial.SetFloat("_GapPer", GapPercentage);
            InstanceMaterial.SetFloat("_Speed", Speed);
        }
    }
}