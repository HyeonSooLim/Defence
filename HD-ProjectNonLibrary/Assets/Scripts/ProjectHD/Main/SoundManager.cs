using DG.Tweening;
using UnityEngine;
using Utilities;

namespace ProjectHD
{
    public class SoundManager : Singleton<SoundManager>
    {
        public AudioSource BGMAudioSource;

        public AudioSource SfxAudioSource;
        private AudioSourcePool _sfxAudioSourcePool;

        private void Start()
        {
            if (SfxAudioSource == null)
            {
                Debug.LogError("SfxAudioSource is not assigned.");
                return;
            }

            _sfxAudioSourcePool = new AudioSourcePool(SfxAudioSource);
        }

        public void PlayBGM(string key, bool isLoop = true, float volume = 1f)
        {
            if (MainManager.Instance.ResourcePool.TryLoad(key, out AudioClip clip) == false)
            {
                Debug.LogError($"SFX key '{key}' not found in ResourcePool.");
                return;
            }

            if (BGMAudioSource == null)
                return;

            BGMAudioSource.Stop();
            BGMAudioSource.loop = isLoop;
            BGMAudioSource.clip = clip;
            BGMVolume = volume;
            BGMAudioSource.volume = BGMVolume;
            BGMAudioSource.Play();
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
