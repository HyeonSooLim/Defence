using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

namespace ProjectHD.UI
{
    public class EdgeRevealController : MonoBehaviour
    {
        private static readonly int Direction1 = Shader.PropertyToID("_Direction");
        private static readonly int Progress = Shader.PropertyToID("_Progress");
        public Camera targetCamera;
        public Camera overlayCamera;
        public RawImage overlayRaw; // RawImage full-screen
        public Material mat; // 위 셰이더 머티리얼
        private RenderTexture rt;

        private void Start()
        {
            rt = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.Default);
            mat = Instantiate(mat);
            if (overlayCamera != null)
                overlayCamera.targetTexture = rt;
            targetCamera.targetTexture = rt;
            overlayRaw.texture = rt;
            overlayRaw.material = mat;
        }

        private void OnRectTransformDimensionsChange()
        {
            if (rt == null)
                return;
            
            // 해상도 변경 처리(선택)
            if (rt.width != Screen.width || rt.height != Screen.height)
            {
                rt.Release();
                rt = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.Default);
                if (overlayCamera != null)
                    overlayCamera.targetTexture = rt;
                targetCamera.targetTexture = rt;
                overlayRaw.texture = rt;
            }
        }
        
        [Button]
        public void PlayReveal(Direction dir, float duration)
        {
            Vector2 d = DirectionToVector(dir);
            mat.SetVector(Direction1, new Vector4(d.x, d.y, 0, 0));
            StartCoroutine(AnimateProgress(0f, 1f, duration));
        }

        [Button]
        public void PlayHide(Direction dir, float duration)
        {
            Vector2 d = DirectionToVector(dir);
            mat.SetVector(Direction1, new Vector4(d.x, d.y, 0, 0));
            StartCoroutine(AnimateProgress(1f, 0f, duration));
        }

        IEnumerator AnimateProgress(float from, float to, float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float p = Mathf.SmoothStep(from, to, t / dur);
                mat.SetFloat(Progress, p);
                yield return null;
            }
            mat.SetFloat(Progress, to);
        }

        Vector2 DirectionToVector(Direction dir)
        {
            switch (dir)
            {
                case Direction.BottomToTop: return new Vector2(0, 1);
                case Direction.TopToBottom: return new Vector2(0, -1);
                case Direction.LeftToRight: return new Vector2(1, 0);
                case Direction.RightToLeft: return new Vector2(-1, 0);
                default: return new Vector2(0, 1);
            }
        }

        public enum Direction { BottomToTop, TopToBottom, LeftToRight, RightToLeft }
    }
}
