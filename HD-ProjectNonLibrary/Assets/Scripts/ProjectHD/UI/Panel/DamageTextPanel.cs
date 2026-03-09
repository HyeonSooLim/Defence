using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectHD.UI
{
    public class DamageTextPanel : MonoBehaviour
    {
        private const string TEXT_ASSET_KEY = "Assets/GameResources/Prefabs/UI/DamageText.prefab";
        [SerializeField] private Canvas _canvas;
        [SerializeField] private RectTransform _canvasRectTransform;

        private HashSet<UI.DamageText> _damageTexts;
        private readonly CancelToken _cancelToken = new();

        private void Awake()
        {
            _cancelToken.SetToken();
            _damageTexts = Utilities.StaticObjectPool.Pop<HashSet<UI.DamageText>>();
            _damageTexts.Clear();
        }

        public void OnEnable()
        {
            Event.EventManager.AddListener<Event.CalculatedDamageEvent>(CalculatedDamageAction);
            Event.EventManager.AddListener<Event.ManagerUnloadEvent>(ManagerUnloadAction);
        }

        public void OnDisable()
        {
            Event.EventManager.RemoveListener<Event.CalculatedDamageEvent>(CalculatedDamageAction);
            Event.EventManager.RemoveListener<Event.ManagerUnloadEvent>(ManagerUnloadAction);
        }

        private void CalculatedDamageAction(Event.CalculatedDamageEvent @event)
        {
            if (@event.FinalDamage <= 0)
                return;
            if (@event.Damageable == null)
                return;

            var damagedObjectID = @event.Damageable.GetInstanceID();
            if (!Runtime.StageInformation.SpawnedEnemies.TryGetValue(damagedObjectID, out var monsterBehavior))
                return;

            var effect = MainManager.Instance.GameObjectPool.Get(TEXT_ASSET_KEY);
            if (!effect.TryGetComponent<UI.DamageText>(out var damageText))
            {
                Utilities.InternalDebug.LogError($"[{this.name}] {TEXT_ASSET_KEY} is not have DamageText component");
                MainManager.Instance.GameObjectPool.Return(effect);
                return;
            }

            var anchoredPosition = StaticMethod.WorldPositionToUIAnchoredPosition(monsterBehavior.transform.position,
                _canvasRectTransform, _canvas.worldCamera);

            effect.transform.SetParent(transform);
            effect.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            effect.transform.localScale = Vector3.one * 3;

            damageText.RectTransform.anchoredPosition = anchoredPosition;
            //damageText.RectTransform.rotation = Quaternion.identity;
            damageText.Construct(@event.FinalDamage, () =>
            {
                _damageTexts.Remove(damageText);
                damageText.Destruct();
                MainManager.Instance.GameObjectPool.Return(effect);
            }, _cancelToken.Token);
            _damageTexts.Add(damageText);
        }

        private void ManagerUnloadAction(Event.ManagerUnloadEvent @event)
        {
            _cancelToken.UnSetToken();

            foreach (var damageText in _damageTexts)
            {
                damageText.Destruct();
                MainManager.Instance.GameObjectPool.Return(damageText.gameObject);
            }
            _damageTexts.Clear();
            Utilities.StaticObjectPool.Push(_damageTexts);
            _damageTexts = null;
        }
    }
}