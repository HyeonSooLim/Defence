using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ProjectHD.Rendering
{
    [System.Serializable, VolumeComponentMenu("Custom/PixelateSettings")]
    public class PixelateSettings : VolumeComponent, IPostProcessComponent
    {
        // Pixel size in screen pixels per "art pixel" (1 = no pixelation)
        public ClampedIntParameter pixelSize = new ClampedIntParameter(1, 1, 1024);

        // If true, adjust pixelSize so screenWidth % pixelSize == 0 and screenHeight % pixelSize == 0
        public BoolParameter snapToDiv = new BoolParameter(true);

        // 0..1 blend between original and pixelated
        public ClampedFloatParameter progress = new ClampedFloatParameter(0f, 0f, 1f);

        // Filter mode: 0 = Mip (default, fast), 1 = Box (direct average)
        public ClampedIntParameter filterMode = new ClampedIntParameter(0, 0, 1);

        // Pixelation mode: 0 = Average/Pixelate, 1 = Red Emphasize, 2 = Bloom
        public ClampedIntParameter mode = new ClampedIntParameter(0, 0, 2);

        // Bloom intensity (used when mode == 2)
        public ClampedFloatParameter bloomIntensity = new ClampedFloatParameter(0.5f, 0f, 5f);

        // Additional: strength for red emphasize
        public ClampedFloatParameter redBoost = new ClampedFloatParameter(0.5f, 0f, 1f);

        // Enable/disable effect — 이름 변경으로 직렬화 충돌 회피
        public BoolParameter enableEffect = new BoolParameter(true);

        // IsActive uses the renamed field
        public bool IsActive() => enableEffect.value && progress.value > 0f;
        public bool IsTileCompatible() => false;
    }
}