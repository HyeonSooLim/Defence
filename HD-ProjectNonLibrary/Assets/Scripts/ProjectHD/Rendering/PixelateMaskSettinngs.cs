using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ProjectHD.Rendering
{
    [System.Serializable, VolumeComponentMenu("Custom/PixelateMaskSettings")]
    public class PixelateMaskSettings : VolumeComponent, IPostProcessComponent
    {
        // Pixel size in screen pixels per "art pixel" (1 = no pixelation)
        [Tooltip("스크린 픽셀 사이즈 (원본은 1)")]
        public ClampedIntParameter maskPixelSize = new ClampedIntParameter(1, 1, 1024);
        // Enable/disable effect — 이름 변경으로 직렬화 충돌 회피
        public BoolParameter enableEffect = new BoolParameter(true);
        
        public LayerMaskParameter layerMask = new LayerMaskParameter(0);

        // IsActive uses the renamed field
        public bool IsActive() => enableEffect.value;
        public bool IsTileCompatible() => false;
    }
}