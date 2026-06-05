using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace ProjectHD.UI
{
    public class LEDEffectPanel : MonoBehaviour
    {
        public Camera targetCamera;
        public RawImage overlayRaw; // RawImage full-screen
        public Material mat; // 위 셰이더 머티리얼
        public GameObject Canvas;
        RenderTexture rt;
        List<Camera> overlayCamera;

        public float targetTiling = 500f; // 최종 타일링 값

        void Start()
        {
            var letterBox = Utilities.LetterBox.Instance;

            Event.EventManager.AddListener<Event.SceneLoadingCompleteEvent>(SceneLoadingCompleteEventAction);

            SetInstanceMaterial();
            CreateRenderTexture();
            Canvas.SetActive(false);

            var cameraData = targetCamera.GetUniversalAdditionalCameraData();
            if (cameraData.cameraStack.Count > 0)
                overlayCamera = cameraData.cameraStack;
        }

        private void OnDestroy()
        {
            Event.EventManager.RemoveListener<Event.SceneLoadingCompleteEvent>(SceneLoadingCompleteEventAction);

            if (rt != null)
            {
                rt.Release();
                Destroy(rt);
                rt = null;
            }
            overlayCamera = null;
        }

        private void SceneLoadingCompleteEventAction(Event.SceneLoadingCompleteEvent e)
        {
            if (e.CurrentSceneName != ProjectEnum.SceneName.BattleWorkSpace)
                return;
            PlayHide(1f);
        }

        public void PlayReveal(float duration)
        {
            StartCoroutine(AnimateProgress(targetTiling, 0, duration));
        }

        public void PlayHide(float duration)
        {
            ScreenCheckAndUpdate();
            SetRenderTexture(true);
            Canvas.SetActive(true);
            StartCoroutine(AnimateProgress(0, targetTiling, duration, true));
        }

        IEnumerator AnimateProgress(float from, float to, float dur, bool isHide = false)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float p = Mathf.SmoothStep(from, to, t / dur);
                mat.SetFloat("_Tiling", p);
                yield return null;
            }
            mat.SetFloat("_Tiling", to);

            if (isHide)
            {
                SetRenderTexture(false);
                Canvas.SetActive(false);
            }
        }

        private void ScreenCheckAndUpdate()
        {
            if (rt == null)
            {
                CreateRenderTexture();
                return;
            }

            if (rt.width != Screen.width || rt.height != Screen.height)
            {
                rt.Release();
                CreateRenderTexture();
            }
        }

        private void SetRenderTexture(bool enable)
        {
            if (enable)
            {

                targetCamera.targetTexture = rt;
                if (overlayCamera != null)
                {
                    for (int i = 0; i < overlayCamera.Count; i++)
                    {
                        overlayCamera[i].targetTexture = rt;
                    }
                }
            }
            else
            {
                targetCamera.targetTexture = null;
                if (overlayCamera != null)
                {
                    for (int i = 0; i < overlayCamera.Count; i++)
                    {
                        overlayCamera[i].targetTexture = null;
                    }
                }
            }
        }

        private void CreateRenderTexture()
        {
            rt = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.Default);
            mat.SetTexture("_MainTex", rt);
        }

        private void SetInstanceMaterial()
        {
            Material created = Instantiate(mat);
            mat = created;
            overlayRaw.material = created;
        }
    }
}
