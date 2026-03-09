using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectHD.UI
{
    public class PlayerLifePanel : MonoBehaviour
    {
        private int _playerLife = 3;
        [SerializeField] private Image _lifeImage;
        [SerializeField] private RectTransform _root;

        private void OnEnable()
        {
            Event.EventManager.AddListener<Event.StageSettingEvent>(StateSettingAction);
            Event.EventManager.AddListener<Event.MonsterGoalInEvent>(MonsterGoalInAction);
        }

        private void OnDisable()
        {
            Event.EventManager.RemoveListener<Event.StageSettingEvent>(StateSettingAction);
            Event.EventManager.RemoveListener<Event.MonsterGoalInEvent>(MonsterGoalInAction);
        }

        private void StateSettingAction(Event.StageSettingEvent @event)
        {
            _playerLife = @event.PlayerLife;
            _lifeImage.fillAmount = _playerLife / StaticValue.MaxPlayerLife;
        }

        private void MonsterGoalInAction(Event.MonsterGoalInEvent @event)
        {
            UpdatePlayerLife(-1);
        }

        private void UpdatePlayerLife(int change)
        {
            _playerLife += change;
            _playerLife = Mathf.Clamp(_playerLife, 0, StaticValue.MaxPlayerLife);

            _lifeImage.DOKill();
            RootInitialize();
            _lifeImage.DOFillAmount((float)_playerLife / StaticValue.MaxPlayerLife, 0.5f).SetEase(Ease.OutCubic);
            _root.DOShakeAnchorPos(0.5f, 10, 10, 50, false, true).OnComplete(() =>
            {
                RootInitialize();
            });
        }

        private void RootInitialize()
        {
            _root.DOKill();
            _root.anchoredPosition = Vector3.zero;
        }
    }    
}