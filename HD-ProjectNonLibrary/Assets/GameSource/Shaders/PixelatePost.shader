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
            // 레거시
            //#include "UnityCG.cginc"
            // URP 스타일 표준 함수 포함
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            Texture2D _MainTex;
            SamplerState sampler_MainTex;
            float4 _MainTex_TexelSize;
            
            float _PixelSize;
            float _Progress;
            int _FilterMode;
            int _Mode;
            float _BloomIntensity;
            float _RedBoost;

            // 1. 입력 구조체: 모델 공간 위치와 UV(버텍스 버퍼에서 전달)
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            // 2. 출력/보간 구조체: 클립 공간 위치와 UV(프래그먼트 셰이더로 전달)
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // 3. 버텍스 셰이더: 모델 공간 위치를 클립 공간으로 변환하고 UV 전달
            Varyings vert(Attributes v)
            {
                Varyings o;
                // 레거시 방식: UnityObjectToClipPos는 모델 공간을 클립 공간으로 변환
                //o.positionCS = UnityObjectToClipPos(v.positionOS);
                // URP 스타일 표준 변환 함수 적용
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            // 4. Mip 레벨 샘플링 함수: 주어진 UV와 Mip 레벨로 텍스처 샘플링
            float4 SampleMip(float2 uv, float mip)
            {
                // 플랫폼에 따라 UV 좌표계가 다를 수 있으므로, 텍셀 크기 정보로 보정
                #if UNITY_UV_STARTS_AT_TOP
                if (_MainTex_TexelSize.y < 0)
                    uv.y = 1-uv.y;
                #endif
                return _MainTex.SampleLevel(sampler_MainTex, uv, mip);
            }

            float4 frag(Varyings i) : SV_Target
            {
                // 현재 픽셀의 UV 좌표
                float2 uv = i.uv;
                
                // _ScreenParams는 유니티 전역 변수로, 화면의 픽셀 크기를 제공(x: width, y: height, z: 1/width, w: 1/height)
                // _ScaledScreenParams는 스케일링된 화면 크기를 제공
                float2 screenResolution = float2(_ScaledScreenParams.x, _ScaledScreenParams.y);
                // 픽셀화 그리드 크기 계산: 화면 크기를 픽셀 크기로 나누어 그리드 셀 수 계산
                float2 grid = screenResolution / max(1.0, _PixelSize);

                // 셀 중심 좌표 계산: UV를 그리드 크기로 나누고, floor로 내림하여 셀 인덱스를 구한 후 0.5를 더해 셀 중심으로 이동
                // 현재 픽셀의 중심 좌표
                float2 cell = (floor(uv * grid) + 0.5) / grid;

                float4 pixelCol;

                if (_FilterMode == 0)
                {
                    // 밉맵 레벨 계산: 현재 카메라 텍스처를 쓰므로 feature에서 픽셀 사이즈를 이용하여 밉맵을 제공
                    float mip = max(0.0, log2(max(1.0, _PixelSize)));
                    pixelCol = SampleMip(cell, mip);
                }
                else
                {
                    // 박스 필터링: 셀 중심 주변 4개 샘플의 평균을 사용하여 픽셀 색상 계산(4회 샘플링)
                    float2 off = 0.25 / grid;
                    float4 c1 = _MainTex.Sample(sampler_MainTex, cell + float2(-off.x, -off.y));
                    float4 c2 = _MainTex.Sample(sampler_MainTex, cell + float2(off.x, -off.y));
                    float4 c3 = _MainTex.Sample(sampler_MainTex, cell + float2(-off.x, off.y));
                    float4 c4 = _MainTex.Sample(sampler_MainTex, cell + float2(off.x, off.y));
                    pixelCol = (c1 + c2 + c3 + c4) * 0.25;
                }

                // 이하 모드에 따른 후처리 효과
                if (_Mode == 1)
                {
                    // 후처리 적색 강화
                    pixelCol.r = saturate(pixelCol.r + _RedBoost * _Progress);
                }
                else if (_Mode == 2)
                {
                    // 후처리 블룸 효과
                    float lum = dot(pixelCol.rgb, float3(0.2126, 0.7152, 0.0722));
                    float thr = 0.6;
                    float bloom = smoothstep(thr, 1.0, lum) * _BloomIntensity * _Progress;
                    pixelCol.rgb = saturate(pixelCol.rgb + bloom);
                }

                // 원본 텍스처 색상과 픽셀 텍스처 색상 보간
                float4 orig = _MainTex.Sample(sampler_MainTex, uv);
                float4 outCol = lerp(orig, pixelCol, _Progress);

                return outCol;
            }
            ENDHLSL
        }
    }
}