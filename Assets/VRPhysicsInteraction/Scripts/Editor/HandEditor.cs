using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace VRPhysicsInteraction
{
    [CustomEditor(typeof(Hand))]
    [CanEditMultipleObjects]
    public class HandEditor : Editor
    {
        private float HandInterpolation = 1.0f; // 0.0f - Closed Hand / 1.0f - Opened Hand
        private bool Interpolate = false;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            Hand hand = (Hand)target;

            SerializedProperty fingersSettingsProperty = serializedObject.FindProperty("FingersSettings");
            SerializedObject scriptableObjectSO = new SerializedObject(fingersSettingsProperty.objectReferenceValue);
            SerializedProperty openArray = scriptableObjectSO.FindProperty("OpenLocalRotations");
            SerializedProperty closeArray = scriptableObjectSO.FindProperty("CloseLocalRotations");

            GUILayout.Space(10.0f);

            EditorGUILayout.LabelField("Pose Settings", EditorStyles.boldLabel);

            if (GUILayout.Button("Save Open Pose"))
            {
                SaveFingersLocalRotations(hand, openArray);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Save Close Pose"))
            {
                SaveFingersLocalRotations(hand, closeArray);
            }

            EditorGUILayout.Space();

            if (hand.FingersSettings.OpenLocalRotations != null && hand.FingersSettings.CloseLocalRotations != null)
            {
                Interpolate = EditorGUILayout.Toggle("Interpolate", Interpolate);
                if (Interpolate)
                {
                    HandInterpolation = EditorGUILayout.Slider("Hand Interpolation", HandInterpolation, 0.0f, 1.0f);
                    ShowFingersLocalRotations(hand);
                }
            }

            scriptableObjectSO.ApplyModifiedProperties();
        }

        private void ShowFingersLocalRotations(Hand hand)
        {
            for (int f = 0; f < hand.Fingers.Length; ++f)
            {
                Transform finger = hand.Fingers[f];
                for (int j = 0; j < hand.JointsPerFinger; ++j)
                {
                    finger.localRotation = Quaternion.Slerp(hand.FingersSettings.OpenLocalRotations[f * hand.JointsPerFinger + j],
                                                            hand.FingersSettings.CloseLocalRotations[f * hand.JointsPerFinger + j],
                                                            HandInterpolation);
                    finger = finger.GetChild(0);
                }
            }
        }

        private void SaveFingersLocalRotations(Hand hand, SerializedProperty array)
        {
            array.arraySize = hand.Fingers.Length * hand.JointsPerFinger;
            for (int f = 0; f < hand.Fingers.Length; ++f)
            {
                Transform finger = hand.Fingers[f];
                for (int j = 0; j < hand.JointsPerFinger; ++j)
                {
                    array.GetArrayElementAtIndex(f * hand.JointsPerFinger + j).quaternionValue = finger.localRotation;
                    finger = finger.GetChild(0);
                }
            }
        }
    }
}
