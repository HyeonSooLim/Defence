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
            public Material Material = null;
            public RenderPassEvent RenderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        }
        
        public FeatureSettings Settings = new();
        PixelateRenderPass pass;

        public override void Create()
        {
            if (Settings.Material == null)
            {
                Debug.LogWarning(
                    $"[{name}] Material is null. Assign a material using Hidden/PixelatePost shader.");
                return;
            }
            
            // 마테리얼 원본은 수정하지 않음
            Material instanceMaterial = Instantiate(Settings.Material);
            pass = new PixelateRenderPass(instanceMaterial)
            {
                renderPassEvent = Settings.RenderPassEvent,
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if(renderingData.cameraData.camera.name != "MainCamera") return;
            if (Settings.Material == null) return;
            if (pass == null) return;
            renderer.EnqueuePass(pass);
        }

        protected override void Dispose(bool disposing)
        {
            pass?.Dispose();
        }
        
        class PixelateRenderPass : ScriptableRenderPass
        {
            private readonly Material pixelateMaterial;
            private RTHandle tempRT;
            private RTHandle maskRT;

            public PixelateRenderPass(Material mat)
            {
                pixelateMaterial = mat;
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
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

                VolumeStack stack = VolumeManager.instance.stack;
                var settings = stack.GetComponent<PixelateMaskSettings>();
                if (settings == null || !settings.IsActive()) return;

                CommandBuffer cmd = CommandBufferPool.Get("PixelateMaskPass");
                
                // 1. 그릴 타깃을 maskRT로 지정하고, 카메라의 기본 뎁스 버퍼 설정
                CoreUtils.SetRenderTarget(cmd,maskRT, renderingData.cameraData.renderer.cameraColorTargetHandle);
                // 2. 타깃을 검은색으로 클리어.
                CoreUtils.ClearRenderTarget(cmd, ClearFlag.Color, Color.black);
                // 3. 렌더 타깃 컨텍스트가 유지된 상태에서 카메라 행렬을 수동 갱신.(카메라가 고정된 상태일 경우 실행하지 않아도 무방)
                Camera camera = renderingData.cameraData.camera;
                cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
                // 4. GPU에 명령 큐 제출
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();    // 명령 큐 클리어

                // 5. 특정 Layer만 마스크 타깃(maskRT)에 정상 드로우
                var shaderTagId = new ShaderTagId("UniversalForward");
                DrawingSettings drawingSettings = CreateDrawingSettings(shaderTagId, ref renderingData, SortingCriteria.CommonOpaque);
                drawingSettings.SetShaderPassName(1, new ShaderTagId("UniversalForwardOnly"));
                drawingSettings.SetShaderPassName(2, new ShaderTagId("SRPDefaultUnlit"));
                
                var filteringSettings = new FilteringSettings(RenderQueueRange.all, settings.layerMask.value);
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
                
                // 6. 카메라 행렬 복원
                cmd.SetViewProjectionMatrices(renderingData.cameraData.GetViewMatrix(),
                    renderingData.cameraData.GetGPUProjectionMatrix());
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                
                // 7. 마테리얼 세팅
                pixelateMaterial.SetTexture(MaskTexId, maskRT.rt);
                pixelateMaterial.SetFloat(PixelSizeId, settings.maskPixelSize.value);
                
                // 8. 셰이더 처리
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
                DestroyImmediate(pixelateMaterial);
            }
        }
    }
}