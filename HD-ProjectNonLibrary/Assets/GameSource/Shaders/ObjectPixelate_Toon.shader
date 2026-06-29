Shader "Custom/ObjectPixelate_Toon"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PixelSize ("Pixel Size", Range(1, 16)) = 8

        _RimPower ("Rim Power", Range(0.1, 10)) = 3
        _ShadowThreshold ("Shadow Threshold", Range(-1,1)) = 0.5
        _ColorPower ("Color Power", Range(0, 2)) = 1

        _LightSteps ("Light Steps", Range(2, 8)) = 4
        _LightPower ("Light Power", Range(0, 2)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "ForwardToon"  // 프레임 디버거 식별 등
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // GPU Instancing 기능 활성화
            #pragma multi_compile_instancing
            // 메인 라이트와 그림자 사용
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            // 라이브러리 (코어: 좌표 변환 함수, 라이팅: 조명 연산 함수)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION; // 오브젝트 중심의 3D 정점 좌표
                float3 normalOS   : NORMAL;   // 정점이 바라보는 표면 방향(법선)
                float2 uv         : TEXCOORD0; // 텍스처 좌표 (텍스처 맵핑용)
                UNITY_VERTEX_INPUT_INSTANCE_ID // GPU 인스턴싱을 위한 오브젝트 고유 ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION; // 화면 스크린(클립 공간) 좌표
                float3 normalWS   : TEXCOORD1; // 월드 기준의 표면 방향
                float3 viewDirWS  : TEXCOORD2; // 카메라가 정점을 보는 방향
                float4 shadowCoord: TEXCOORD3; // 실시간 그림자 좌표
                float2 uv         : TEXCOORD0; // 전달받은 텍스처 좌표
                UNITY_VERTEX_INPUT_INSTANCE_ID // 픽셀 연산에서도 인스턴싱 ID 유지
            };

            // 텍스처 자원은 CBUFFER 바깥에 위치
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // SRP 배처를 위한 상수 버퍼(UnityPerMaterial 이름 고정)
            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_TexelSize;
            float _PixelSize;
            float3 _pd0; // 16바이트를 위한 패딩
        
            float _RimPower;
            float _ShadowThreshold;
            float _ColorPower;
            float3 _pd1; // 16바이트를 위한 패딩

            float _LightSteps;
            float _LightPower;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                // GPU Instancing
                // 입력받은 정점의 인스턴싱 ID 활성화
                UNITY_SETUP_INSTANCE_ID(v);
                // Varyings에 인스턴싱 ID 넘김
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                // 정점과 노멀 방향
                VertexPositionInputs posInput = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(v.normalOS, 1.0);

                o.positionCS = posInput.positionCS; // 3D 좌표를 2D 화면 스크린 공간 좌표 변환
                o.normalWS   = normalInput.normalWS; // 노멀 정보를 월드 표준 방향으로 변환
                o.viewDirWS  = GetWorldSpaceViewDir(posInput.positionWS); // 현재 카메라가 정점을 바라보는 방향
                o.uv         = v.uv;

                // 실시간 그림자가 켜져 있다면 월드 좌표를 기반으로 그림자 매핑 좌표 생성
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
                o.shadowCoord = TransformWorldToShadowCoord(posInput.positionWS);
                #else
                o.shadowCoord = float4(0,0,0,0);
                #endif

                return o;
            }

            // 라이트 단계화 함수
            float StepLight(float intensity, float steps)
            {
                return floor(intensity * steps) / steps;
            }

            float4 frag(Varyings i) : SV_Target
            {
                // 인스턴싱 ID 셋업
                UNITY_SETUP_INSTANCE_ID(i);

                float2 uv = i.uv;
                float4 shadowCoord = i.shadowCoord;
                float3 normalWS = i.normalWS; // 오프셋을 적용할 로컬 노멀 변수 생성
                float3 viewDirWS = i.viewDirWS;

                if (_PixelSize != 1)
                {
                    // 1. 화면 기준 픽셀화 좌표 및 오차(Offset) 계산
                    float2 pixelCoord = i.positionCS.xy; 
                    float2 snappedPixelCoord = floor(pixelCoord / _PixelSize) * _PixelSize + (_PixelSize * 0.5);
                    float2 pixelOffset = snappedPixelCoord - pixelCoord;

                    // 2. 텍스처 UV 픽셀화 보정
                    float2 uvGradX = ddx(i.uv); 
                    float2 uvGradY = ddy(i.uv); 
                    uv = i.uv + uvGradX * pixelOffset.x + uvGradY * pixelOffset.y;

                    // 3. 그림자 스크린 좌표(shadowCoord) 픽셀화 보정
                    float4 shadowGradX = ddx(i.shadowCoord);
                    float4 shadowGradY = ddy(i.shadowCoord);
                    shadowCoord = i.shadowCoord + shadowGradX * pixelOffset.x + shadowGradY * pixelOffset.y;

                    // 4. 월드 노멀(normalWS) 픽셀화 보정 (★핵심 추가)
                    // 화면 픽셀의 변화량에 맞춰 노멀 방향도 뚝뚝 끊어지도록 강제합니다.
                    float3 normalGradX = ddx(i.normalWS);
                    float3 normalGradY = ddy(i.normalWS);
                    normalWS = i.normalWS + normalGradX * pixelOffset.x + normalGradY * pixelOffset.y;

                    // 5. 월드 뷰 방향(viewDirWS) 픽셀화 보정 (림 라이트 픽셀화 보정)
                    float3 viewGradX = ddx(i.viewDirWS);
                    float3 viewGradY = ddy(i.viewDirWS);
                    viewDirWS = i.viewDirWS + viewGradX * pixelOffset.x + viewGradY * pixelOffset.y;
                }

                // 픽셀화된 uv 기반 색상 샘플링
                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                // [수정] 보정된 노멀 및 카메라 방향 정규화
                normalWS = normalize(normalWS);
                viewDirWS = normalize(viewDirWS);

                // 보정된 shadowCoord를 사용하여 메인 라이트 샘플링
                Light mainLight = GetMainLight(shadowCoord);

                // [수정] 보정된 normalWS를 사용하여 빛의 방향과의 내적 계산
                // 이제 NdotL 자체가 픽셀 그리드에 맞춰 계단현상이 생깁니다.
                float NdotL = dot(normalWS, mainLight.direction);
                
                // 툰 라이팅 계단화
                float steppedLight = StepLight(saturate(NdotL) + _LightPower, _LightSteps);

                // 그림자 범위 조절 (외곽 실시간 그림자용)
                float shadowAtten = mainLight.shadowAttenuation;
                float shadow = shadowAtten > _ShadowThreshold ? 1.0 : 0.0;

                // 노멀과 카메라 방향 내적 (Rim 라이트도 픽셀화된 노멀의 영향을 받음)
                float rim = 1.0 - saturate(dot(normalWS, viewDirWS));
                rim = pow(rim, _RimPower);
                
                float3 rimColor = rim * (col.rgb * mainLight.color);

                // 최종 색상 계산
                float3 finalColor = (col.rgb * steppedLight + rimColor) * _ColorPower * shadow * mainLight.color;

                return float4(finalColor, col.a);
            }
            ENDHLSL
        }

        // URP 내장 그림자 생성 패스 사용
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}