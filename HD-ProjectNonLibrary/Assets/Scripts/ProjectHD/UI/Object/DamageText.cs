using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace ProjectHD.UI
{
    public class DamageText : MonoBehaviour
    {
        [SerializeField] private TMPro.TextMeshProUGUI _damageText;
        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private float _moveDistance = 300f;
        [SerializeField] private float _moveDuration = 2f;
        [SerializeField] private float _fadeEndValue = 0f;
        [SerializeField] private Ease _animationType = Ease.OutCubic;

        public RectTransform RectTransform => _rectTransform;

        public void Construct(int damageAmount, System.Action completeAction, CancellationToken cancellationToken = default)
        {
            _damageText.SetText($"{damageAmount}");
            _damageText.alpha = 1;
            _rectTransform.DOKill();
            _rectTransform.DOAnchorPosY(_moveDistance, _moveDuration).SetRelative().SetEase(_animationType);
            _damageText.DOFade(_fadeEndValue, _moveDuration).SetEase(Ease.OutCubic).OnComplete(() =>
            {
                completeAction?.Invoke();
            }).ToUniTask(cancellationToken: cancellationToken);
        }

        public void Destruct()
        {
            _damageText.alpha = 1;
            _rectTransform.DOKill();
        }
    }
}