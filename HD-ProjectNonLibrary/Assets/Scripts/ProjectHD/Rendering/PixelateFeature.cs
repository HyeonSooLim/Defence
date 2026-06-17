using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ProjectHD.Rendering
{
    [System.Serializable]
    public class PixelateFeature : ScriptableRendererFeature
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static UnityEngine.Profiling.CustomSampler Sampler = UnityEngine.Profiling.CustomSampler.Create("PixelateFeature");
#endif

        private static readonly int PixelSize = Shader.PropertyToID("_PixelSize");
        private static readonly int Progress = Shader.PropertyToID("_Progress");
        private static readonly int FilterMode = Shader.PropertyToID("_FilterMode");
        private static readonly int Mode = Shader.PropertyToID("_Mode");
        private static readonly int BloomIntensity = Shader.PropertyToID("_BloomIntensity");
        private static readonly int RedBoost = Shader.PropertyToID("_RedBoost");

        [System.Serializable]
        public class FeatureSettings
        {
            public Material material = null;
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        }

        public FeatureSettings settings = new FeatureSettings();
        PixelatePass pixelatePass;

        public override void Create()
        {
            if (settings.material == null)
            {
                Debug.LogWarning("[PixelateFeature] pixelateMaterial is null. Assign a material using Hidden/PixelatePost shader.");
                return;
            }

            pixelatePass = new PixelatePass(settings.material)
            {
                renderPassEvent = settings.renderPassEvent
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings.material == null) return;
            renderer.EnqueuePass(pixelatePass);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        class PixelatePass : ScriptableRenderPass
        {
            Material mat;
            RTHandle tempRT;
            RTHandle lowRT;
            int maxPixelSize = 6;

            public PixelatePass(Material material)
            {
                this.mat = material;
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                var desc = renderingData.cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;

                // 원본 크기 tempRT 준비
                RenderingUtils.ReAllocateIfNeeded(ref tempRT, desc, name: "_PixelateTempRT");

                // 저해상도 RT는 최대 다운샘플 크기 기준으로 준비
                desc.width = Mathf.Max(1, desc.width / maxPixelSize);
                desc.height = Mathf.Max(1, desc.height / maxPixelSize);
                RenderingUtils.ReAllocateIfNeeded(ref lowRT, desc, name: "_PixelateLowRT");
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (mat == null) return;
                var stack = VolumeManager.instance.stack;
                var settings = stack.GetComponent<PixelateSettings>();
                if (settings == null || !settings.IsActive()) return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Sampler.Begin();
#endif

                int pixelSize = settings.pixelSize.value;
                bool snap = settings.snapToDiv.value;
                float progress = settings.progress.value;
                int filterMode = settings.filterMode.value; // 0=mip, 1=box
                int mode = settings.mode.value; // 0=avg, 1=red, 2=bloom
                float bloomIntensity = settings.bloomIntensity.value;
                float redBoost = settings.redBoost.value;

                int adjustedPixelSize = AdjustPixelSizeForSnap(pixelSize, snap);

                mat.SetFloat(PixelSize, adjustedPixelSize);
                mat.SetFloat(Progress, progress);
                mat.SetInt(FilterMode, filterMode);
                mat.SetInt(Mode, mode);
                mat.SetFloat(BloomIntensity, bloomIntensity);
                mat.SetFloat(RedBoost, redBoost);

                CommandBuffer cmd = CommandBufferPool.Get("PixelatePass");
                var source = renderingData.cameraData.renderer.cameraColorTargetHandle;

                if (filterMode == 0 && adjustedPixelSize > maxPixelSize)
                {
                    // 원본 → 저해상도 RT
                    cmd.Blit(source, lowRT);

                    // 저해상도 → tempRT (셰이더 적용)
                    cmd.Blit(lowRT, tempRT, mat, 0);
                }
                else
                {
                    // 원본 → tempRT (셰이더 적용)
                    cmd.Blit(source, tempRT, mat, 0);
                }

                // tempRT → 원본
                cmd.Blit(tempRT, source);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Sampler.End();
#endif
            }

            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                // RTHandle은 자동 관리되므로 Release 필요 없음
            }

            // If snap is enabled, find a pixelSize that divides both width and height.
            // Simple search: try requested, then expand +/- until find divisor pair.
            int AdjustPixelSizeForSnap(int requestedSize, bool snap)
            {
                if (!snap || requestedSize <= 1) return Mathf.Max(1, requestedSize);

                int w = Screen.width;
                int h = Screen.height;
                int maxCandidate = Mathf.Min(w, h);
                int best = requestedSize;

                // If requested already divides both, return
                if (w % requestedSize == 0 && h % requestedSize == 0)
                    return requestedSize;

                // Search outward for nearest divisor that divides both
                int maxDelta = Mathf.Max(64, requestedSize * 4);
                for (int delta = 1; delta <= maxDelta; delta++)
                {
                    int down = requestedSize - delta;
                    if (down >= 1 && w % down == 0 && h % down == 0) { best = down; break; }
                    int up = requestedSize + delta;
                    if (up <= maxCandidate && w % up == 0 && h % up == 0) { best = up; break; }
                }

                // Fallback: clamp to 1 if nothing found
                return Mathf.Clamp(best, 1, maxCandidate);
            }
        }
    }
}