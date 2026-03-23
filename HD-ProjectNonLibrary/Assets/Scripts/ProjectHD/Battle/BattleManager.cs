using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectHD.Battle
{
    public class BattleManager : BaseManager<BattleManager>
    {
        private const string MAP_ASSET_KEY_01 = "Assets/GameResources/Prefabs/World/Stage_01.prefab";
        private const string MAP_ASSET_KEY_02 = "Assets/GameResources/Prefabs/World/Stage_01_BG.prefab";
        private const string WAVE_CONTROLLER = "Assets/GameResources/Prefabs/Battle/WaveController.prefab";
        private const string BASE_UI = "Assets/GameResources/Prefabs/UI/BattleBaseUI.prefab";
        private const string EFFECT_UI = "Assets/GameResources/Prefabs/UI/BattleEffectUI.prefab";
        private const string MONSTER_HEALTH_UI = "Assets/GameResources/Prefabs/UI/MonsterHealthUI.prefab";
        private const string CHARACTER_COMBINE_CONTROLLER = "Assets/GameResources/Prefabs/Battle/CharacterCombineController.prefab";
        private const string DAMAGE_CONTROLLER = "Assets/GameResources/Prefabs/Battle/DamageController.prefab";
        private const string BUFF_SET_CONTROLLER = "Assets/GameResources/Prefabs/Battle/BuffSetController.prefab";

        private List<GameObject> _poolingObjects;
        private Queue<System.Func<UniTask>> _initializationTasks;

        private UnityEngine.ResourceManagement.ResourceProviders.SceneInstance _backgroundSceneInsctance;
        private UnityEngine.SceneManagement.Scene _backgroundScene => _backgroundSceneInsctance.Scene;

        public int StageSeed { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Initialize().Forget();
        }

#if UNITY_EDITOR

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                MoveToOherScene(ProjectEnum.SceneName.TitleWorkSpace, UniTask.Defer(DeInitialize));
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                Initialize();
            }

            if (Input.GetKeyDown(KeyCode.V))
            {
                DeInitialize();
            }
        }
#endif

        public override async UniTask Initialize()
        {
            StageSeed = Runtime.StageInformation.StageSeed; // 임시값

            _poolingObjects = Utilities.StaticObjectPool.Pop<List<GameObject>>();
            _poolingObjects.Clear();
            _initializationTasks = Utilities.StaticObjectPool.Pop<Queue<System.Func<UniTask>>>();
            _initializationTasks.Clear();

            InitializeTask();
            await RunTaskWithProgress(_initializationTasks, 0.5f);

            SetPlayers();

            await base.Initialize();
            ExecuteStageSettingEvent(StageSeed);
        }

        public override async UniTask DeInitialize()
        {
            await base.DeInitialize();

            _initializationTasks.Clear();
            Utilities.StaticObjectPool.Push(_initializationTasks);
            _initializationTasks = null;

            foreach (var gameObject in _poolingObjects)
            {
                MainManager.Instance.GameObjectPool.Return(gameObject);
            }
            _poolingObjects.Clear();
            Utilities.StaticObjectPool.Push(_poolingObjects);
            _poolingObjects = null;

            MainManager.Instance.SceneInstancePool.Return(_backgroundSceneInsctance);
            _backgroundSceneInsctance = default;

            Utilities.InternalDebug.Log($"[CleanUp][{name}] DeInitialize Done");
        }

        private async UniTask SetBackgroundScene()
        {
            if (Global.DataManager.StageTable.TryGet(StageSeed, out var stageTable))
            {
                var assetKey = stageTable.SceneAssetKey;
#if UNITY_EDITOR
                assetKey = GetOptimizedAssetKey(assetKey, DeviceRepositoryKey.Editor_Project_Optimization_Scene);
#endif
                _backgroundSceneInsctance = await MainManager.Instance.SceneInstancePool.GetAsync(assetKey);
                var rootObjects = Utilities.StaticObjectPool.Pop<List<GameObject>>();
                rootObjects.Clear();
                _backgroundScene.GetRootGameObjects(rootObjects);
                foreach (var rootGameObject in rootObjects)
                    rootGameObject.SetActive(true);
                rootObjects.Clear();
                Utilities.StaticObjectPool.Push(rootObjects);
                ExecuteSetActiveSceneEvent(_backgroundScene);
            }

            Utilities.InternalDebug.Log($"[{name}] SetBackgroundScene Done");
        }

        private async UniTask SetUI()
        {
            var assetKey = BASE_UI;
#if UNITY_EDITOR
            assetKey = GetOptimizedAssetKey(assetKey, DeviceRepositoryKey.Editor_Project_Optimization_UI);
#endif

            var baseUI = await MainManager.Instance.GameObjectPool.GetAsync(assetKey);
            MoveToWorkspace(baseUI);
            _poolingObjects.Add(baseUI);

            var effectUI = await MainManager.Instance.GameObjectPool.GetAsync(EFFECT_UI);
            MoveToWorkspace(effectUI);
            _poolingObjects.Add(effectUI);

            var monsterHealthUI = await MainManager.Instance.GameObjectPool.GetAsync(MONSTER_HEALTH_UI);
            MoveToWorkspace(monsterHealthUI);
            _poolingObjects.Add(monsterHealthUI);

            Utilities.InternalDebug.Log($"[{name}] SetBaseUI Done");
        }

        private async UniTask SetController(string assetKey)
        {
            var controller = await MainManager.Instance.GameObjectPool.GetAsync(assetKey);
            MoveToWorkspace(controller);
            _poolingObjects.Add(controller);
            Utilities.InternalDebug.Log($"[{name}] {controller.name} Done");
        }

        private async UniTask SetMapObject()
        {
            var assetKey01 = MAP_ASSET_KEY_01;
            var assetKey02 = MAP_ASSET_KEY_02;
#if UNITY_EDITOR
            assetKey01 = GetOptimizedAssetKey(assetKey01, DeviceRepositoryKey.Editor_Project_Optimization_MapObject);
            assetKey02 = GetOptimizedAssetKey(assetKey02, DeviceRepositoryKey.Editor_Project_Optimization_MapObject);
#endif

            var map01 = await MainManager.Instance.GameObjectPool.GetAsync(assetKey01);
            var map02 = await MainManager.Instance.GameObjectPool.GetAsync(assetKey02);
            MoveToWorkspace(map01);
            MoveToWorkspace(map02);
            _poolingObjects.Add(map01);
            _poolingObjects.Add(map02);
            Utilities.InternalDebug.Log($"[{name}] SetMapObject Done");
        }

        private void SetPlayers()
        {
            Utilities.InternalDebug.Log($"[{name}] SetPlayers Done");
        }

        private async UniTask PreloadAllCharacters()
        {
            var characterTableEnum = Global.DataManager.CharacterTable.GetEnumerator();
            while (characterTableEnum.MoveNext())
            {
                var characterTable = characterTableEnum.Current.Value;
                if (characterTable.ModelAssetKey.IsNullOrEmpty())
                    continue;
                var character = await MainManager.Instance.GameObjectPool.GetAsync(characterTable.ModelAssetKey);
                _poolingObjects.Add(character);
            }
            characterTableEnum.Dispose();

            foreach (var gameObject in _poolingObjects)
            {
                MainManager.Instance.GameObjectPool.Return(gameObject);
            }
            _poolingObjects.Clear();
            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        private void InitializeTask()
        {
            _initializationTasks.Enqueue(() => PreloadAllCharacters());
            _initializationTasks.Enqueue(() => SetBackgroundScene());
            _initializationTasks.Enqueue(() => SetUI());
            _initializationTasks.Enqueue(() => SetController(WAVE_CONTROLLER));
            _initializationTasks.Enqueue(() => SetMapObject());
            _initializationTasks.Enqueue(() => SetController(CHARACTER_COMBINE_CONTROLLER));
            _initializationTasks.Enqueue(() => SetController(DAMAGE_CONTROLLER));
            _initializationTasks.Enqueue(() => SetController(BUFF_SET_CONTROLLER));
        }

        private async UniTask RunTaskWithProgress(Queue<System.Func<UniTask>> tasks, float startProgress)
        {
            float totalTasks = tasks.Count;
            float completedTaskAddProgress = (1 - startProgress) / totalTasks;

            while (tasks.TryDequeue(out var result))
            {
                await result();
                var taskGoalProgress = startProgress + completedTaskAddProgress;
                ExecuteLoadingEvent((float)Mathf.Lerp(startProgress, taskGoalProgress, 1));
                startProgress = taskGoalProgress;
            }
        }

        #region Events

        private void ExecuteLoadingEvent(float progress)
        {
            Event.Events.SceneLoadingEvent.Progress = progress; // 이벤트에 진행도 설정
            Event.EventManager.Broadcast(Event.Events.SceneLoadingEvent); // 이벤트 실행
            Utilities.InternalDebug.Log($"SceneLoading Event");
        }

        private void ExecuteStageSettingEvent(int stageSeed)    // 씬의 세팅이 끝난 후 호출함
        {
            var stageSettingEvent = Event.Events.StageSettingEvent;
            stageSettingEvent.StageSeed = stageSeed;
            stageSettingEvent.PlayerLife = Runtime.StageInformation.PlayerLife;
            Event.EventManager.Broadcast(stageSettingEvent);
            Utilities.InternalDebug.Log($"StageSetting Event");
        }

        #endregion

#if UNITY_EDITOR
        private string GetOptimizedAssetKey(string assetKey, DeviceRepositoryKey editorKey)
        {
            if (DeviceRepository.LoadKeyForBoolean(editorKey, true))
                return assetKey;
            else
            {
                var newAssetKey = assetKey.Insert(assetKey.LastIndexOf('.'), "_NonOptimization");
                return newAssetKey;
            }
        }
#endif
    }
}