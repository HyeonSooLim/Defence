using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectHD
{
    public class SmartButton : Button
    {
        [SerializeField]
        private AssetReferenceT<AudioClip> _uiClickSound;

        [SerializeField]
        private int _delayTime = 300;

        [SerializeField]
        private CanvasGroup _canvasGroup;
        [SerializeField]
        private float _activeAlphaValue = 0.7f;
        [SerializeField]
        private float _inactiveAlphaValue = 0.3f;

        [SerializeField]
        private bool _setInteractable = false;

        //[SerializeField]
        //private MixerGroupType _mixerGroupType = MixerGroupType.UI;

        [Tooltip("true시 다른버튼과 딜레이상태를 공유합니다.")]
        [SerializeField]
        private bool _useShareDelay = true;

        public static bool IsDelay { get; private set; }
        private bool _isCustomDelay;
        private bool _isEnableToTrigger = true;

        private bool DelayState
        {
            get
            {
                return _useShareDelay ? IsDelay : _isCustomDelay;
            }

            set
            {
                if (_useShareDelay)
                {
                    IsDelay = value;
                }
                else
                {
                    _isCustomDelay = value;
                }
            }
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            Press();
        }

        public void SetActiveButton(bool onOff)
        {
            _isEnableToTrigger = onOff;
            SetCanvasAlpha(onOff);
        }

        public void SetCanvasAlpha(bool onOff)
        {
            if (_canvasGroup)
                _canvasGroup.alpha = onOff ? _activeAlphaValue : _inactiveAlphaValue;
        }

        public void SetCanvasAlpha(float val)
        {
            if (_canvasGroup)
                _canvasGroup.alpha = val;
        }

        private void Press()
        {
            if (!IsActive() || !IsInteractable() || DelayState)
                return;

            if (_delayTime > 0)
                DelayButton();

            UISystemProfilerApi.AddMarker("Button.onClick", this);
            PressSound();

            if (!_isEnableToTrigger)
                return;

            onClick.Invoke();

        }

        private void PressSound()
        {
            if (_uiClickSound == null || _uiClickSound.RuntimeKey == "")
                return;

            //switch (_mixerGroupType)
            //{
            //    case MixerGroupType.UI:
            //        MainManager.Instance.PlayUIAudio(_uiClickSound);
            //        break;
            //    case MixerGroupType.Voice:
            //        MainManager.Instance.PlayVoiceAudio(_uiClickSound);
            //        break;
            //    case MixerGroupType.Effect:
            //        MainManager.Instance.PlayEffectAudio(_uiClickSound);
            //        break;
            //    case MixerGroupType.Background:
            //        MainManager.Instance.PlayBGMAudio(_uiClickSound);
            //        break;
            //}
        }

        private async void DelayButton()
        {
            if (_setInteractable)
            {
                interactable = false;
            }
            DelayState = true;
            await UniTask.Delay(_delayTime);
            DelayState = false;
            if (_setInteractable)
            {
                interactable = true;
            }
        }
    }
}