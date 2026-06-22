using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ProjectHD.Rendering
{
    public class PixelateMaskFeature : ScriptableRendererFeature
    {
        private static readonly int MaskTexId = Shader.PropertyToID("_MaskTex");
        private static readonly int PixelSizeId = Shader.PropertyToID("_MaskPixelSize");
        
        [System.Serializable]
        public class FeatureSettings
        {
            public Material material = null;
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        }
        
        public FeatureSettings settings = new();
        PixelateRenderPass pass;

        public override void Create()
        {
            if (settings.material == null)
            {
                Debug.LogWarning(
                    $"[{name}] Material is null. Assign a material using Hidden/PixelatePost shader.");
                return;
            }
            
            // 마테리얼 원본은 수정하지 않음
            Material instanceMaterial = Instantiate(settings.material);
            pass = new PixelateRenderPass(instanceMaterial)
            {
                renderPassEvent = settings.renderPassEvent,
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings.material == null) return;
            if (pass == null) return;
            renderer.EnqueuePass(pass);
        }

        protected override void Dispose(bool disposing)
        {
            pass?.Dispose();
        }
        
        class PixelateRenderPass : ScriptableRenderPass
        {
            private Material pixelateMaterial;
            private RTHandle tempRT;
            private RTHandle maskRT;

            public PixelateRenderPass(Material mat)
            {
                pixelateMaterial = mat;
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                var desc = renderingData.cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0; // 마스크 컬러만 받을 것이므로 자체 뎁스는 0

                RenderingUtils.ReAllocateIfNeeded(ref tempRT, desc, name: "_TempPixelateTex");
                RenderingUtils.ReAllocateIfNeeded(ref maskRT, desc, name: "_MaskRT");
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (pixelateMaterial == null) return;
                switch (renderingData.cameraData.cameraType)
                {
                    case CameraType.Preview:
                    case CameraType.SceneView:
                        return;
                }

                // 글로벌 볼륨 세팅 검사
                var stack = VolumeManager.instance.stack;
                var settings = stack.GetComponent<PixelateMaskSettings>();
                if (settings == null || !settings.IsActive()) return;

                CommandBuffer cmd = CommandBufferPool.Get("PixelateMaskPass");

                // [🔥 핵심 해결포인트 1: 순서 정렬 🔥]
                // 1. 그릴 타깃을 maskRT로 지정하고, 카메라의 기본 뎁스 버퍼를 빌려옵니다.
                cmd.SetRenderTarget(maskRT, renderingData.cameraData.renderer.cameraDepthTargetHandle);
                
                // 2. 타깃을 검은색으로 깨끗이 비웁니다.
                cmd.ClearRenderTarget(false, true, Color.black);
                
                // 3. 렌더 타깃 컨텍스트가 유지된 상태에서 카메라 행렬을 수동 갱신합니다.
                Camera camera = renderingData.cameraData.camera;
                cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
                
                // 4. 드로우 준비가 끝난 이 명령셋을 딱 한 번만 제출합니다. 타깃 롤백 현상이 방지됩니다.
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                // 5. 특정 Layer만 마스크 타깃(maskRT)에 정상 드로우
                var shaderTagId = new ShaderTagId("UniversalForward");
                var drawingSettings = CreateDrawingSettings(shaderTagId, ref renderingData, SortingCriteria.CommonOpaque);
                drawingSettings.SetShaderPassName(1, new ShaderTagId("UniversalForwardOnly"));
                drawingSettings.SetShaderPassName(2, new ShaderTagId("SRPDefaultUnlit"));
                
                var filteringSettings = new FilteringSettings(RenderQueueRange.all, settings.layerMask.value);
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
                
                pixelateMaterial.SetTexture(MaskTexId, maskRT.rt);
                pixelateMaterial.SetFloat(PixelSizeId, settings.maskPixelSize.value);
                
                // 6. 안전한 Blit 시퀀스 실행
                RTHandle cameraColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
                cmd.Blit(cameraColorTarget.nameID, tempRT.nameID);
                cmd.Blit(tempRT.nameID, cameraColorTarget.nameID, pixelateMaterial, 0);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public void Dispose()
            {
                tempRT?.Release();
                maskRT?.Release();
            }
        }
    }
}