Shader "Custom/ObjectPixelate_Toon"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PixelSize ("Pixel Size", Float) = 8

        [Toggle(_ENABLE_OUTLINE)] _EnableOutline("Enable Outline", Float) = 0
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineThickness ("Outline Thickness", Range(0, 4)) = 1

        _RimPower ("Rim Power", Range(0.1, 10)) = 3
        _ShadowThreshold ("Shadow Threshold", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "ForwardToon"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD1;
                float3 viewDirWS  : TEXCOORD2;
                float4 shadowCoord: TEXCOORD3;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_TexelSize;
            float _PixelSize;
            float3 _pd0;
        
            float4 _OutlineColor;
            float _OutlineThickness;
            float3 _pd1;
        
            float _RimPower;
            float _ShadowThreshold;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                VertexPositionInputs posInput = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(v.normalOS, 1.0);

                o.positionCS = posInput.positionCS;
                o.normalWS   = normalInput.normalWS;
                o.viewDirWS  = GetWorldSpaceViewDir(posInput.positionWS);
                o.uv         = v.uv;

                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
                o.shadowCoord = GetShadowCoord(posInput);
                #else
                o.shadowCoord = float4(0,0,0,0);
                #endif

                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                // Pixelation
                float2 pixelCoord = i.positionCS.xy;
                float2 snappedPixelCoord = floor(pixelCoord / _PixelSize) * _PixelSize + (_PixelSize * 0.5);
                float2 pixelOffset = snappedPixelCoord - pixelCoord;
                float2 uvGradX = ddx(i.uv);
                float2 uvGradY = ddy(i.uv);
                float2 snappedUV = i.uv + uvGradX * pixelOffset.x + uvGradY * pixelOffset.y;

                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, snappedUV);

                // Normal / ViewDir
                float3 normalWS = normalize(i.normalWS);
                float3 viewDirWS = normalize(i.viewDirWS);

                Light mainLight = GetMainLight(i.shadowCoord);

                // Toon Shadow with Threshold
                float NdotL = dot(normalWS, mainLight.direction);
                float shadow = NdotL > _ShadowThreshold ? 1.0 : 0.0;

                // Rim Light (텍스처 색 × 라이트 색)
                float rim = 1.0 - saturate(dot(normalWS, viewDirWS));
                rim = pow(rim, _RimPower);
                float3 rimColor = rim * (col.rgb * mainLight.color);

                // 최종 색상
                col.rgb = col.rgb * shadow * mainLight.color + rimColor;

                return col;
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}