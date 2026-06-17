
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ProjectHD.Rendering
{
    [System.Serializable, VolumeComponentMenu("Custom/GaussianBlurSettings")]
    public class GaussianBlurSettings : VolumeComponent, IPostProcessComponent
    {
        // 0..1 blend between original and pixelated
        public ClampedFloatParameter progress = new ClampedFloatParameter(0f, 0f, 1f);

        // Bloom intensity (used when mode == 2)
        public ClampedFloatParameter blurPower = new ClampedFloatParameter(1f, 0f, 5f);

        // Additional: strength for red emphasize
        public ClampedFloatParameter softness = new ClampedFloatParameter(0f, 0.02f, 0.5f);

        // Direction of the blur (normalized vector)
        public Vector2Parameter direction = new Vector2Parameter(new Vector2(0, 1));

        // Enable/disable effect — 이름 변경으로 직렬화 충돌 회피
        public BoolParameter enableEffect = new BoolParameter(true);

        // IsActive uses the renamed field
        public bool IsActive() => enableEffect.value && progress.value > 0f;
        public bool IsTileCompatible() => false;
    }
}
