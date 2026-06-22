Shader "Custom/ObjectPixelate"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PixelSize ("Pixel Size", Float) = 8
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            //#include "UnityCG.cginc"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize; // (1/width, 1/height, width, height)
            float _PixelSize;

            float4 frag (Varyings i) : SV_Target
            {
                // 1. 화면 픽셀 좌표 구하기 [0, Resolution]
                float2 pixelCoord = i.positionCS.xy; // frag에서 i.positionCS.xy는 이미 스크린 픽셀 좌표입니다.

                // 2. 픽셀 사이즈 단위로 화면 좌표를 스냅 (블록의 중심점)
                float2 snappedPixelCoord = floor(pixelCoord / _PixelSize) * _PixelSize + (_PixelSize * 0.5);

                // 3. [핵심] 현재 화면 픽셀 대비 오브젝트 UV의 변화량(경사도) 계산
                // 화면 x, y축으로 1픽셀 움직일 때 uv가 얼마나 변하는지 알아냅니다.
                float2 uvGradX = ddx(i.uv);
                float2 uvGradY = ddy(i.uv);

                // 4. 화면 기준 스냅된 좌표와 원래 픽셀 좌표의 차이(오프셋) 구하기
                float2 pixelOffset = snappedPixelCoord - pixelCoord;

                // 5. 화면 오프셋을 오브젝트 UV 오프셋으로 변환하여 더해줌
                // 이 과정을 통해 화면 블록 중심에 해당하는 오브젝트 표면의 진짜 UV를 찾아냅니다.
                float2 snappedObjectUV = i.uv + (uvGradX * pixelOffset.x) + (uvGradY * pixelOffset.y);

                // 6. 계산된 정밀한 오브젝트 UV 기반으로 텍스처 샘플링
                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, snappedObjectUV);

                return col;
            }

            ENDHLSL
        }
    }
}