// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "MoveGames/ParticleCustom_TR"
{
	Properties
	{
		[NoScaleOffset]_MainTex("MainTex", 2D) = "white" {}
		_MainTexTiling("MainTexTiling", Vector) = (1,1,0,0)
		[Toggle]_UseMainTex("UseMainTex", Float) = 1
		[HDR]_Color("Color", Color) = (0,0,0,0)
		[NoScaleOffset]_DissolveTex("DissolveTex", 2D) = "white" {}
		_DissolveTexTiling("DissolveTexTiling", Vector) = (1,1,0,0)
		_DistortLevel("DistortLevel", Range( 0 , 1)) = 0
		[NoScaleOffset]_Mask("Mask", 2D) = "white" {}
		_OpacityLevel("Opacity Level", Range( 0 , 1)) = 0
		_Max("Max", Float) = 2
		[NoScaleOffset]_ColorGradient("ColorGradient", 2D) = "white" {}
		_SoftParticleFactor("Soft Particle Factor", Range( 0 , 1)) = 0.5
		[Toggle]_NotUseParticleSoftFactor("NotUseParticleSoftFactor", Float) = 0
		[HideInInspector] _texcoord2( "", 2D ) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Off
		CGPROGRAM
		#include "UnityCG.cginc"
		#pragma target 3.0
		#pragma exclude_renderers xboxseries playstation switch nomrt 
		#pragma surface surf Unlit alpha:fade keepalpha noshadow noambient novertexlights nolightmap  nodynlightmap nodirlightmap nofog nometa noforwardadd vertex:vertexDataFunc 
		#undef TRANSFORM_TEX
		#define TRANSFORM_TEX(tex,name) float4(tex.xy * name##_ST.xy + name##_ST.zw, tex.z, tex.w)
		struct Input
		{
			float2 uv_texcoord;
			float4 vertexColor : COLOR;
			float4 uv2_texcoord2;
			float4 screenPos;
		};

		uniform sampler2D _DissolveTex;
		uniform float2 _DissolveTexTiling;
		uniform float _Max;
		uniform float _DistortLevel;
		uniform sampler2D _ColorGradient;
		uniform float4 _Color;
		uniform float _UseMainTex;
		uniform sampler2D _MainTex;
		uniform float2 _MainTexTiling;
		uniform sampler2D _Mask;
		uniform float _OpacityLevel;
		uniform float _NotUseParticleSoftFactor;
		uniform float _SoftParticleFactor;
		UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
		uniform float4 _CameraDepthTexture_TexelSize;

		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float2 appendResult29 = (float2(v.texcoord1.w , 0.0));
			float2 uv_TexCoord30 = v.texcoord.xy * _DissolveTexTiling + appendResult29;
			float4 temp_output_12_0 = ( tex2Dlod( _DissolveTex, float4( uv_TexCoord30, 0, 0.0) ) + ( 1.0 - (1.0 + (v.texcoord1.y - 0.0) * (_Max - 1.0) / (1.0 - 0.0)) ) );
			float3 ase_vertexNormal = v.normal.xyz;
			v.vertex.xyz += ( temp_output_12_0 * _DistortLevel * float4( ase_vertexNormal , 0.0 ) ).rgb;
			v.vertex.w = 1;
		}

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float2 uv_DissolveTex51 = i.uv_texcoord;
			float2 appendResult50 = (float2(tex2D( _DissolveTex, uv_DissolveTex51 ).rg));
			float2 appendResult26 = (float2(i.uv2_texcoord2.x , 0.0));
			float2 uv_TexCoord28 = i.uv_texcoord * _MainTexTiling + appendResult26;
			float4 tex2DNode3 = tex2D( _MainTex, uv_TexCoord28 );
			o.Emission = ( tex2D( _ColorGradient, appendResult50 ) * i.vertexColor * _Color * (( _UseMainTex )?( tex2DNode3 ):( float4( 1,1,1,1 ) )) ).rgb;
			float2 appendResult35 = (float2(i.uv2_texcoord2.z , 0.0));
			float2 uv_TexCoord36 = i.uv_texcoord + appendResult35;
			float2 appendResult29 = (float2(i.uv2_texcoord2.w , 0.0));
			float2 uv_TexCoord30 = i.uv_texcoord * _DissolveTexTiling + appendResult29;
			float4 temp_output_12_0 = ( tex2D( _DissolveTex, uv_TexCoord30 ) + ( 1.0 - (1.0 + (i.uv2_texcoord2.y - 0.0) * (_Max - 1.0) / (1.0 - 0.0)) ) );
			float4 ase_screenPos = float4( i.screenPos.xyz , i.screenPos.w + 0.00000000001 );
			float4 ase_screenPosNorm = ase_screenPos / ase_screenPos.w;
			ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
			float eyeDepth72 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_screenPosNorm.xy ));
			float PSFtoOpacity70 = saturate( ( ( 1.0 - _SoftParticleFactor ) * ( eyeDepth72 - ase_screenPos.w ) * 3.0 ) );
			float4 clampResult80 = clamp( ( ( tex2DNode3 * tex2D( _Mask, uv_TexCoord36 ) ) * (1.0 + (_OpacityLevel - 0.0) * (3.0 - 1.0) / (1.0 - 0.0)) * ceil( temp_output_12_0 ) * i.vertexColor.a * (( _NotUseParticleSoftFactor )?( 1.0 ):( PSFtoOpacity70 )) ) , float4( 0,0,0,0 ) , float4( 1,1,1,1 ) );
			o.Alpha = clampResult80.r;
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18935
-2529;102;1802;1210;1509.48;1295.961;1.3;True;False
Node;AmplifyShaderEditor.TextureCoordinatesNode;25;-1154,-618;Inherit;False;1;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WireNode;55;-961.0337,-488.4838;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;34;-943.684,-537.7474;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;60;-959.9427,-488.4837;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;52;-916.758,-517.4861;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;69;-967.3331,562.3813;Inherit;False;848;433;Particle Soft Factor;8;78;77;76;75;74;73;72;71;Particle Soft Factor;1,1,1,1;0;0
Node;AmplifyShaderEditor.ScreenPosInputsNode;71;-856.3331,783.3812;Float;False;1;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;73;-917.3331,612.3812;Inherit;False;Property;_SoftParticleFactor;Soft Particle Factor;11;0;Create;True;0;0;0;False;0;False;0.5;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenDepthNode;72;-855.3331,698.3812;Inherit;False;0;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;33;-942.684,-537.7474;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;56;-965.3069,103.7348;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;53;-915.6674,-516.304;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;75;-611.333,617.3812;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;61;-917.9451,-330.6592;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;31;-942.684,251.2526;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;74;-603.3331,724.3812;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;59;-965.3073,103.4626;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;76;-604.3331,841.3812;Inherit;False;Constant;_3;3;3;0;Create;True;0;0;0;False;0;False;3;3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;44;-658.1841,314.2531;Inherit;False;Property;_Max;Max;9;0;Create;True;0;0;0;False;0;False;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;29;-828.3757,73.94907;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;68;-881.2811,-76.68346;Inherit;False;Property;_DissolveTexTiling;DissolveTexTiling;5;0;Create;True;0;0;0;False;0;False;1,1;1,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.WireNode;32;-941.684,253.2526;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;54;-917.8489,-330.2018;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;77;-428.3332,699.3812;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;78;-284.3333,700.3812;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;35;-876.6223,-358.7861;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;67;-922.1702,-743.4832;Inherit;False;Property;_MainTexTiling;MainTexTiling;1;0;Create;True;0;0;0;False;0;False;1,1;1,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.DynamicAppendNode;26;-884,-595;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TFHCRemapNode;5;-488,224;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;1;False;4;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;30;-658,27;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;36;-698.775,-407.8371;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;9;-284,223;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;10;-424,-1;Inherit;True;Property;_DissolveTex;DissolveTex;4;1;[NoScaleOffset];Create;True;0;0;0;False;0;False;-1;None;9ab59a8b39b245b46bfa75676ff2ddd3;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;70;-71.08308,696.6312;Inherit;False;PSFtoOpacity;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;28;-703,-643;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;4;-609.2209,-234.3053;Float;False;Property;_OpacityLevel;Opacity Level;8;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;12;-118,4;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;79;-151.6234,-120.5209;Inherit;False;70;PSFtoOpacity;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;2;-446,-437;Inherit;True;Property;_Mask;Mask;7;1;[NoScaleOffset];Create;True;0;0;0;False;0;False;-1;None;7c885849ffaf0cc4bb8830ef6b62f00f;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;3;-448,-672;Inherit;True;Property;_MainTex;MainTex;0;1;[NoScaleOffset];Create;True;0;0;0;False;0;False;-1;None;833b8b99b56cfbe42badd918dc5f270d;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;51;-798.4429,-1218.974;Inherit;True;Property;_TextureSample0;Texture Sample 0;4;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;10;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.VertexColorNode;45;-202.0586,-1047.287;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;6;-126,-456;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.CeilOpNode;43;86.81592,4.253113;Inherit;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.DynamicAppendNode;50;-495.1523,-1214.612;Inherit;False;FLOAT2;4;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TFHCRemapNode;8;-330.221,-229.3053;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;1;False;4;FLOAT;3;False;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode;81;36.47107,-121.2063;Inherit;False;Property;_NotUseParticleSoftFactor;NotUseParticleSoftFactor;12;0;Create;True;0;0;0;False;0;False;0;True;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;49;-333.4575,-1243.421;Inherit;True;Property;_ColorGradient;ColorGradient;10;1;[NoScaleOffset];Create;True;0;0;0;False;0;False;-1;None;ee2c1625f8801df4ea984bc0de5e5f6e;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;65;-204.4196,-876.6398;Inherit;False;Property;_Color;Color;3;1;[HDR];Create;True;0;0;0;False;0;False;0,0,0,0;5.007197,2.908268,2.810644,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;13;350.6999,-332.7366;Inherit;False;5;5;0;COLOR;0,0,0,0;False;1;FLOAT;1;False;2;COLOR;0,0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.NormalVertexDataNode;64;-33.02534,404.5043;Inherit;False;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ToggleSwitchNode;66;-131.5154,-696.9458;Inherit;False;Property;_UseMainTex;UseMainTex;2;0;Create;True;0;0;0;False;0;False;1;True;2;0;COLOR;1,1,1,1;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;63;-122.0699,312.2894;Inherit;False;Property;_DistortLevel;DistortLevel;6;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;11;132.4126,-762.7317;Inherit;True;4;4;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ClampOpNode;80;491.203,-329.8914;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;1,1,1,1;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;62;195.9301,291.2894;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;FLOAT3;0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;687.8673,-522.051;Float;False;True;-1;2;ASEMaterialInspector;0;0;Unlit;MoveGames/ParticleCustom_TR;False;False;False;False;True;True;True;True;True;True;True;True;False;False;True;False;False;False;False;False;False;Off;1;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Transparent;0;True;False;0;False;Transparent;;Transparent;All;14;d3d9;d3d11_9x;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;ps4;psp2;n3ds;wiiu;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;False;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;55;0;25;4
WireConnection;34;0;25;2
WireConnection;60;0;55;0
WireConnection;52;0;25;3
WireConnection;33;0;34;0
WireConnection;56;0;60;0
WireConnection;53;0;52;0
WireConnection;75;0;73;0
WireConnection;61;0;53;0
WireConnection;31;0;33;0
WireConnection;74;0;72;0
WireConnection;74;1;71;4
WireConnection;59;0;56;0
WireConnection;29;0;59;0
WireConnection;32;0;31;0
WireConnection;54;0;61;0
WireConnection;77;0;75;0
WireConnection;77;1;74;0
WireConnection;77;2;76;0
WireConnection;78;0;77;0
WireConnection;35;0;54;0
WireConnection;26;0;25;1
WireConnection;5;0;32;0
WireConnection;5;4;44;0
WireConnection;30;0;68;0
WireConnection;30;1;29;0
WireConnection;36;1;35;0
WireConnection;9;0;5;0
WireConnection;10;1;30;0
WireConnection;70;0;78;0
WireConnection;28;0;67;0
WireConnection;28;1;26;0
WireConnection;12;0;10;0
WireConnection;12;1;9;0
WireConnection;2;1;36;0
WireConnection;3;1;28;0
WireConnection;6;0;3;0
WireConnection;6;1;2;0
WireConnection;43;0;12;0
WireConnection;50;0;51;0
WireConnection;8;0;4;0
WireConnection;81;0;79;0
WireConnection;49;1;50;0
WireConnection;13;0;6;0
WireConnection;13;1;8;0
WireConnection;13;2;43;0
WireConnection;13;3;45;4
WireConnection;13;4;81;0
WireConnection;66;1;3;0
WireConnection;11;0;49;0
WireConnection;11;1;45;0
WireConnection;11;2;65;0
WireConnection;11;3;66;0
WireConnection;80;0;13;0
WireConnection;62;0;12;0
WireConnection;62;1;63;0
WireConnection;62;2;64;0
WireConnection;0;2;11;0
WireConnection;0;9;80;0
WireConnection;0;11;62;0
ASEEND*/
//CHKSM=34FC487D334C20F419F5EAD0606BC79990B93875