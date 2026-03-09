// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "MoveGames/ParticleEdgeDissolve"
{
	Properties
	{
		[HDR]_Color("Color", Color) = (1,1,1,1)
		[NoScaleOffset]_Noise("Noise", 2D) = "white" {}
		_NoiseTexTiling("NoiseTexTiling", Vector) = (1,1,0,0)
		_TileSpeed("TileSpeed", Vector) = (0,0,0,0)
		[HDR]_EdgeColor("EdgeColor", Color) = (0.4716981,0.4716981,0.4716981,0)
		_EdgeTick("EdgeTick", Float) = 0.13
		_SoftParticleFactor("Soft Particle Factor", Range( 0 , 1)) = 0.5
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] _texcoord2( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Off
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#include "UnityCG.cginc"
		#pragma target 3.0
		#pragma surface surf Unlit alpha:fade keepalpha noshadow noambient novertexlights nolightmap  nodynlightmap nodirlightmap nofog nometa noforwardadd 
		struct Input
		{
			float4 vertexColor : COLOR;
			float2 uv_texcoord;
			float2 uv2_texcoord2;
			float4 screenPos;
		};

		uniform float4 _Color;
		uniform sampler2D _Noise;
		uniform float2 _TileSpeed;
		uniform float2 _NoiseTexTiling;
		uniform float _EdgeTick;
		uniform float4 _EdgeColor;
		uniform float _SoftParticleFactor;
		UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
		uniform float4 _CameraDepthTexture_TexelSize;

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float2 uv_TexCoord31 = i.uv_texcoord * _NoiseTexTiling;
			float2 panner32 = ( _Time.x * _TileSpeed + uv_TexCoord31);
			float4 tex2DNode2 = tex2D( _Noise, panner32 );
			float temp_output_6_0 = step( ( tex2DNode2.r - _EdgeTick ) , i.uv2_texcoord2.x );
			o.Emission = ( ( ( _Color * i.vertexColor ) * ( 1.0 - temp_output_6_0 ) ) + ( _EdgeColor * temp_output_6_0 ) ).rgb;
			float4 ase_screenPos = float4( i.screenPos.xyz , i.screenPos.w + 0.00000000001 );
			float4 ase_screenPosNorm = ase_screenPos / ase_screenPos.w;
			ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
			float eyeDepth38 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_screenPosNorm.xy ));
			o.Alpha = ( i.vertexColor.a * ( 1.0 - step( tex2DNode2.r , i.uv2_texcoord2.x ) ) * saturate( ( ( 1.0 - _SoftParticleFactor ) * ( eyeDepth38 - ase_screenPos.w ) * 3.0 ) ) );
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18935
-2529;102;1802;1210;1395.356;725.4858;1;True;False
Node;AmplifyShaderEditor.Vector2Node;45;-1340.86,-418.6618;Inherit;False;Property;_NoiseTexTiling;NoiseTexTiling;2;0;Create;True;0;0;0;False;0;False;1,1;1,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.TextureCoordinatesNode;31;-1157.245,-438.9318;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TimeNode;30;-1133.11,-182.4538;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;29;-1088.313,-311.1705;Float;False;Property;_TileSpeed;TileSpeed;3;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.PannerNode;32;-891.3149,-330.1705;Inherit;False;3;0;FLOAT2;1,1;False;2;FLOAT2;-1,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;35;-533.97,-557.2615;Inherit;False;Property;_EdgeTick;EdgeTick;5;0;Create;True;0;0;0;False;0;False;0.13;0.13;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;36;-798.5162,160.3865;Inherit;False;848;433;Particle Soft Factor;8;44;43;42;41;40;39;38;37;Particle Soft Factor;1,1,1,1;0;0
Node;AmplifyShaderEditor.SamplerNode;2;-685.9003,-359.2001;Inherit;True;Property;_Noise;Noise;1;1;[NoScaleOffset];Create;True;0;0;0;False;0;False;-1;None;0adb51a978ba15d4381794ca675680ab;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TexCoordVertexDataNode;34;-590.1014,-146.0455;Inherit;False;1;2;0;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleSubtractOpNode;5;-342.9001,-469.2002;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0.13;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenPosInputsNode;37;-687.5162,381.3865;Float;False;1;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;39;-748.5162,210.3865;Inherit;False;Property;_SoftParticleFactor;Soft Particle Factor;6;0;Create;True;0;0;0;False;0;False;0.5;0.912;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenDepthNode;38;-686.5162,296.3865;Inherit;False;0;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;40;-434.5163,322.3865;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;42;-435.5163,439.3865;Inherit;False;Constant;_3;3;3;0;Create;True;0;0;0;False;0;False;3;3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;6;-106.9,-470.2002;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;12;-107.984,-1116.883;Inherit;False;Property;_Color;Color;0;1;[HDR];Create;True;0;0;0;False;0;False;1,1,1,1;0.8443396,0.9014888,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;41;-442.5162,215.3865;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;13;-52.184,-924.5835;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;14;179.0156,-993.1834;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;43;-259.5163,297.3865;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;8;-125.3633,-694.6488;Inherit;False;Property;_EdgeColor;EdgeColor;4;1;[HDR];Create;True;0;0;0;False;0;False;0.4716981,0.4716981,0.4716981,0;2.639016,2.639016,2.639016,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StepOpNode;3;-345.8948,-147.6738;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;17;128.5536,-444.1239;Inherit;True;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;23;354.6169,-992.4592;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;9;126.1443,-690.1384;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode;10;-103.2144,-148.4739;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;44;-115.5163,298.3865;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;20;576.576,-715.4641;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;33;132.6445,-174.2182;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;802.0115,-368.3498;Float;False;True;-1;2;ASEMaterialInspector;0;0;Unlit;MoveGames/ParticleEdgeDissolve;False;False;False;False;True;True;True;True;True;True;True;True;False;False;True;False;False;False;False;False;False;Off;1;False;-1;1;False;-1;False;0;False;-1;0;False;-1;False;0;Transparent;0.5;True;False;0;False;Transparent;;Transparent;All;18;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;False;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;31;0;45;0
WireConnection;32;0;31;0
WireConnection;32;2;29;0
WireConnection;32;1;30;1
WireConnection;2;1;32;0
WireConnection;5;0;2;1
WireConnection;5;1;35;0
WireConnection;40;0;38;0
WireConnection;40;1;37;4
WireConnection;6;0;5;0
WireConnection;6;1;34;1
WireConnection;41;0;39;0
WireConnection;14;0;12;0
WireConnection;14;1;13;0
WireConnection;43;0;41;0
WireConnection;43;1;40;0
WireConnection;43;2;42;0
WireConnection;3;0;2;1
WireConnection;3;1;34;1
WireConnection;17;1;6;0
WireConnection;23;0;14;0
WireConnection;23;1;17;0
WireConnection;9;0;8;0
WireConnection;9;1;6;0
WireConnection;10;0;3;0
WireConnection;44;0;43;0
WireConnection;20;0;23;0
WireConnection;20;1;9;0
WireConnection;33;0;13;4
WireConnection;33;1;10;0
WireConnection;33;2;44;0
WireConnection;0;2;20;0
WireConnection;0;9;33;0
ASEEND*/
//CHKSM=963DDA9B863A9F985C0A8432F8285C9FDA0BD97A