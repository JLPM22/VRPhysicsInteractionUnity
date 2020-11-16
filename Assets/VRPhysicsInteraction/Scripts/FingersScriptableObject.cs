using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPhysicsInteraction
{
    [CreateAssetMenu(fileName = "NewFingersSettings", menuName = "VRPhysicsHand/FingerSettings")]
    public class FingersScriptableObject : ScriptableObject
    {
        public Quaternion[] OpenLocalRotations; // Fingers, Joints per Finger
        public Quaternion[] CloseLocalRotations;
    }
}