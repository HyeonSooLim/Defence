using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectHD
{
    public class EffectManager : Singleton<EffectManager>
    {
        private readonly Dictionary<int, GameObject> _poolDictionary = new();   // 객체 풀에서 관리하는 게임 오브젝트를 인스턴스 ID로 관리
        private readonly Dictionary<int, float> _poolRemainTimeDcictionary = new(); // 게임 오브젝트 남은 시간. 인스턴스 ID로 관리

        private const int _maxProcessPerFrame = 50; // 한 프레임에 최대 50개 처리

        [SerializeField] private Transform _effectParent;

        private void Awake()
        {            
            Event.EventManager.AddListener<Event.ManagerUnloadEvent>(ManagerUnloadAction);  // 씬 전환 시 매니저에서 전파되는 이벤트를 수신했을 때
            Event.EventManager.AddListener<Event.SpawnEffectEvent>(SpawnEffectAction);  // 이펙트 호출 이벤트를 수신했을 때
        }

        private void OnDestroy()
        {
            Event.EventManager.RemoveListener<Event.ManagerUnloadEvent>(ManagerUnloadAction);
            Event.EventManager.RemoveListener<Event.SpawnEffectEvent>(SpawnEffectAction);
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

        private void SpawnEffectAction(Event.SpawnEffectEvent @event)
        {
            if (@event.AssetKey.IsNullOrEmpty())
                return;
            if (@event.Transform == null)
                return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            MainManager.Sampler.Begin();
#endif

            var effect = MainManager.Instance.GameObjectPool.Get(@event.AssetKey, parent: _effectParent);
            effect.transform.SetPositionAndRotation(@event.Transform.position, @event.Transform.rotation);
            var instanceID = effect.GetInstanceID();
            if (!_poolDictionary.ContainsKey(instanceID))
            {
                _poolDictionary.Add(instanceID, effect);
                _poolRemainTimeDcictionary.Add(instanceID, @event.Duration);
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            MainManager.Sampler.End();
#endif
        }

        private void Update()   // 시간이 다 된 오브젝트 반환. 매 프레임 50개씩 처리(성능 저하 방지)
        {
            using var raii = new Utilities.StaticObjectPool.RAII<List<int>>(out var keysToRemove);
            keysToRemove.Clear();
            using var raiiHasSet = new Utilities.StaticObjectPool.RAII<List<int>>(out var poolKeys);
            poolKeys.Clear();
            poolKeys.AddRange(_poolDictionary.Keys);    // 순회 중 컬렉션이 변경될 수 있으므로 키 목록을 별도로 관리

            int processedCount = 0;

            foreach (var kvp in poolKeys)
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
            poolKeys.Clear();
        }
    }
}