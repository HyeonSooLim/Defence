using DG.Tweening;
using UnityEngine;
using Utilities;

namespace ProjectHD
{
    public class SoundManager : Singleton<SoundManager>
    {
        public AudioSource BGMAudioSource;
        public AudioClip TitleBGM;
        public AudioClip LobbyBGM;
        public AudioClip InGameBGM;

        private AudioSourcePool _sfxAudioSourcePool;
        public AudioSource SfxAudioSource;

        private void Start()
        {
            if (SfxAudioSource == null)
            {
                Debug.LogError("SfxAudioSource is not assigned.");
                return;
            }

            _sfxAudioSourcePool = new AudioSourcePool(SfxAudioSource);
        }

        public void PlaySFX(string key)
        {
            if (MainManager.Instance.ResourcePool.TryLoad(key, out AudioClip clip) == false)
            {
                Debug.LogError($"SFX key '{key}' not found in ResourcePool.");
                return;
            }

            PlaySFX(clip);
        }

        public void PlaySFX(AudioClip clip)
        {
            if (clip == null)
            {
                Debug.LogError("SFX clip is null.");
                return;
            }

            _sfxAudioSourcePool?.PlaySFX(clip);
        }

        public float BGMVolume
        {
            get => BGMAudioSource.volume;
            set => BGMAudioSource.volume = Mathf.Clamp01(value);
        }
    }
}
