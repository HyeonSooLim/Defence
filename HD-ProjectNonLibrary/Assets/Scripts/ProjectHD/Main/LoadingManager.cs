using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectHD
{
    public class LoadingManager : MonoBehaviour
    {
        [SerializeField] private Image _progressBar;
        [SerializeField] private TMP_Text _progressText;

        private void Start()
        {
            Event.EventManager.AddListener<Event.SceneLoadingEvent>(SetProgress);
            _progressBar.fillAmount = 0;
        }

        private void OnDestroy()
        {
            Event.EventManager.RemoveListener<Event.SceneLoadingEvent>(SetProgress);
        }

        private void SetProgress(Event.SceneLoadingEvent evt)
        {
            float progress = Mathf.Clamp01(evt.Progress / 0.9f);
            progress = Mathf.Floor(progress * 1000f) * 0.001f;
            //_progressBar.fillAmount = progress;
            _progressBar.DOFillAmount(progress, 0.5f).SetEase(Ease.OutCubic);
            _progressText.text = string.Format("{0}%", progress * 100);
        }
    }
}