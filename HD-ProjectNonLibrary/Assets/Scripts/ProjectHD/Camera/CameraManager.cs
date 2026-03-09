using DG.Tweening;
using UnityEngine;

namespace ProjectHD
{
    public class CameraManager : Singleton<CameraManager>
    {
        [System.Serializable]
        public class MainCameraSet
        {
            public Transform CameraRoot;
            public Transform CameraArm;
            public Camera MainCamera;
        }

        private void Awake()
        {
            Event.EventManager.AddListener<Event.NextSceneLoadCompleteEvent>(NextSceneLoadCompleteAction);
            Event.EventManager.AddListener<Event.MainCameraSetActiveEvent>(SetActiveCamera);
            Event.EventManager.AddListener<Event.CameraShakeEvent>(CameraShakeAction);
        }

        private void OnDestroy()
        {
            Event.EventManager.RemoveListener<Event.NextSceneLoadCompleteEvent>(NextSceneLoadCompleteAction);
            Event.EventManager.RemoveListener<Event.MainCameraSetActiveEvent>(SetActiveCamera);
            Event.EventManager.RemoveListener<Event.CameraShakeEvent>(CameraShakeAction);
        }

        public Camera UICamera => uiCamera;
        public Camera MainCamera => mainCameraSet.MainCamera;
        public MainCameraSet MainCamSet => mainCameraSet;

        [SerializeField]
        private Camera uiCamera;

        [SerializeField]
        private MainCameraSet mainCameraSet;

        private CameraSettings.CameraSet currentCameraSet;

        private void NextSceneLoadCompleteAction(Event.NextSceneLoadCompleteEvent @event)
        {
            var cameraSettings = StaticValue.CameraSettings;
            for (int i = 0; i < cameraSettings.CameraSets.Length; i++)
            {
                var cameraSet = cameraSettings.CameraSets[i];
                if (cameraSet.SceneName == @event.CurrentSceneName)
                {
                    mainCameraSet.CameraRoot.SetPositionAndRotation(cameraSet.RootTransformPosition, Quaternion.Euler(cameraSet.RootTransformRotation));
                    mainCameraSet.CameraArm.SetLocalPositionAndRotation(cameraSet.ArmLocalPosition, Quaternion.Euler(cameraSet.ArmLocalRotation));
                    mainCameraSet.MainCamera.transform.SetLocalPositionAndRotation(cameraSet.CameraLocalPosition, Quaternion.Euler(cameraSet.CameraLocalRotation));
                    mainCameraSet.MainCamera.fieldOfView = cameraSet.FieldOfView;
                    mainCameraSet.MainCamera.nearClipPlane = cameraSet.CameraNear;
                    mainCameraSet.MainCamera.farClipPlane = cameraSet.CameraFar;
                    currentCameraSet = cameraSet;
                    return;
                }
            }

            currentCameraSet = default;
        }

        private void SetActiveCamera(Event.MainCameraSetActiveEvent @event)
        {
            mainCameraSet.CameraRoot.gameObject.SetActive(@event.IsOnCameraRoot);
            mainCameraSet.CameraArm.gameObject.SetActive(@event.IsOnArm);
            mainCameraSet.MainCamera.gameObject.SetActive(@event.IsOnMainCamera);
        }

        private void CameraShakeAction(Event.CameraShakeEvent @event)
        {
            var duration = @event.Duration;
            var magnitude = @event.Magnitude;
            var cameraArm = mainCameraSet.CameraArm;
            cameraArm.DOKill();
            //var originalPosition = cameraArm.localPosition;
            cameraArm.DOShakePosition(duration, magnitude).OnComplete(() =>
            {
                cameraArm.SetLocalPositionAndRotation(currentCameraSet.ArmLocalPosition, Quaternion.Euler(currentCameraSet.ArmLocalRotation));
            }).SetUpdate(true);
        }
    }
}