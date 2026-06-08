Shader "Custom/EdgeReveal_Directional"
{
    Properties
    {
        _MainTex("SceneTex", 2D) = "white" {}
        _EdgeColor("Edge Color", Color) = (0.0,1.0,1.0,1)
        _EdgeThreshold("Edge Threshold", Range(0,1)) = 0.2
        _EdgeStrength("Edge Strength", Range(0,4)) = 1.5
        _Progress("Progress", Range(0,1)) = 0.0
        _Direction("Direction (x,y)", Vector) = (0,1,0,0) // (0,1)=bottom->top, (0,-1)=top->bottom, (1,0)=left->right
        _Softness("Edge Softness", Range(0,0.5)) = 0.02
        _FadeEdge("Edge Fade", Range(0,1)) = 1.0
        _MaskDirection("Mask Direction (x,y)", Vector) = (0,1,0,0)
        _MaskProgress("MaskProgress", Range(0,1)) = 0.2
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _EdgeColor;
            float _EdgeThreshold;
            float _EdgeStrength;
            float _Progress;
            float2 _Direction;
            float _Softness;
            float _FadeEdge;
            float2 _MaskDirection;
            float _MaskProgress;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // luminance(휘도)
            float lum(float3 c) { return dot(c, float3(0.2126, 0.7152, 0.0722)); }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 ts = _MainTex_TexelSize.xy;

                // Sobel kernels on luminance(가장자리 밝기 변화 계산을 위한 3*3 행렬(픽셀))
                float3 c00 = tex2D(_MainTex, uv + float2(-ts.x, -ts.y)).rgb;
                float3 c10 = tex2D(_MainTex, uv + float2(0, -ts.y)).rgb;
                float3 c20 = tex2D(_MainTex, uv + float2(ts.x, -ts.y)).rgb;
                float3 c01 = tex2D(_MainTex, uv + float2(-ts.x, 0)).rgb;
                float3 c11 = tex2D(_MainTex, uv).rgb;
                float3 c21 = tex2D(_MainTex, uv + float2(ts.x, 0)).rgb;
                float3 c02 = tex2D(_MainTex, uv + float2(-ts.x, ts.y)).rgb;
                float3 c12 = tex2D(_MainTex, uv + float2(0, ts.y)).rgb;
                float3 c22 = tex2D(_MainTex, uv + float2(ts.x, ts.y)).rgb;
                
                // [-1   0   +1]
                // [-2   0   +2]
                // [-1   0   +1]
                // 중앙의 변화에 더 민감하게 반응(가중치 2)
                float gx = -lum(c00) - 2.0*lum(c01) - lum(c02) + lum(c20) + 2.0*lum(c21) + lum(c22);
                // [-1  -2  -1]
                // [ 0   0   0]
                // [+1  +2  +1]
                float gy = -lum(c00) - 2.0*lum(c10) - lum(c20) + lum(c02) + 2.0*lum(c12) + lum(c22);
                
                // 엣지 강도
                float edge = sqrt(gx*gx + gy*gy) * _EdgeStrength;
                // threshold + smooth
                float e = smoothstep(_EdgeThreshold, _EdgeThreshold + 0.05, edge);
                
                // 방향에 따른 엣지
                // uv 좌표가 _Direction 위에 있다면 (t = dot(uv, dir)
                float2 dir = normalize(_Direction);
                // uv 좌표가 _MaskDirection 위에 있다면 (mt = dot(uv, maskDir)
                float2 maskDir = normalize(_MaskDirection);
                float t = dot(uv, dir);
                float mt = dot(uv, maskDir);
                
                // 부드럽게 표현하기 (smoothstep(x,y,t)는 t 값이 x보다 작으면 0, y보다 크면 1을 반환)
                float reveal = smoothstep(_Progress - _Softness, _Progress + _Softness, t);
                float maskReveal = smoothstep(_MaskProgress - _Softness, _MaskProgress + _Softness, mt);
                
                float edgeVisible = e * (1.0 - reveal);
                //float maskArea = e * maskReveal; // 마스킹되는 정도이므로 visible 계산은 따로 하지 않는다
                // 픽셀 알파값 = progress에 따른 엣지 * 마스킹 uv좌표 여부 * 엣지페이드
                float result = edgeVisible * maskReveal * _FadeEdge;
                
                // 셰이더 적용될 텍스처(카메라의 렌더 텍스처)
                fixed4 scene = tex2D(_MainTex, uv);
                fixed4 edgeCol = _EdgeColor;
                edgeCol.a = result;
                
                // 최종. 원본 화면에 엣지 블렌딩
                fixed4 outCol = lerp(scene, edgeCol, edgeCol.a);
                
                // overlay pass 이므로 알파값 1 고정
                outCol.a = 1.0;
                return outCol;
            }
            ENDHLSL
        }
    }
    FallBack Off
}