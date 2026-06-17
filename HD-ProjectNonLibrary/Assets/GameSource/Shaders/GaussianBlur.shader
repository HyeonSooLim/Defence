Shader "Custom/GaussianBlur"
{
    Properties
    {
        _MainTex("SceneTex", 2D) = "white" {}
        _Progress("Progress", Range(0,1)) = 0.0
        _Softness("Softness", Range(0,0.5)) = 0.02
        _Direction("Direction (x,y)", Vector) = (0,1,0,0)
        _BlurPower("Blur Power", Range(0,5)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" }
        Pass // Horizontal Blur
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragX
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

            fixed4 fragX(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 ts = _MainTex_TexelSize.xy;

                float weights[9] = {0.05,0.09,0.12,0.15,0.18,0.15,0.12,0.09,0.05};
                float3 sum = 0;
                for(int x=-4; x<=4; x++)
                {
                    sum += tex2D(_MainTex, uv + float2(x*ts.x,0)).rgb * weights[x+4];
                }
                return fixed4(sum, 1.0);
            }
            ENDCG
        }

        Pass // Vertical Blur + Final
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragY
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Progress;
            float _Softness;
            float2 _Direction;
            float _BlurPower;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

            fixed4 fragY(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 ts = _MainTex_TexelSize.xy;

                float weights[9] = {0.05,0.09,0.12,0.15,0.18,0.15,0.12,0.09,0.05};
                float3 sum = 0;
                for(int y=-4; y<=4; y++)
                {
                    sum += tex2D(_MainTex, uv + float2(0,y*ts.y)).rgb * weights[y+4];
                }
                float3 blurred = sum;

                // 진행도 계산
                float t = dot(uv, normalize(_Direction));
                float reveal = smoothstep(_Progress - _Softness, _Progress + _Softness, t);

                // 블러 파워 적용
                float3 scene = tex2D(_MainTex, uv).rgb;
                float3 finalColor = lerp(scene, blurred, (1.0 - reveal) * _BlurPower);

                return fixed4(finalColor, 1.0);
            }
            ENDCG
        }
    }
}
