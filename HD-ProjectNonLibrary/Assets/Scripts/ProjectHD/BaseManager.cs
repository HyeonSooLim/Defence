using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectHD
{
    public abstract class BaseManager<T> : Singleton<T> where T : MonoBehaviour
    {
        protected virtual void Awake()
        {
            // 이니셜라이즈에 시간이 필요하다면 (await 등) 씬 로딩이 다음 단계로 넘어가지 않도록 하는 이벤트
            ExecuteInitialzeCompleteEvent(false);
        }

        public virtual async UniTask Initialize()
        {
            ExecuteInitialzeCompleteEvent(true);    // 씬 로딩이 다음 단계로 넘어가도록 하는 이벤트
            Utilities.InternalDebug.Log($"[Manager][{name}] Initialize Done");
            await UniTask.DelayFrame(2);
        }

        public virtual async UniTask DeInitialize()
        {
            ExecuteDeInitializeStartEvent();
            await UniTask.DelayFrame(2);
        }

        public void MoveToWorkspace(GameObject gameObject)
        {
            MainManager.Instance.MoveToCurrentWorkspace(gameObject);
        }

        public void MoveToOherScene(ProjectEnum.SceneName sceneName, UniTask cleanUp)
        {
            SceneLoadManager.Instance.MoveToScene(sceneName, cleanUp);
        }

        public void ExecuteInitialzeCompleteEvent(bool isDone)
        {
            var managerLoadEvent = Event.Events.ManagerInitialzeCompleteEvent;
            managerLoadEvent.IsDone = isDone;
            Event.EventManager.Broadcast(managerLoadEvent);
        }

        public void ExecuteDeInitializeStartEvent()
        {
            Event.EventManager.Broadcast(Event.Events.ManagerUnloadEvent);
        }

        public void ExecuteSetActiveSceneEvent(UnityEngine.SceneManagement.Scene scene)
        {
            var setActiveSceneEvent = Event.Events.SetActiveSceneEvent;
            setActiveSceneEvent.Scene = scene;
            Event.EventManager.Broadcast(setActiveSceneEvent);
            Utilities.InternalDebug.Log($"SetActiveScene Event");
        }
    }
}