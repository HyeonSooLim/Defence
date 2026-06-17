Shader "Custom/PixelatePost"
{
    Properties
    {
        _MainTex("MainTex", 2D) = "white" {}
        _PixelSize("PixelSize", Float) = 1
        _Progress("Progress", Range(0,1)) = 0
        _FilterMode("FilterMode", Int) = 0
        _Mode("Mode", Int) = 0
        _BloomIntensity("BloomIntensity", Float) = 0.5
        _RedBoost("RedBoost", Float) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // Texture / Sampler declarations
            Texture2D _MainTex;
            SamplerState sampler_MainTex;
            float4 _MainTex_TexelSize;
            
            float _PixelSize;
            float _Progress;
            int _FilterMode;
            int _Mode;
            float _BloomIntensity;
            float _RedBoost;
            // NOTE: _ScreenParams is provided by Unity globally; do NOT redeclare it here.

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = UnityObjectToClipPos(v.positionOS);
                o.uv = v.uv;
                return o;
            }

            // Helper: sample with mip level (approximate average)
            float4 SampleMip(float2 uv, float mip)
            {
                #if UNITY_UV_STARTS_AT_TOP
                if (_MainTex_TexelSize.y < 0)
                    uv.y = 1-uv.y;
                #endif
                return _MainTex.SampleLevel(sampler_MainTex, uv, mip);
            }

            float4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.uv;

                // Use Unity-provided _ScreenParams (x = width, y = height)
                float2 screenPx = float2(_ScreenParams.x, _ScreenParams.y);
                float2 grid = screenPx / max(1.0, _PixelSize);

                // Compute cell UV center
                float2 cell = (floor(uv * grid) + 0.5) / grid;

                float4 pixelCol;

                if (_FilterMode == 0)
                {
                    // Mip-based approximation
                    float mip = max(0.0, log2(max(1.0, _PixelSize)));
                    pixelCol = SampleMip(cell, mip);
                }
                else
                {
                    // Box filter: sample 4 points inside the cell and average
                    float2 off = 0.25 / grid;
                    float4 c1 = _MainTex.Sample(sampler_MainTex, cell + float2(-off.x, -off.y));
                    float4 c2 = _MainTex.Sample(sampler_MainTex, cell + float2(off.x, -off.y));
                    float4 c3 = _MainTex.Sample(sampler_MainTex, cell + float2(-off.x, off.y));
                    float4 c4 = _MainTex.Sample(sampler_MainTex, cell + float2(off.x, off.y));
                    pixelCol = (c1 + c2 + c3 + c4) * 0.25;
                }

                // Mode-specific adjustments
                if (_Mode == 1)
                {
                    // Red emphasize: boost red channel by redBoost * progress
                    pixelCol.r = saturate(pixelCol.r + _RedBoost * _Progress);
                }
                else if (_Mode == 2)
                {
                    // Simple bloom: threshold luminance and add glow
                    float lum = dot(pixelCol.rgb, float3(0.2126, 0.7152, 0.0722));
                    float thr = 0.6;
                    float bloom = smoothstep(thr, 1.0, lum) * _BloomIntensity * _Progress;
                    pixelCol.rgb = saturate(pixelCol.rgb + bloom);
                }

                // Blend with original by progress
                float4 orig = _MainTex.Sample(sampler_MainTex, uv);
                float4 outCol = lerp(orig, pixelCol, _Progress);

                return outCol;
            }
            ENDHLSL
        }
    }
}