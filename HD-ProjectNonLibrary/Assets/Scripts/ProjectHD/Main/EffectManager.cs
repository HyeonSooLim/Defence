using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectHD
{
    public class EffectManager : Singleton<EffectManager>
    {
        private const string SPAWN_SFX_KEY = "Assets/GameResources/Audio/UI/Summoning_Audio.wav";

        private readonly Dictionary<int, GameObject> _poolDictionary = new();
        private readonly Dictionary<int, float> _poolRemainTimeDcictionary = new();

        private Scene _poolingScene;
        private const int _maxProcessPerFrame = 50; // 한 프레임에 최대 50개 처리
        private const int _defaultRemainTime = 2;

        private void Awake()
        {
            _poolingScene = SceneManager.GetSceneByName(ProjectEnum.SceneName.MainWorkSpace.ToString());
            Event.EventManager.AddListener<Event.SpawnEffectEvent>(SpawnEffectAction);
            Event.EventManager.AddListener<Event.ManagerUnloadEvent>(ManagerUnloadAction);
            Event.EventManager.AddListener<Event.CharacterOnCellEvent>(CharacterOnCellAction);
        }

        private void OnDestroy()
        {
            _poolingScene = default;
            Event.EventManager.RemoveListener<Event.SpawnEffectEvent>(SpawnEffectAction);
            Event.EventManager.RemoveListener<Event.ManagerUnloadEvent>(ManagerUnloadAction);
            Event.EventManager.RemoveListener<Event.CharacterOnCellEvent>(CharacterOnCellAction);
        }

        private void SpawnEffectAction(Event.SpawnEffectEvent @event)
        {
            if (@event.AssetKey.IsNullOrEmpty())
                return;
            if (@event.Transform == null)
                return;

            var effect = MainManager.Instance.GameObjectPool.Get(@event.AssetKey);

            effect.transform.SetParent(null);
            SceneManager.MoveGameObjectToScene(effect, _poolingScene);
            effect.transform.SetPositionAndRotation(@event.Transform.position, @event.Transform.rotation);
            //effect.transform.localScale = @event.Transform.localScale;
            var instanceID = effect.GetInstanceID();
            if (!_poolDictionary.ContainsKey(instanceID))
            {
                _poolDictionary.Add(instanceID, effect);
                _poolRemainTimeDcictionary.Add(instanceID, @event.Duration);
            }
        }

        private void ManagerUnloadAction(Event.ManagerUnloadEvent @event)
        {
            foreach (var kvp in _poolDictionary)
            {
                MainManager.Instance.GameObjectPool.Return(kvp.Value);
            }

            _poolDictionary.Clear();
            _poolRemainTimeDcictionary.Clear();
        }

        private void CharacterOnCellAction(Event.CharacterOnCellEvent @event)
        {
            if (!@event.IsFirst)
                return;

            var characterInstanceID = @event.InstanceID;
            if (Runtime.StageInformation.SpawnedCharacters.TryGetValue(characterInstanceID, out var characterBehavior)
                && Global.DataManager.UnitPropertyDefine.TryGet(characterBehavior.CharacterTable.CharacterProperty, out var unitPropertyDefine))
            {
                if (unitPropertyDefine.SpawnEffectAssetKey.IsNullOrEmpty())
                    return;

                var effect = MainManager.Instance.GameObjectPool.Get(unitPropertyDefine.SpawnEffectAssetKey);
                effect.transform.SetParent(characterBehavior.transform);
                //SceneManager.MoveGameObjectToScene(effect, _poolingScene);
                effect.transform.SetLocalPositionAndRotation(new Vector3(0, unitPropertyDefine.OffsetY, 0), Quaternion.identity);
                //effect.transform.localScale = Vector3.one;
                var instanceID = effect.GetInstanceID();
                if (!_poolDictionary.ContainsKey(instanceID))
                {
                    _poolDictionary.Add(instanceID, effect);
                    _poolRemainTimeDcictionary.Add(instanceID, _defaultRemainTime);
                }

                SoundManager.Instance.PlaySFX(SPAWN_SFX_KEY);
            }
        }

        private void Update()
        {
            using var raii = new Utilities.StaticObjectPool.RAII<List<int>>(out var keysToRemove);
            keysToRemove.Clear();
            using var raiiHasSet = new Utilities.StaticObjectPool.RAII<List<int>>(out var outData);
            outData.Clear();
            outData.AddRange(_poolDictionary.Keys);

            int processedCount = 0;

            foreach (var kvp in outData)
            {
                if (processedCount >= _maxProcessPerFrame)
                    break;

                if (_poolRemainTimeDcictionary.TryGetValue(kvp, out float remainTime))
                {
                    remainTime -= Time.deltaTime;
                    _poolRemainTimeDcictionary[kvp] = remainTime;

                    if (remainTime <= 0f)
                    {
                        var gameObject = _poolDictionary[kvp];
                        MainManager.Instance.GameObjectPool.Return(gameObject);
                        keysToRemove.Add(kvp);
                    }
                }

                processedCount++;
            }

            foreach (int key in keysToRemove)
            {
                _poolDictionary.Remove(key);
                _poolRemainTimeDcictionary.Remove(key);
            }

            keysToRemove.Clear();
            outData.Clear();
        }
    }
}