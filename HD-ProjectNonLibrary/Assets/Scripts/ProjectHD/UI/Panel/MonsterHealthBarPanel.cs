using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectHD.UI
{
    public class MonsterHealthBarPanel : MonoBehaviour
    {
        private const string HEALTH_BAR_ASSET_KEY = "Assets/GameResources/Prefabs/UI/HealthBar.prefab";
        [SerializeField] private Canvas _canvas;
        [SerializeField] private RectTransform _canvasRectTransform;
        [SerializeField] private float _size = 3f;
        [SerializeField] private Vector3 _offset;
        private Dictionary<int, HealthBar> _activeHealthBars;

        private void Awake()
        {
            _activeHealthBars = Utilities.StaticObjectPool.Pop<Dictionary<int, HealthBar>>();
            _activeHealthBars.Clear();
        }

        private void OnEnable()
        {
            Event.EventManager.AddListener<Event.ManagerUnloadEvent>(ManagerUnloadAction);
            Event.EventManager.AddListener<Event.MonsterDieEvent>(MonsterDieAction);
            Event.EventManager.AddListener<Event.MonsterMoveEvent>(MonsterMoveAction);
            Event.EventManager.AddListener<Event.MonsterHealthUpdateEvent>(MonsterHealthUpdateAction);
            Event.EventManager.AddListener<Event.MonsterGoalInEvent>(MonsterGoalInAction);
        }

        private void OnDisable()
        {
            Event.EventManager.RemoveListener<Event.ManagerUnloadEvent>(ManagerUnloadAction);
            Event.EventManager.RemoveListener<Event.MonsterDieEvent>(MonsterDieAction);
            Event.EventManager.RemoveListener<Event.MonsterMoveEvent>(MonsterMoveAction);
            Event.EventManager.RemoveListener<Event.MonsterHealthUpdateEvent>(MonsterHealthUpdateAction);
            Event.EventManager.RemoveListener<Event.MonsterGoalInEvent>(MonsterGoalInAction);
        }

        private void MonsterHealthUpdateAction(Event.MonsterHealthUpdateEvent @event)
        {
            var instanceID = @event.InstanceID;
            SetHealthBar(instanceID, @event.CurrentHealth);
        }

        private void ManagerUnloadAction(Event.ManagerUnloadEvent @event)
        {
            foreach (var healthBar in _activeHealthBars.Values)
            {
                healthBar.Destruct();
                MainManager.Instance.GameObjectPool.Return(healthBar.gameObject);
            }
            _activeHealthBars.Clear();
            Utilities.StaticObjectPool.Push(_activeHealthBars);
            _activeHealthBars = null;
        }

        private void MonsterDieAction(Event.MonsterDieEvent @event)
        {
            var instanceID = @event.InstanceID;
            if (_activeHealthBars.TryGetValue(instanceID, out var healthBar))
            {
                healthBar.Destruct();
                MainManager.Instance.GameObjectPool.Return(healthBar.gameObject);
                _activeHealthBars.Remove(instanceID);
            }
        }

        private void MonsterMoveAction(Event.MonsterMoveEvent @event)
        {
            var instanceID = @event.InstanceID;
            if (!Runtime.StageInformation.SpawnedEnemies.TryGetValue(instanceID, out var monsterBehavior))
                return;
            if (!_activeHealthBars.TryGetValue(instanceID, out var healthBar))
                return;
            var worldPosition = monsterBehavior.transform.position + _offset; // 캐릭터 머리 위로 오프셋
            var anchoredPosition = StaticMethod.WorldPositionToUIAnchoredPosition(worldPosition, _canvasRectTransform, _canvas.worldCamera);
            healthBar.RectTransform.anchoredPosition = anchoredPosition;
        }

        private void MonsterGoalInAction(Event.MonsterGoalInEvent @event)
        {
            var instanceID = @event.InstanceID;
            if (_activeHealthBars.TryGetValue(instanceID, out var healthBar))
            {
                healthBar.Destruct();
                MainManager.Instance.GameObjectPool.Return(healthBar.gameObject);
                _activeHealthBars.Remove(instanceID);
            }
        }

        private void SetHealthBar(int instanceID, float health)
        {
            if (!Runtime.StageInformation.SpawnedEnemies.TryGetValue(instanceID, out var monsterBehavior))
                return;
            if (_activeHealthBars.TryGetValue(instanceID, out var bar))
            {
                bar.Sethealth(health);
            }
            else
            {
                var healthBarObject = MainManager.Instance.GameObjectPool.Get(HEALTH_BAR_ASSET_KEY);
                if (!healthBarObject.TryGetComponent<HealthBar>(out var healthBar))
                {
                    Utilities.InternalDebug.LogError($"[{nameof(MonsterHealthBarPanel)}] {HEALTH_BAR_ASSET_KEY} is not have HealthBar component");
                    MainManager.Instance.GameObjectPool.Return(healthBarObject);
                    return;
                }

                healthBar.transform.SetParent(transform);
                healthBar.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                healthBar.transform.localScale = Vector3.one * _size;
                healthBar.Construct(health);

                _activeHealthBars.Add(instanceID, healthBar);
            }
        }
    }
}
