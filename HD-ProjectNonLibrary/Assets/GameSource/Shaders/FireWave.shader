// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "MoveGames/Effect/FireWave"
{
	Properties
	{
		_FlowTex("FlowTex", 2D) = "white" {}
		[HDR]_Color("Color", Color) = (1,1,1,1)
		_FlowTIle("Flow TIle", Vector) = (1,1,0,0)
		_Bias("Bias", Float) = 1
		_Scale("Scale", Float) = 2
		_Power("Power", Float) = 3
		_AddColor("AddColor", Range( 0 , 1)) = 0
		[Toggle]_Use2ndFlow("Use2ndFlow", Float) = 1
		_FlowTIle2("Flow TIle 2", Vector) = (1,1,0,0)
		[HDR]_2ndColor("2ndColor", Color) = (0,0,0,0)
		_MaskTex("MaskTex", 2D) = "white" {}
		_OffsetPower("OffsetPower", Float) = 0
		[Toggle(_CUSTOMOFFSET_ON)] _CustomOffset("CustomOffset", Float) = 0
		[HideInInspector] _texcoord2( "", 2D ) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Off
		CGINCLUDE
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		#pragma shader_feature_local _CUSTOMOFFSET_ON
		#undef TRANSFORM_TEX
		#define TRANSFORM_TEX(tex,name) float4(tex.xy * name##_ST.xy + name##_ST.zw, tex.z, tex.w)
		struct Input
		{
			float4 vertexColor : COLOR;
			float3 uv2_texcoord2;
			float2 uv_texcoord;
			float3 worldPos;
			float3 worldNormal;
		};

		uniform sampler2D _FlowTex;
		uniform float2 _FlowTIle;
		uniform float _OffsetPower;
		uniform sampler2D _MaskTex;
		uniform float4 _MaskTex_ST;
		uniform float4 _Color;
		uniform float4 _2ndColor;
		uniform float _Use2ndFlow;
		uniform float2 _FlowTIle2;
		uniform float _Bias;
		uniform float _Scale;
		uniform float _Power;
		uniform float _AddColor;

		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float2 appendResult41 = (float2(0.0 , v.texcoord1.xyz.x));
			float2 uv_TexCoord38 = v.texcoord.xy * _FlowTIle + appendResult41;
			float4 tex2DNode5 = tex2Dlod( _FlowTex, float4( uv_TexCoord38, 0, 0.0) );
			float3 ase_vertexNormal = v.normal.xyz;
			#ifdef _CUSTOMOFFSET_ON
				float staticSwitch43 = v.texcoord1.xyz.z;
			#else
				float staticSwitch43 = _OffsetPower;
			#endif
			float2 uv_MaskTex = v.texcoord * _MaskTex_ST.xy + _MaskTex_ST.zw;
			float4 tex2DNode33 = tex2Dlod( _MaskTex, float4( uv_MaskTex, 0, 0.0) );
			v.vertex.xyz += ( tex2DNode5 * float4( ase_vertexNormal , 0.0 ) * staticSwitch43 * tex2DNode33 ).rgb;
			v.vertex.w = 1;
		}

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float2 appendResult42 = (float2(0.0 , i.uv2_texcoord2.y));
			float2 uv_TexCoord39 = i.uv_texcoord * _FlowTIle2 + appendResult42;
			float4 lerpResult31 = lerp( _Color , _2ndColor , (( _Use2ndFlow )?( tex2D( _FlowTex, uv_TexCoord39 ) ):( float4( 0,0,0,1 ) )));
			float3 ase_worldPos = i.worldPos;
			float3 ase_worldViewDir = normalize( UnityWorldSpaceViewDir( ase_worldPos ) );
			float3 ase_worldNormal = i.worldNormal;
			float fresnelNdotV1 = dot( ase_worldNormal, ase_worldViewDir );
			float fresnelNode1 = ( _Bias + _Scale * pow( 1.0 - fresnelNdotV1, _Power ) );
			o.Emission = ( i.vertexColor * lerpResult31 * fresnelNode1 ).rgb;
			float2 appendResult41 = (float2(0.0 , i.uv2_texcoord2.x));
			float2 uv_TexCoord38 = i.uv_texcoord * _FlowTIle + appendResult41;
			float4 tex2DNode5 = tex2D( _FlowTex, uv_TexCoord38 );
			float2 uv_MaskTex = i.uv_texcoord * _MaskTex_ST.xy + _MaskTex_ST.zw;
			float4 tex2DNode33 = tex2D( _MaskTex, uv_MaskTex );
			o.Alpha = ( ( ( i.vertexColor.a * fresnelNode1 * tex2DNode5 ) + _AddColor + (( _Use2ndFlow )?( tex2D( _FlowTex, uv_TexCoord39 ) ):( float4( 0,0,0,1 ) )) ) * tex2DNode33 * i.vertexColor.a ).r;
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf Unlit alpha:fade keepalpha fullforwardshadows vertex:vertexDataFunc 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile UNITY_PASS_SHADOWCASTER
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#include "HLSLSupport.cginc"
			#if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
				#define CAN_SKIP_VPOS
			#endif
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			sampler3D _DitherMaskLOD;
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float3 customPack1 : TEXCOORD1;
				float2 customPack2 : TEXCOORD2;
				float3 worldPos : TEXCOORD3;
				float3 worldNormal : TEXCOORD4;
				half4 color : COLOR0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				Input customInputData;
				vertexDataFunc( v, customInputData );
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				o.worldNormal = worldNormal;
				o.customPack1.xyz = customInputData.uv2_texcoord2;
				o.customPack1.xyz = v.texcoord1;
				o.customPack2.xy = customInputData.uv_texcoord;
				o.customPack2.xy = v.texcoord;
				o.worldPos = worldPos;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				o.color = v.color;
				return o;
			}
			half4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				surfIN.uv2_texcoord2 = IN.customPack1.xyz;
				surfIN.uv_texcoord = IN.customPack2.xy;
				float3 worldPos = IN.worldPos;
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.worldPos = worldPos;
				surfIN.worldNormal = IN.worldNormal;
				surfIN.vertexColor = IN.color;
				SurfaceOutput o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutput, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				half alphaRef = tex3D( _DitherMaskLOD, float3( vpos.xy * 0.25, o.Alpha * 0.9375 ) ).a;
				clip( alphaRef - 0.01 );
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18935
-2529;102;1802;1210;2281.718;571.8318;1.763524;True;False
Node;AmplifyShaderEditor.TexCoordVertexDataNode;40;-1425.374,458.3637;Inherit;False;1;3;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;8;-1197.901,222.2496;Inherit;False;Property;_FlowTIle;Flow TIle;2;0;Create;True;0;0;0;False;0;False;1,1;3,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.DynamicAppendNode;42;-1176.436,714.9125;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;20;-1193.966,565.2421;Inherit;False;Property;_FlowTIle2;Flow TIle 2;8;0;Create;True;0;0;0;False;0;False;1,1;6,2;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.DynamicAppendNode;41;-1189.48,351.831;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;14;-977,-84;Inherit;False;Property;_Bias;Bias;3;0;Create;True;0;0;0;False;0;False;1;1.21;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;15;-978,11;Inherit;False;Property;_Scale;Scale;4;0;Create;True;0;0;0;False;0;False;2;0.67;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;39;-1007.938,667.0811;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;16;-978,99;Inherit;False;Property;_Power;Power;5;0;Create;True;0;0;0;False;0;False;3;1.57;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;38;-1035.116,304;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.VertexColorNode;4;-636,-356;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FresnelNode;1;-775,50;Inherit;True;Standard;WorldNormal;ViewDir;False;False;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0.05;False;2;FLOAT;1.73;False;3;FLOAT;2.89;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;5;-774,275;Inherit;True;Property;_FlowTex;FlowTex;0;0;Create;True;0;0;0;False;0;False;-1;None;3584f2bf4afb5284d91edb6a29126e62;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;19;-784.4129,638;Inherit;True;Property;_TextureSample0;Texture Sample 0;0;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;5;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ToggleSwitchNode;25;-482.5,614;Inherit;False;Property;_Use2ndFlow;Use2ndFlow;7;0;Create;True;0;0;0;False;0;False;1;True;2;0;COLOR;0,0,0,1;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;37;-391.8089,1106.449;Inherit;False;Property;_OffsetPower;OffsetPower;11;0;Create;True;0;0;0;False;0;False;0;0.14;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;2;-688,-138;Inherit;False;Property;_Color;Color;1;1;[HDR];Create;True;0;0;0;False;0;False;1,1,1,1;1.498039,0.2972828,0.1098039,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;18;-392.5,-9;Inherit;False;Property;_AddColor;AddColor;6;0;Create;True;0;0;0;False;0;False;0;0.299;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;13;-313,148;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;32;-458.5,753;Inherit;False;Property;_2ndColor;2ndColor;9;1;[HDR];Create;True;0;0;0;False;0;False;0,0,0,0;2.433572,1.31473,0.4476855,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;17;-129.5,148;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;31;-180.5,572;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;33;-292.5,300;Inherit;True;Property;_MaskTex;MaskTex;10;0;Create;True;0;0;0;False;0;False;-1;None;5b66e25d75ad8bc47a270f48df3ae17d;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StaticSwitch;43;-206.218,1107.799;Inherit;False;Property;_CustomOffset;CustomOffset;12;0;Create;True;0;0;0;False;0;False;0;0;1;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NormalVertexDataNode;35;-150.8897,945.9759;Inherit;False;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;34;58.5,149;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;COLOR;1,1,1,1;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;3;10,-2;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;36;116.8742,923.7397;Inherit;False;4;4;0;COLOR;0,0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;3;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;235,-57;Float;False;True;-1;2;ASEMaterialInspector;0;0;Unlit;MoveGames/Effect/FireWave;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Off;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Transparent;0.5;True;True;0;False;Transparent;;Transparent;All;18;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;42;1;40;2
WireConnection;41;1;40;1
WireConnection;39;0;20;0
WireConnection;39;1;42;0
WireConnection;38;0;8;0
WireConnection;38;1;41;0
WireConnection;1;1;14;0
WireConnection;1;2;15;0
WireConnection;1;3;16;0
WireConnection;5;1;38;0
WireConnection;19;1;39;0
WireConnection;25;1;19;0
WireConnection;13;0;4;4
WireConnection;13;1;1;0
WireConnection;13;2;5;0
WireConnection;17;0;13;0
WireConnection;17;1;18;0
WireConnection;17;2;25;0
WireConnection;31;0;2;0
WireConnection;31;1;32;0
WireConnection;31;2;25;0
WireConnection;43;1;37;0
WireConnection;43;0;40;3
WireConnection;34;0;17;0
WireConnection;34;1;33;0
WireConnection;34;2;4;4
WireConnection;3;0;4;0
WireConnection;3;1;31;0
WireConnection;3;2;1;0
WireConnection;36;0;5;0
WireConnection;36;1;35;0
WireConnection;36;2;43;0
WireConnection;36;3;33;0
WireConnection;0;2;3;0
WireConnection;0;9;34;0
WireConnection;0;11;36;0
ASEEND*/
//CHKSM=49A475022792FF11A06FCCB0852ABAF4F8311135