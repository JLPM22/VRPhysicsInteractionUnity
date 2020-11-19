using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPhysicsInteraction
{
    public static class Utils
    {
        public static void SetLayerRecursively(GameObject gO, int layer)
        {
            foreach (Transform t in gO.GetComponentsInChildren<Transform>())
            {
                t.gameObject.layer = layer;
            }
        }
    }
}
