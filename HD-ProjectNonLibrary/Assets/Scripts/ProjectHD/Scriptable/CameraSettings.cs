using System;
using UnityEngine;

namespace ProjectHD
{
    [CreateAssetMenu(fileName = "CameraSettings", menuName = "ScriptableObjects/CameraSettings")]
    public class CameraSettings : ScriptableObject
    {
        [Serializable]
        public class CameraSet
        {
            public ProjectEnum.SceneName SceneName;
            public float FieldOfView = 60f;
            public float CameraNear = 0.3f;
            public float CameraFar = 1000f;
            public Vector3 RootTransformPosition;
            public Vector3 RootTransformRotation;
            public Vector3 ArmLocalPosition;
            public Vector3 ArmLocalRotation;
            public Vector3 CameraLocalPosition;
            public Vector3 CameraLocalRotation;
        }

        public CameraSet[] CameraSets;
    }
}