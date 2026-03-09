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

        private List<GameObject> poolingObjects;
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
            poolingObjects = Utilities.StaticObjectPool.Pop<List<GameObject>>();
            poolingObjects.Clear();

            float totalTasks = 8;
            float startProgress = 0.5f;
            float completedTaskAddProgress = (1 - startProgress) / totalTasks;
            async UniTask RunTaskWithProgress(UniTask task)
            {
                var taskGoalProgress = startProgress + completedTaskAddProgress;
                await UpdateLoadingProgress(task, startProgress, taskGoalProgress);
                startProgress = taskGoalProgress;
            }

            StageSeed = Runtime.StageInformation.StageSeed; // 임시값

            await RunTaskWithProgress(PreloadAllCharacters());
            await RunTaskWithProgress(SetBackgroundScene());    
            await RunTaskWithProgress(SetUI());
            await RunTaskWithProgress(SetWaveController());
            await RunTaskWithProgress(SetMapObject());
            await RunTaskWithProgress(SetCharacterCombineController());
            await RunTaskWithProgress(SetDamageController());
            await RunTaskWithProgress(SetBuffSetController());
            SetPlayers();

            await base.Initialize();
            ExecuteStageSettingEvent(StageSeed);
        }

        public override async UniTask DeInitialize()
        {
            await base.DeInitialize();

            foreach (var gameObject in poolingObjects)
            {
                MainManager.Instance.GameObjectPool.Return(gameObject);
            }
            poolingObjects.Clear();
            Utilities.StaticObjectPool.Push(poolingObjects);
            poolingObjects = null;

            MainManager.Instance.SceneInstancePool.Return(_backgroundSceneInsctance);
            _backgroundSceneInsctance = default;

            Utilities.InternalDebug.Log($"[CleanUp][{name}] DeInitialize Done");
        }

        private async UniTask SetBackgroundScene()
        {
            if (Global.DataManager.StageTable.TryGet(StageSeed, out var stageTable))
            {
                _backgroundSceneInsctance = await MainManager.Instance.SceneInstancePool.GetAsync(stageTable.SceneAssetKey);
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
            var baseUI = await MainManager.Instance.GameObjectPool.GetAsync(BASE_UI);
            MoveToWorkspace(baseUI);
            poolingObjects.Add(baseUI);

            var effectUI = await MainManager.Instance.GameObjectPool.GetAsync(EFFECT_UI);
            MoveToWorkspace(effectUI);
            poolingObjects.Add(effectUI);

            var monsterHealthUI = await MainManager.Instance.GameObjectPool.GetAsync(MONSTER_HEALTH_UI);
            MoveToWorkspace(monsterHealthUI);
            poolingObjects.Add(monsterHealthUI);

            Utilities.InternalDebug.Log($"[{name}] SetBaseUI Done");
        }

        private async UniTask SetWaveController()
        {
            var waveController = await MainManager.Instance.GameObjectPool.GetAsync(WAVE_CONTROLLER);
            MoveToWorkspace(waveController);
            poolingObjects.Add(waveController);
            Utilities.InternalDebug.Log($"[{name}] SetWaveController Done");
        }

        private async UniTask SetMapObject()
        {
            var map01 = await MainManager.Instance.GameObjectPool.GetAsync(MAP_ASSET_KEY_01);
            var map02 = await MainManager.Instance.GameObjectPool.GetAsync(MAP_ASSET_KEY_02);
            MoveToWorkspace(map01);
            MoveToWorkspace(map02);
            poolingObjects.Add(map01);
            poolingObjects.Add(map02);
            Utilities.InternalDebug.Log($"[{name}] SetMapObject Done");
        }

        private async UniTask SetCharacterCombineController()
        {
            var combineController = await MainManager.Instance.GameObjectPool.GetAsync(CHARACTER_COMBINE_CONTROLLER);
            MoveToWorkspace(combineController);
            poolingObjects.Add(combineController);
            Utilities.InternalDebug.Log($"[{name}] SetCharacterCombineController Done");
        }

        private async UniTask SetDamageController()
        {
            var damageController = await MainManager.Instance.GameObjectPool.GetAsync(DAMAGE_CONTROLLER);
            MoveToWorkspace(damageController);
            poolingObjects.Add(damageController);
            Utilities.InternalDebug.Log($"[{name}] SetDamageController Done");
        }

        private async UniTask SetBuffSetController()
        {
            var buffSetController = await MainManager.Instance.GameObjectPool.GetAsync(BUFF_SET_CONTROLLER);
            MoveToWorkspace(buffSetController);
            poolingObjects.Add(buffSetController);
            Utilities.InternalDebug.Log($"[{name}] SetBuffSetController Done");
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
                poolingObjects.Add(character);
            }
            characterTableEnum.Dispose();

            foreach (var gameObject in poolingObjects)
            {
                MainManager.Instance.GameObjectPool.Return(gameObject);
            }
            poolingObjects.Clear();
            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        private async UniTask UpdateLoadingProgress(UniTask task, float startProgress, float endProgress)
        {
            await task;
            ExecuteLoadingEvent((float)Mathf.Lerp(startProgress, endProgress, 1));
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
    }
}