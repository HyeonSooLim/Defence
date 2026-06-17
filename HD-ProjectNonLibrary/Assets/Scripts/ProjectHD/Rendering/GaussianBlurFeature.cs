using ProjectHD.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GaussianBlurFeature : ScriptableRendererFeature
{
    private static readonly int BlurPower = Shader.PropertyToID("_BlurPower");
    private static readonly int Progress = Shader.PropertyToID("_Progress");
    private static readonly int Softness = Shader.PropertyToID("_Softness");
    private static readonly int Direction = Shader.PropertyToID("_Direction");

    [System.Serializable]
    public class FeatureSettings
    {
        public Material material = null;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    }

    public FeatureSettings settings = new FeatureSettings();

    BlurPass blurPass;
    Material blurMaterial;

    public override void Create()
    {
        if (settings.material == null)
        {
            Debug.LogWarning("[PixelateFeature] pixelateMaterial is null. Assign a material using Hidden/PixelatePost shader.");
            return;
        }

        blurPass = new BlurPass(settings.material)
        {
            renderPassEvent = settings.renderPassEvent
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.material == null) return;

        renderer.EnqueuePass(blurPass);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }

    class BlurPass : ScriptableRenderPass
    {
        public Material blurMaterial;

        public BlurPass(Material mat)
        {
            blurMaterial = mat;
        }

        //public RenderTargetHandle tempRT;
        public int tempRTid = Shader.PropertyToID("_TempBlurRT");

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("GaussianBlur");

            var stack = VolumeManager.instance.stack;
            var settings = stack.GetComponent<GaussianBlurSettings>();
            if (settings == null || !settings.IsActive()) return;

            // 파라미터 전달
            blurMaterial.SetFloat(BlurPower, settings.blurPower.value);
            blurMaterial.SetFloat(Progress, settings.progress.value);
            blurMaterial.SetFloat(Softness, settings.softness.value);
            blurMaterial.SetVector(Direction, settings.direction.value);

            // Temp RT 생성
            RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            cmd.GetTemporaryRT(tempRTid, desc);
            var source = renderingData.cameraData.renderer.cameraColorTargetHandle.nameID;

            // Pass 0: Horizontal Blur
            cmd.Blit(source, tempRTid, blurMaterial, 0);

            // Pass 1: Vertical Blur + Final
            cmd.Blit(tempRTid, source, blurMaterial, 1);

            cmd.ReleaseTemporaryRT(tempRTid);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}