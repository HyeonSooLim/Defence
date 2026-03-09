using Cysharp.Threading.Tasks;
using ProjectHD.Event;
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectHD
{
    public interface ISceneLoader
    {
        UniTask MoveToScene(ProjectEnum.SceneName sceneName, UniTask cleanUp);
        void SetEvent();
        void RemoveEvent();
    }

    public class SceneLoader : ISceneLoader
    {
        private ProjectEnum.SceneName _currentScene = ProjectEnum.SceneName.None;
        private ProjectEnum.SceneName _nextScene;

        bool _isManagerInitializeDone = true;

        public void SetEvent()
        {
            Event.EventManager.AddListener<Event.ManagerInitialzeCompleteEvent>(ManagerInitialzeCompleteAction);
            Event.EventManager.AddListener<Event.SetActiveSceneEvent>(SetActiveSceneAction);
        }

        public void RemoveEvent()
        {
            Event.EventManager.RemoveListener<Event.ManagerInitialzeCompleteEvent>(ManagerInitialzeCompleteAction);
            Event.EventManager.RemoveListener<Event.SetActiveSceneEvent>(SetActiveSceneAction);
        }

        public async UniTask MoveToScene(ProjectEnum.SceneName sceneName, UniTask cleanUp)
        {
            await Utilities.Fade.FadeOutAsync(0.3f, Color.black);

            await cleanUp;
            await UniTask.Yield();
            await MainManager.Instance.CleanUp();
            await UniTask.Yield();

            await LoadLoadingScene();
            ExcuteMainCameraSetActiveEvent();
            await Utilities.Fade.FadeInAsync(0.3f, Color.black);

            // 현재 씬이 None이 아니면 현재 씬 언로드 후 다음 씬 로드
            if (_currentScene != ProjectEnum.SceneName.None)
            {
                await UnloadCurrentScene();
                await UniTask.Yield();
            }

            // 유효한 씬 이름인지 확인
            if (!System.Enum.IsDefined(typeof(ProjectEnum.SceneName), sceneName))
            {
                Debug.LogError($"Invalid scene name: {sceneName}");
                return;
            }
            _nextScene = sceneName;
            await LoadNextScene(_nextScene);
            await UniTask.Yield();
            await UniTask.WaitUntil(() => _isManagerInitializeDone);
            Utilities.InternalDebug.Log($"NextSceneInitialized");
            ExecuteLoadingEvent(1.0f);

            await Utilities.Fade.FadeOutAsync(0.3f, Color.black);
            await UnloadLoadingScene(); // 로딩 씬 언로드
            ExecuteSceneLoadingCompleteEvent();
            Utilities.Fade.FadeInAsync(0.3f, Color.black).Forget();
        }

        private async UniTask LoadNextScene(ProjectEnum.SceneName sceneName)
        {
            var sceneNameString = sceneName.ToString();
            var nextScene = SceneManager.LoadSceneAsync(sceneNameString, LoadSceneMode.Additive); // 다음 씬 로드
            if (nextScene == null)
            {
                Debug.LogError($"Failed to load scene: {sceneName}");
                return;
            }

            UpdateLoadingProgress(nextScene, 0.25f, 0.5f).Forget(); // 로딩 진행 업데이트

            nextScene.completed += _ =>
            {
                // 이 부분이 없으면 MainScene에 오브젝트들이 생성된다(인게임 씬 오브젝트들)
                //SetActiveScene(sceneNameString); // 액티브 씬 설정
                _currentScene = sceneName; // 현재 씬 업데이트
                ExecuteNextSceneLoadCompleteEvent(_currentScene); // 씬 로드 완료 이벤트 실행
            };

            await UniTask.WaitUntil(()=> nextScene.isDone); // 씬 로드 완료 대기
        }

        private async UniTask UnloadCurrentScene()
        {
            var mainScene = SceneManager.GetSceneByName(ProjectEnum.SceneName.MainWorkSpace.ToString());
            SceneManager.SetActiveScene(mainScene); // 메인 씬을 액티브 씬으로 설정

            var unloadScene = SceneManager.UnloadSceneAsync(_currentScene.ToString()); // 현재 씬 언로드
            if (unloadScene == null)
            {
                Debug.LogError($"Failed to unload scene: {_currentScene}");
                return;
            }
            UpdateLoadingProgress(unloadScene, 0.0f, 0.25f).Forget(); // 언로드 진행 업데이트

            await Resources.UnloadUnusedAssets(); // 사용하지 않는 리소스 언로드
            await UniTask.Yield();
            CheckAndCollectGC();

            await UniTask.WaitUntil(() => unloadScene.isDone); // 언로드 완료 대기
        }

        #region Loading Scene

        private async UniTask LoadLoadingScene()
        {
            var loading = SceneManager.LoadSceneAsync(ProjectEnum.SceneName.LoadingWorkSpace.ToString(), LoadSceneMode.Additive); // 로딩 씬 로드
            if (loading == null)
            {
                Debug.LogError("Failed to load Loading scene");
                return;
            }

            await UniTask.WaitUntil(() => loading.isDone); // 로딩 씬 로드 완료 대기
            await UniTask.Yield(); // 로딩 씬 로드 완료 대기
        }

        private async UniTask UnloadLoadingScene()
        {
            var loadingScene = SceneManager.GetSceneByName(ProjectEnum.SceneName.LoadingWorkSpace.ToString());
            var unloadScene = SceneManager.UnloadSceneAsync(loadingScene); // 로딩 씬 언로드
            if (unloadScene == null)
            {
                Debug.LogError("Failed to unload Loading scene");
                return;
            }

            await UniTask.WaitUntil(() => unloadScene.isDone); // 로딩 씬 언로드 완료 대기
            await UniTask.Yield(); // 로딩 씬 언로드 완료 대기
        }

        private async UniTask UpdateLoadingProgress(AsyncOperation operation, float startProgress, float endProgress)
        {
            while (!operation.isDone)
            {
                // operation.progress는 0.9까지만 진행되므로 0.9로 나눠서 진행도 계산
                float progress = Mathf.Lerp(startProgress, endProgress, Mathf.Clamp01(operation.progress / 0.9f));
                Utilities.InternalDebug.Log($"로딩: start:{startProgress}, end{endProgress}, operation{operation.progress}, progress{progress}");
                ExecuteLoadingEvent(progress);
                await UniTask.Yield();
            }
        }

        #endregion

        private void SetActiveScene(string sceneName)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            if (scene.IsValid())
            {
                SceneManager.SetActiveScene(scene); // 액티브 씬 설정
            }
            else
            {
                Debug.LogError($"Failed to set active scene: {sceneName}");
            }
        }

        private void SetActiveScene(Scene scene)
        {
            if (scene.IsValid())
            {
                SceneManager.SetActiveScene(scene); // 액티브 씬 설정
            }
            else
            {
                Debug.LogError($"Failed to set active scene: {scene.name}");
            }
        }

        private void CheckAndCollectGC()
        {
            //GC.Collect 전에 메모리 확인
            long before = GC.GetTotalMemory(false);
            Utilities.InternalDebug.Log($"메모리 확인 Before: {before / (1024 * 1024)}mb");
            if (before > StaticValue.MemoryLimit)
            {
                GC.Collect();
                //GC.Collect 후 메모리 확인
                long after = GC.GetTotalMemory(false);
                Utilities.InternalDebug.Log($"메모리 확인 After: {after / (1024 * 1024)}mb");
                Utilities.InternalDebug.Log($"메모리 확인 발생한 가비지: {(before / (1024 * 1024)) - (after / (1024 * 1024))}mb");
            }
        }

        #region Events

        private void ExecuteNextSceneLoadCompleteEvent(ProjectEnum.SceneName sceneName)
        {
            Events.NextSceneLoadCompleteEvent.CurrentSceneName = sceneName; // 이벤트에 현재 씬 이름 설정
            EventManager.Broadcast(Events.NextSceneLoadCompleteEvent); // 씬 로드 완료 이벤트 실행
            Utilities.InternalDebug.Log($"NextSceneLoadComplete Event");
        }

        private void ExecuteSceneLoadingCompleteEvent()
        {
            EventManager.Broadcast(Events.SceneLoadingCompleteEvent); // 씬 로드 완료 이벤트 실행
            Utilities.InternalDebug.Log($"SceneLoadingComplete Event");
        }

        private void ExcuteMainCameraSetActiveEvent(bool isOnCameraRoot = true, bool isOnArm = true, bool isOnMainCamera = true)
        {
            var cameraEvent = Events.MainCameraSetActiveEvent;
            cameraEvent.IsOnCameraRoot = isOnCameraRoot;
            cameraEvent.IsOnArm = isOnArm;
            cameraEvent.IsOnMainCamera = isOnMainCamera;
            EventManager.Broadcast(cameraEvent); // 메인 카메라 활성화 이벤트 실행
            Utilities.InternalDebug.Log($"MainCameraSetActive Event");
        }

        private void ExecuteLoadingEvent(float progress)
        {
            Events.SceneLoadingEvent.Progress = progress; // 이벤트에 진행도 설정
            EventManager.Broadcast(Events.SceneLoadingEvent); // 이벤트 실행
            Utilities.InternalDebug.Log($"SceneLoading Event");
        }

        private void ManagerInitialzeCompleteAction(Event.ManagerInitialzeCompleteEvent @event)
        {
            _isManagerInitializeDone = @event.IsDone;
        }

        private void SetActiveSceneAction(Event.SetActiveSceneEvent @event)
        {
            SetActiveScene(@event.Scene);
        }

        #endregion
    }
}