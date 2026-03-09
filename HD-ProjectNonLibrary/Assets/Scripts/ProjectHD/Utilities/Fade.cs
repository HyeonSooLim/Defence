using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Utilities
{ 
    public class Fade : Singleton<Fade>
    {
        Image m_imageFade;
        Image imageFade
        {
            get
            {
                if (!m_imageFade)
                {
                    Canvas canvas = gameObject.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 10000;
                    m_imageFade = gameObject.AddComponent<Image>();
                    m_imageFade.CrossFadeAlpha(0f, 0f, true);
                }
                return m_imageFade;
            }
        }
        void Awake()
        {
            imageFade.ToString();
        }

        Coroutine m_coroutineFade;
        public bool isFading => m_coroutineFade != null;

        void _StartFade(float fadeOut, float fadeIn, Color color, bool ignoreTimeScale = false)
        {
            if (isFading) return;

            if (fadeOut > 0 && fadeIn > 0f)
                m_coroutineFade = StartCoroutine(IFade(fadeOut, fadeIn, color, ignoreTimeScale));
            else if (fadeOut > 0)
                m_coroutineFade = StartCoroutine(IFadeOut(fadeOut, color, ignoreTimeScale));
            else if (fadeIn > 0)
                m_coroutineFade = StartCoroutine(IFadeIn(fadeIn, color, ignoreTimeScale));
        }

        IEnumerator IFade(float fadeOut, float fadeIn, Color color, bool ignoreTimeScale = false)
        {
            imageFade.color = color;
            imageFade.CrossFadeAlpha(0f, 0f, ignoreTimeScale);
            yield return null;
            imageFade.CrossFadeAlpha(1f, fadeOut, ignoreTimeScale);
            yield return new WaitForSeconds(fadeOut);
            imageFade.CrossFadeAlpha(0f, fadeIn, ignoreTimeScale);
            yield return new WaitForSeconds(fadeIn);
            m_coroutineFade = null;
        }

        IEnumerator IFadeOut(float fadeOut, Color color, bool ignoreTimeScale = false)
        {
            imageFade.color = color;
            imageFade.CrossFadeAlpha(0f, 0f, ignoreTimeScale);
            yield return null;
            imageFade.CrossFadeAlpha(1f, fadeOut, ignoreTimeScale);
            yield return new WaitForSeconds(fadeOut);
            m_coroutineFade = null;
        }

        IEnumerator IFadeIn(float fadeIn, Color color, bool ignoreTimeScale = false)
        {
            imageFade.color = color;
            imageFade.CrossFadeAlpha(1f, 0f, ignoreTimeScale);
            yield return null;
            imageFade.CrossFadeAlpha(0f, fadeIn, ignoreTimeScale);
            yield return new WaitForSeconds(fadeIn);
            m_coroutineFade = null;
        }

        async UniTask _FadeOutAsync(float fadeOut, Color color, bool ignoreTimeScale = false,CancellationToken cancellationToken = default)
        {
            imageFade.color = color;
            imageFade.CrossFadeAlpha(0f, 0f, ignoreTimeScale);
            await UniTask.Yield(cancellationToken);
            imageFade.CrossFadeAlpha(1f, fadeOut, ignoreTimeScale);
            await UniTask.Delay(TimeSpan.FromSeconds(fadeOut), ignoreTimeScale, PlayerLoopTiming.Update, cancellationToken);
        }

        async UniTask _FadeInAsync(float fadeIn, Color color, bool ignoreTimeScale = false, CancellationToken cancellationToken = default)
        {
            imageFade.color = color;
            imageFade.CrossFadeAlpha(1f, 0f, ignoreTimeScale);
            await UniTask.Yield(cancellationToken);
            imageFade.CrossFadeAlpha(0f, fadeIn, ignoreTimeScale);
            await UniTask.Delay(TimeSpan.FromSeconds(fadeIn), ignoreTimeScale, PlayerLoopTiming.Update,cancellationToken);
        }

        void _SetFade(float fade, Color color)
        {
            m_imageFade.color = color;
            m_imageFade.CrossFadeAlpha(fade, 0f, true);
        }

        #region

        public static void StartFade(float fadeOut, float fadeIn, Color color, bool ignoreTimeScale = false)
        {
            Instance._StartFade(fadeOut, fadeIn, color, ignoreTimeScale);
        }

        public static IEnumerator FadeOut(float fadeOut, Color color, bool ignoreTimeScale = false)
        {
            return Instance.IFadeOut(fadeOut, color, ignoreTimeScale);
        }

        public static IEnumerator FadeIn(float fadeIn, Color color, bool ignoreTimeScale = false)
        {
            return Instance.IFadeIn(fadeIn, color, ignoreTimeScale);
        }

        public static async UniTask FadeOutAsync(float fadeOut,
            Color color,
            bool ignoreTimeScale = false,
            CancellationToken cancellationToken = default)
        {
            await Instance._FadeOutAsync(fadeOut, color, ignoreTimeScale, cancellationToken);
        }

        public static async UniTask FadeInAsync(float fadeIn,
            Color color,
            bool ignoreTimeScale = false,
            CancellationToken cancellationToken = default)
        {
            await Instance._FadeInAsync(fadeIn, color, ignoreTimeScale, cancellationToken);
        }

        public static void SetFade(float fade, Color color)
        {
            Instance._SetFade(fade, color);
        }

        #endregion
    }
}