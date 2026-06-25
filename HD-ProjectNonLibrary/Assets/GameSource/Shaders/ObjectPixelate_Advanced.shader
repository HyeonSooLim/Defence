Shader "Custom/ObjectPixelate_Advanced"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PixelSize ("Pixel Size", Float) = 8

        // 기능 제어 토글 및 속성
        [Toggle(_ENABLE_LIGHTING)] _EnableLighting("Enable Lighting", Float) = 1
        [Toggle(_ENABLE_SHADOW)] _EnableShadow("Enable Shadow", Float) = 1
        
        [Toggle(_ENABLE_OUTLINE)] _EnableOutline("Enable Outline", Float) = 0
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineThickness ("Outline Thickness", Range(0, 4)) = 1

        [Toggle(_ENABLE_FRESNEL)] _EnableFresnel("Enable Fresnel", Float) = 0
        _FresnelColor ("Fresnel Color", Color) = (1, 1, 1, 1)
        _FresnelPower ("Fresnel Power", Range(0.1, 10)) = 3
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // [추가] GPU 인스턴싱을 위한 멀티 컴파일 키워드 강제 활성화
            #pragma multi_compile_instancing

            // URP 그림자 및 라이팅 핵심 키워드 활성화
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID // [추가] 정점 데이터 입력 단계 인스턴스 ID 받아오기
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 normalWS     : TEXCOORD1;
                float3 viewDirWS    : TEXCOORD3;
                float4 shadowCoord  : TEXCOORD4;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID // [추가] 프래그먼트 단계로 인스턴스 ID 전달용
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            // 타일링 (아틀라스)가 필요하다면 사용
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
            UNITY_INSTANCING_BUFFER_END(Props)
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_TexelSize;
                float _PixelSize;
                float3 _pad0;
            
                float4 _OutlineColor;
                float _OutlineThickness;
                float3 _pad1;         
            
                float4 _FresnelColor;
                float _FresnelPower;
                float3 _pad2;
            
                float _EnableLighting;
                float _EnableShadow;
                float _EnableOutline; 
                float _EnableFresnel; 
                CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                
                // [추가] 인스턴싱 시스템 초기화 및 전달 데이터 세팅
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(v.normalOS, float4(1,1,1,1));

                o.positionCS = vertexInput.positionCS;
                o.normalWS = normalInput.normalWS;
                o.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                
                // 타일링 (아틀라스 등)을 이용하려면 사용. 대신 드로우콜이 증가함
                // float4 st = UNITY_ACCESS_INSTANCED_PROP(Props, _MainTex_ST);
                // o.uv = v.uv * st.xy + st.zw;
                o.uv = v.uv;
                    
                // 그림자 맵 매핑을 위한 좌표 계산
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
                o.shadowCoord = GetShadowCoord(vertexInput);
                #else
                o.shadowCoord = float4(0, 0, 0, 0);
                #endif

                return o;
            }

            float4 frag (Varyings i) : SV_Target
            {
                // [추가] 프래그먼트 셰이더 내 인스턴싱 시스템 초기화
                UNITY_SETUP_INSTANCE_ID(i);
                
                // 1. 화면 스크린 픽셀 좌표 구하기
                float2 pixelCoord = i.positionCS.xy;

                // 2. 픽셀 사이즈 단위로 스냅된 그리드 중심점 계산
                float2 snappedPixelCoord = floor(pixelCoord / _PixelSize) * _PixelSize + (_PixelSize * 0.5);
                float2 pixelOffset = snappedPixelCoord - pixelCoord;

                // 3. 미분 함수를 이용한 스냅된 정밀 오브젝트 UV 계산
                float2 uvGradX = ddx(i.uv);
                float2 uvGradY = ddy(i.uv);
                float2 snappedObjectUV = i.uv + (uvGradX * pixelOffset.x) + (uvGradY * pixelOffset.y);

                // 4. 텍스처 기본 색상 샘플링
                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, snappedObjectUV);

                // 5. [핵심] 라이팅과 프레넬을 위한 Normal, ViewDir 데이터도 화면 픽셀 단위로 스냅
                float3 normalGradX = ddx(i.normalWS);
                float3 normalGradY = ddy(i.normalWS);
                float3 snappedNormalWS = normalize(i.normalWS + (normalGradX * pixelOffset.x) + (normalGradY * pixelOffset.y));

                float3 viewGradX = ddx(i.viewDirWS);
                float3 viewGradY = ddy(i.viewDirWS);
                float3 snappedViewDirWS = normalize(i.viewDirWS + (viewGradX * pixelOffset.x) + (viewGradY * pixelOffset.y));

                // 기본 라이트 정보 가져오기
                Light mainLight = GetMainLight(i.shadowCoord);

                // 6. 외곽선(Outline) 처리 (화면 미분 함수 기반 스크린 공간 외곽선 검출)
                if (_EnableOutline > 0.5)
                {
                    // 주변 픽셀과의 깊이(Depth) 차이나 노말 차이를 스크린 공간에서 대략적으로 감지
                    float3 ddxN = ddx(i.normalWS) * _OutlineThickness;
                    float3 ddyN = ddy(i.normalWS) * _OutlineThickness;
                    float edgeNormal = length(ddxN) + length(ddyN);

                    float ddxD = ddx(i.positionCS.z) * _OutlineThickness;
                    float ddyD = ddy(i.positionCS.z) * _OutlineThickness;
                    float edgeDepth = length(ddxD) + length(ddyD);

                    // 일정 수치 이상 변화량이 크면 외곽선 영역으로 판단
                    if (edgeNormal > 0.4 || edgeDepth > 0.0001)
                    {
                        return _OutlineColor;
                    }
                }

                // 7. 라이팅 & 그림자(Shadow) 연산
                float3 lighting = float3(1.0, 1.0, 1.0); // 기본값 (라이트 꺼짐 시 흰색 유지)
                
                if (_EnableLighting > 0.5)
                {
                    // 픽셀화 스타일을 위해 라이트 연산을 하프-램프(Half-Lambert)나 셀셰이딩 형태로 커스텀 가능
                    float NdotL = saturate(dot(snappedNormalWS, mainLight.direction));
                    
                    // 그림자 감쇠 값 계산
                    float shadowAttenuation = 1.0;
                    if (_EnableShadow > 0.5)
                    {
                        shadowAttenuation = mainLight.shadowAttenuation;
                        // 그림자 경계도 픽셀 느낌이 나도록 0 또는 1로 강하게 끊어줌 (원치 않으면 제거 가능)
                        shadowAttenuation = shadowAttenuation > 0.5 ? 1.0 : 0.2; 
                    }
                    
                    // 최종 라이트 강도 계산 (인바이런먼트 앰비언트 라이트 추가)
                    float3 ambient = SampleSH(snappedNormalWS);
                    lighting = mainLight.color * (NdotL * shadowAttenuation) + ambient;
                }

                col.rgb *= lighting;

                // 8. 프레넬(Fresnel) 효과 연산
                if (_EnableFresnel > 0.5)
                {
                    // 내적값을 이용해 외곽면 계산 후 픽셀 느낌으로 살짝 단일화
                    float fresnel = 1.0 - saturate(dot(snappedNormalWS, snappedViewDirWS));
                    fresnel = pow(fresnel, _FresnelPower);
                    
                    // 프레넬 컬러를 텍스처 위에 얹어줌
                    col.rgb = lerp(col.rgb, _FresnelColor.rgb, fresnel * _FresnelColor.a);
                }

                return col;
            }
            ENDHLSL
        }

        // URP 실시간 그림자 맵 투사를 위해 필요한 Pass
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}