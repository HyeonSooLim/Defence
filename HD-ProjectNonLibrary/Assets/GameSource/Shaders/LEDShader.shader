Shader "Unlit/LedShader" 
{
    Properties 
    {
        [NoScaleOffset]_MainTex ("Texture", 2D) = "white" {} 
        [NoScaleOffset]_LEDTex("LED Texture", 2D) = "white" {} 
        _Tiling("Tiling", float) = 1 
        _Brightness("Brightness", range(0,20)) = 1
        _GlowPower("Glow Power", range(0, 5)) = 1
        _OffsetX("OffsetX", float) = 0 
        _OffsetY("OffsetY", float) = 0 
    }
    
    SubShader 
    {
        Tags { "RenderType"="Opaque" } 
        LOD 100 

        Pass 
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "UnityCG.cginc"

            struct appdata 
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f 
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1) 
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _LEDTex;
            
            // _MainTex_ST는 TRANSFORM_TEX에 필요하지만 프로퍼티 선언에서 제외되었습니다.
            float4 _MainTex_ST; 
            
            float _Tiling;
            float _Brightness;
            float _OffsetX, _OffsetY;
            float _GlowPower;

            v2f vert (appdata v) 
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target 
            {
                // // 1. _Tiling이 너무 작으면 영상이 깨지므로 최소값 보정
                // float tiling = max(_Tiling, 1.0);

                // // 2. 비디오 텍스처의 실제 UV 영역(Tiling/Offset)을 반영하여 픽셀화 계산
                // TRANSFORM_TEX가 적용된 i.uv를 기반으로 픽셀화합니다.
                // float2 pUV = floor(i.uv * tiling) / tiling;
    
                // // 3. 인스펙터에서 조절하는 Offset 추가
                // pUV += float2(_OffsetX, _OffsetY);

                // // 4. 비디오 텍스처 샘플링 (c) 및 LED 마스크 샘플링 (d)
                // fixed4 c = tex2D(_MainTex, pUV);
    
                // // LED 텍스처는 i.uv를 그대로 사용하여 격자가 밀리지 않게 함
                // fixed4 d = tex2D(_LEDTex, i.uv * tiling); 
    
                // // 5. 최종 결과물
                // fixed4 col = c * d * _Brightness;

                // UNITY_APPLY_FOG(i.fogCoord, col);
                // return col;

                float tiling = max(_Tiling, 1.0);
                float2 pUV = floor(i.uv * tiling) / tiling;
                pUV += float2(_OffsetX, _OffsetY);

                fixed4 c = tex2D(_MainTex, pUV);
                fixed4 d = tex2D(_LEDTex, i.uv * tiling); 
    
                // 단순 곱셈이 아니라, 밝기를 HDR 영역으로 끌어올림
                // d(LED 텍스처)의 흰 부분을 강조하여 빛나게 만듭니다.
                fixed4 col = c * d * _Brightness;
    
                // 글로우를 위해 특정 밝기 이상의 값을 가중치로 부여
                col.rgb += col.rgb * d.rgb * _GlowPower;

                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}