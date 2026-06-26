Shader "Custom/ObjectPixelate_Toon"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PixelSize ("Pixel Size", Range(1, 64)) = 8

        _RimPower ("Rim Power", Range(0.1, 10)) = 3
        _ShadowThreshold ("Shadow Threshold", Range(-1,1)) = 0.5
        _ColorPower ("Color Power", Range(0, 2)) = 1
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
            float _ColorPower; // 이후 16바이트 자동 패딩됨
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

            float4 frag(Varyings i) : SV_Target
            {
                // 인스턴싱 ID 셋업(Attributes와 Varyings 모두 완료)
                UNITY_SETUP_INSTANCE_ID(i);

                float2 uv = i.uv;
                if (_PixelSize != 1)
                {
                    // 픽셀화
                    float2 pixelCoord = i.positionCS.xy; // 현재 화면의 실시간 픽셀 좌표(1080 * 2280)
                    // 현재 좌표를 floor를 이용해 뭉뚱그려 잘라내고 정중앙으로 맞춤
                    float2 snappedPixelCoord = floor(pixelCoord / _PixelSize) * _PixelSize + (_PixelSize * 0.5);
                    // 뭉뚱그린 좌표와 실제 좌표의 차이
                    float2 pixelOffset = snappedPixelCoord - pixelCoord;
                    float2 uvGradX = ddx(i.uv); // 픽셀 가로 방향으로 UV 변화량 측정
                    float2 uvGradY = ddy(i.uv); // 픽셀 세로 방향으로 UV 변화량 측정
                    // ddx와 ddy 는 1픽셀 간의 uv값의 차이 즉, 각각 1픽셀 가로 크기(ddx) 세로 크기(ddy)이다
                    uv = i.uv + uvGradX * pixelOffset.x + uvGradY * pixelOffset.y;
                    // 텍스처 좌표와 화면 픽셀 좌표의 오차(ddx * pixelOffset) 만큼 이동
                }

                // uv 기반 색상 샘플링
                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                // 월드 노말 및 카메라 방향 정규화
                float3 normalWS = normalize(i.normalWS);
                float3 viewDirWS = normalize(i.viewDirWS);

                // 그림자 좌표를 이용하여 메인 라이트 구함
                Light mainLight = GetMainLight(i.shadowCoord);

                // 노멀 dot 빛 방향(-1 에서 1)
                float NdotL = dot(normalWS, mainLight.direction);
                // 그림자 범위 조절
                float shadow = NdotL > _ShadowThreshold ? 1.0 : 0.0;

                // 노멀과 카메라 방향 내적(정면이 0이 되도록 oneminus)
                float rim = 1.0 - saturate(dot(normalWS, viewDirWS));
                rim = pow(rim, _RimPower);
                // Rim Light (텍스처 색 × 라이트 색)
                float3 rimColor = rim * (col.rgb * mainLight.color);

                // 최종 색상
                col.rgb = (col.rgb + rimColor) * _ColorPower * shadow * mainLight.color;

                return col;
            }
            ENDHLSL
        }

        // URP 내장 그림자 생성 패스 사용
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}