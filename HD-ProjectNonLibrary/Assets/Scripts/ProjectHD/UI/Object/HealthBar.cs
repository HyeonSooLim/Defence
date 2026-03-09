using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace ProjectHD.UI
{
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private Image _healthBar;
        [SerializeField] private TMP_Text _health;
        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private float _tweenDuration = 0.5f;

        public RectTransform RectTransform => _rectTransform;

        private float _maxHealth;
        private float _currentHealth;

        public void Construct(float maxHealth)
        {
            _maxHealth = maxHealth;
            _currentHealth = maxHealth;
            _healthBar.fillAmount = 1;
            _health.SetText($"{_maxHealth}");
        }

        public void Sethealth(float health)
        {
            _currentHealth = health;
            var amount = _currentHealth / _maxHealth;
            _healthBar.DOKill();
            _healthBar.DOFillAmount(amount, _tweenDuration).SetEase(Ease.OutQuad);
            _health.SetText($"{Mathf.Round(_currentHealth)}");
        }

        public void Destruct()
        {
            _healthBar.DOKill();
            _healthBar.fillAmount = 0;
            _health.SetText($"0");
            _currentHealth = 0;
            _maxHealth = 0;
        }
    }
}