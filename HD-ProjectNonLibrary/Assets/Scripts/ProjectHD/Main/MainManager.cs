using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utilities;

namespace ProjectHD
{
    public class MainManager : Singleton<MainManager>
    {
        public Utilities.ResourcePool ResourcePool { get; private set; }
        public Utilities.InstancePool InstancePool { get; private set; }
        public Utilities.GameObjectPool GameObjectPool { get; private set; }
        public Utilities.SceneInstancePool SceneInstancePool { get; private set; }

        public Scene Workspace => _workspaceSceneInstance;
        private Scene _workspaceSceneInstance;

        public async UniTask AsyncSetting()
        {
            ResourcePool ??= new();
            InstancePool ??= new();
            GameObjectPool ??= new("TempGameObjectPool");
            SceneInstancePool ??= new();

            //AudioSettings.OnAudioConfigurationChanged += ResumeBGM;
        }

        //TriggerFirst
        public async UniTask AsyncStart()
        {
            Event.EventManager.AddListener<Event.NextSceneLoadCompleteEvent>(NextSceneLoadCompleteAction);

            InternalDebug.LogCore($"AsyncStart.Start");
            InternalDebug.LogCore($"StaticValue.LoadAsync");
            await StaticValue.LoadAsync();
            InternalDebug.LogCore($"StaticValue.AsyncTest");
            await AsyncSetting();
            InternalDebug.LogCore($"AsyncStart.End");
        }

        public async UniTask AfterResourceDownload()
        {
            AtlasLoader.Startup();

            await AtlasLoader.Instance.AfterResourceDownloaded();
        }

        public void AfterNetworkConnect()
        {

        }

        public async UniTask CleanUp()
        {
            ResourcePool.UnloadAll();
            InstancePool.ReleaseAll();
            GameObjectPool.ReleaseAll();
            await SceneInstancePool.ClearAsync();
        }

        public void SetWorkspace(Scene workspace)
        {
            _workspaceSceneInstance = workspace;
        }

        public void MoveToCurrentWorkspace(GameObject gameObject)
        {
            gameObject.transform.SetParent(null);
            SceneManager.MoveGameObjectToScene(gameObject, _workspaceSceneInstance);
        }

        public void NextSceneLoadCompleteAction(Event.NextSceneLoadCompleteEvent events)
        {
            SetWorkspace(SceneManager.GetSceneByName(events.CurrentSceneName.ToString()));
        }

        private void OnDestroy()
        {
            ResourcePool = null;
            InstancePool = null;
            GameObjectPool = null;
            SceneInstancePool = null;
            _workspaceSceneInstance = default;
            Event.EventManager.RemoveListener<Event.NextSceneLoadCompleteEvent>(NextSceneLoadCompleteAction);
            InternalDebug.LogCore($"MainManager Destroyed");
        }

#if UNITY_EDITOR

        public void MoveToOherScene(ProjectEnum.SceneName sceneName, UniTask cleanUp)
        {
            SceneLoadManager.Instance.MoveToScene(sceneName, cleanUp);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.B))
            {
                MoveToOherScene(ProjectEnum.SceneName.BattleWorkSpace, UniTask.Defer(CleanUp));
            }
        }
#endif
    }
}