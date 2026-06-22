Shader "Custom/PixelatePostMask"
{
    Properties
    {
        _MainTex("MainTex", 2D) = "white" {}
        _MaskTex("MaskTex", 2D) = "white" {}
        _MaskPixelSize ("Mask Pixel Size", Float) = 8
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }
            
            TEXTURE2D(_MaskTex);     SAMPLER(sampler_MaskTex);
            TEXTURE2D(_MainTex);     SAMPLER(sampler_MainTex);
            
            float4 _MainTex_TexelSize;
            float _MaskPixelSize;

            float4 frag(Varyings i) : SV_Target
            {
                // 포스트 프로세스 스크린 공간 UV 정합성 보정
                float2 maskUV = i.uv;
                #if UNITY_UV_STARTS_AT_TOP
                if (_MainTex_TexelSize.y < 0)
                    maskUV.y = 1.0 - maskUV.y;
                #endif

                // 원본 화면 버퍼 샘플링
                float4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                
                // 전역 공간에서 수신한 마스크 지도 데이터를 읽습니다.
                float mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, maskUV).r;

                // 마스크 영역 판정 (글로벌 텍스처와 동기화 완료되어 정상 진입)
                if (mask > 0.01)
                {
                    float pixelSize = max(1.0, _MaskPixelSize);
                    float2 screenResolution = _ScaledScreenParams.xy;
                    float2 blockCount = screenResolution / pixelSize;
                    
                    // 화면 격자 스냅 연산
                    float2 snappedUV = (floor(i.uv * blockCount) + 0.5) / blockCount;
                    baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, snappedUV);
                }

                return baseColor;
            }
            ENDHLSL
        }
    }
}