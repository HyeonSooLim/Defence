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
        private static readonly int Mode = Shader.PropertyToID("_FilterMode");
        private static readonly int Mode1 = Shader.PropertyToID("_Mode");
        private static readonly int BloomIntensity = Shader.PropertyToID("_BloomIntensity");
        private static readonly int RedBoost = Shader.PropertyToID("_RedBoost");

        [System.Serializable]
        public class FeatureSettings
        {
            public Material pixelateMaterial = null;
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        }

        public FeatureSettings settings = new FeatureSettings();

        PixelatePass pixelatePass;

        public override void Create()
        {
            if (settings.pixelateMaterial == null)
            {
                Debug.LogWarning("[PixelateFeature] pixelateMaterial is null. Assign a material using Hidden/PixelatePost shader.");
                return;
            }

            pixelatePass = new PixelatePass(settings.pixelateMaterial)
            {
                renderPassEvent = settings.renderPassEvent
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings.pixelateMaterial == null) return;
            
            renderer.EnqueuePass(pixelatePass);
        }

        class PixelatePass : ScriptableRenderPass
        {
            Material mat;
            RenderTargetIdentifier source;
            
            int tempRTId = Shader.PropertyToID("_PixelateTempRT");
            int lowRTId = Shader.PropertyToID("_PixelateLowRT");

            public PixelatePass(Material material)
            {
                this.mat = material;
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

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (mat == null) return;
                
                // cameraColorTarget is valid in URP 14 (이제는 권장되지 않으므로 RTHandle의 nameID를 가져옴)
                source = renderingData.cameraData.renderer.cameraColorTargetHandle.nameID;
                var stack = VolumeManager.instance.stack;
                var comp = stack.GetComponent<PixelateSettings>();
                if (comp == null || !comp.IsActive()) return;

                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Sampler.Begin();
                #endif
                
                // Read parameters from VolumeComponent
                int pixelSize = comp.pixelSize.value;
                bool snap = comp.snapToDiv.value;
                float progress = comp.progress.value;
                int filterMode = comp.filterMode.value; // 0 = mip, 1 = box
                int mode = comp.mode.value; // 0 = avg, 1 = red, 2 = bloom
                float bloomIntensity = comp.bloomIntensity.value;
                float redBoost = comp.redBoost.value;

                // Adjust pixel size if snap requested
                int adjustedPixelSize = AdjustPixelSizeForSnap(pixelSize, snap);

                // Set shader properties
                mat.SetFloat(PixelSize, adjustedPixelSize);
                mat.SetFloat(Progress, progress);
                mat.SetInt(Mode, filterMode);
                mat.SetInt(Mode1, mode);
                mat.SetFloat(BloomIntensity, bloomIntensity);
                mat.SetFloat(RedBoost, redBoost);
                // NOTE: _ScreenParams is provided by Unity globally in shader; no need to set here.

                CommandBuffer cmd = CommandBufferPool.Get("PixelatePass");

                // Get camera descriptor for RT size
                RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
                int w = desc.width;
                int h = desc.height;

                // 1) 임시 RT 생성 (원본 크기) - 결과를 여기에 저장
                cmd.GetTemporaryRT(tempRTId, w, h, 0, FilterMode.Bilinear, desc.colorFormat);

                if (filterMode == 0)
                {
                    // Mip 근사 모드이지만 GenerateMips를 피하기 위해 다운샘플 폴백 사용
                    // 2) 저해상도 RT 크기 계산
                    int lowW = Mathf.Max(1, Mathf.CeilToInt((float)w / Mathf.Max(1, adjustedPixelSize)));
                    int lowH = Mathf.Max(1, Mathf.CeilToInt((float)h / Mathf.Max(1, adjustedPixelSize)));

                    // 안전을 위해 최소 1x1 보장
                    lowW = Mathf.Max(1, lowW);
                    lowH = Mathf.Max(1, lowH);

                    // 3) 저해상도 RT 생성
                    cmd.GetTemporaryRT(lowRTId, lowW, lowH, 0, FilterMode.Bilinear, desc.colorFormat);

                    // 4) 원본 -> 저해상도 (하드웨어 다운샘플)
                    cmd.Blit(source, lowRTId);

                    // 5) 셰이더에 저해상도 텍스처 바인딩
                    // Material의 _MainTex는 Blit에서 덮어씌워지므로 안전. 하지만 명시적으로 바인딩하려면 아래 사용 가능:
                    // cmd.SetGlobalTexture("_PixelateLowTex", lowRTId);
                    // mat.SetTexture("_MainTex", ???) // 보통 Blit이 자동 바인딩하므로 생략 가능

                    // 6) 저해상도 -> tempRT : 픽셀화 셰이더가 lowRT를 읽도록 Blit 수행
                    // 여기서 셰이더는 grid 계산을 할 때 _PixelSize를 고려하여 동작하도록 설계되어야 함.
                    // 우리는 lowRT를 소스로 전달하므로 셰이더는 lowRT의 해상도에 맞춰 1샘플로 읽으면 됨.
                    cmd.Blit(lowRTId, tempRTId, mat, 0);

                    // 7) 해제
                    cmd.ReleaseTemporaryRT(lowRTId);
                }
                else
                {
                    // Box 필터 모드 또는 기본: 원본을 직접 셰이더로 처리
                    cmd.Blit(source, tempRTId, mat, 0);
                }

                // 8) tempRT -> source (덮어쓰기)
                cmd.Blit(tempRTId, source);

                cmd.ReleaseTemporaryRT(tempRTId);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
                
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Sampler.End();
                #endif
            }
        }
    }
}