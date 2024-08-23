// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "ASESampleShaders/Community/TFHC/Hologram Simple"
{
	Properties
	{
		_Hologramcolor("Hologram color", Color) = (0.3973832,0.7720588,0.7410512,0)
		_Speed("Speed", Range( 0 , 100)) = 26
		_ScanLines("Scan Lines", Range( 0 , 10)) = 3
		_Opacity("Opacity", Range( 0 , 1)) = 0.5
		_RimNormalMap("Rim Normal Map", 2D) = "bump" {}
		_RimPower("Rim Power", Range( 0 , 10)) = 5
		_Intensity("Intensity", Range( 1 , 10)) = 1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Back
		CGINCLUDE
		#include "UnityShaderVariables.cginc"
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		#ifdef UNITY_PASS_SHADOWCASTER
			#undef INTERNAL_DATA
			#undef WorldReflectionVector
			#undef WorldNormalVector
			#define INTERNAL_DATA half3 internalSurfaceTtoW0; half3 internalSurfaceTtoW1; half3 internalSurfaceTtoW2;
			#define WorldReflectionVector(data,normal) reflect (data.worldRefl, half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal)))
			#define WorldNormalVector(data,normal) half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal))
		#endif
		struct Input
		{
			float3 worldPos;
			float2 uv_texcoord;
			float3 viewDir;
			INTERNAL_DATA
		};

		uniform float4 _Hologramcolor;
		uniform float _ScanLines;
		uniform float _Speed;
		uniform sampler2D _RimNormalMap;
		uniform float _RimPower;
		uniform float _Intensity;
		uniform float _Opacity;


		float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }

		float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }

		float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }

		float snoise( float2 v )
		{
			const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
			float2 i = floor( v + dot( v, C.yy ) );
			float2 x0 = v - i + dot( i, C.xx );
			float2 i1;
			i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
			float4 x12 = x0.xyxy + C.xxzz;
			x12.xy -= i1;
			i = mod2D289( i );
			float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
			float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
			m = m * m;
			m = m * m;
			float3 x = 2.0 * frac( p * C.www ) - 1.0;
			float3 h = abs( x ) - 0.5;
			float3 ox = floor( x + 0.5 );
			float3 a0 = x - ox;
			m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
			float3 g;
			g.x = a0.x * x0.x + h.x * x0.y;
			g.yz = a0.yz * x12.xz + h.yz * x12.yw;
			return 130.0 * dot( m, g );
		}


		void surf( Input i , inout SurfaceOutputStandard o )
		{
			o.Normal = float3(0,0,1);
			float4 HologramColor32 = _Hologramcolor;
			float3 ase_worldPos = i.worldPos;
			float Speed156 = _Speed;
			float temp_output_13_0 = sin( ( ( ( _ScanLines * ase_worldPos.y ) + (( 1.0 - ( Speed156 * _Time ) )).x ) * UNITY_PI ) );
			float clampResult15 = clamp( (0.0 + (temp_output_13_0 - -1.0) * (1.0 - 0.0) / (1.0 - -1.0)) , 0.0 , 1.0 );
			float4 lerpResult16 = lerp( float4(1,1,1,0) , float4(0,0,0,0) , clampResult15);
			float2 temp_cast_0 = (( ( ase_worldPos.z / 100.0 ) * _Time.x )).xx;
			float simplePerlin2D137 = snoise( temp_cast_0 );
			float myVarName3146 = ( simplePerlin2D137 * temp_output_13_0 );
			float4 temp_cast_1 = (myVarName3146).xxxx;
			float ScanLines30 = ( lerpResult16 - temp_cast_1 );
			float3 normalizeResult57 = normalize( i.viewDir );
			float dotResult55 = dot( UnpackNormal( tex2D( _RimNormalMap, ( ( ( Speed156 / 1000.0 ) * _Time ) + float4( i.uv_texcoord, 0.0 , 0.0 ) ).xy ) ) , normalizeResult57 );
			float temp_output_60_0 = pow( ( 1.0 - saturate( dotResult55 ) ) , ( 10.0 - _RimPower ) );
			float Rim65 = temp_output_60_0;
			o.Emission = ( ( HologramColor32 * ( ScanLines30 + Rim65 ) ) * _Intensity ).rgb;
			o.Alpha = _Opacity;
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf Standard alpha:fade keepalpha fullforwardshadows 

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
				float2 customPack1 : TEXCOORD1;
				float4 tSpace0 : TEXCOORD2;
				float4 tSpace1 : TEXCOORD3;
				float4 tSpace2 : TEXCOORD4;
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
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				half3 worldTangent = UnityObjectToWorldDir( v.tangent.xyz );
				half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				half3 worldBinormal = cross( worldNormal, worldTangent ) * tangentSign;
				o.tSpace0 = float4( worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x );
				o.tSpace1 = float4( worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y );
				o.tSpace2 = float4( worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z );
				o.customPack1.xy = customInputData.uv_texcoord;
				o.customPack1.xy = v.texcoord;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
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
				surfIN.uv_texcoord = IN.customPack1.xy;
				float3 worldPos = float3( IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w );
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.viewDir = IN.tSpace0.xyz * worldViewDir.x + IN.tSpace1.xyz * worldViewDir.y + IN.tSpace2.xyz * worldViewDir.z;
				surfIN.worldPos = worldPos;
				surfIN.internalSurfaceTtoW0 = IN.tSpace0.xyz;
				surfIN.internalSurfaceTtoW1 = IN.tSpace1.xyz;
				surfIN.internalSurfaceTtoW2 = IN.tSpace2.xyz;
				SurfaceOutputStandard o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputStandard, o )
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
Version=17800
-61;407;1716;458;3010.505;-585.2124;1.002488;True;False
Node;AmplifyShaderEditor.CommentaryNode;168;-1574.711,-440.4086;Inherit;False;614.0698;167.2261;Comment;2;6;156;Speed;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;6;-1532.711,-366.1825;Float;False;Property;_Speed;Speed;1;0;Create;True;0;0;False;0;26;64;0;100;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;156;-1194.641,-390.4085;Float;False;Speed;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;170;-3047.519,563.0162;Inherit;False;2377.06;920.5361;Comment;26;26;157;27;10;8;2;106;105;107;3;144;11;143;150;13;145;137;14;18;15;149;17;16;146;155;30;Scan Lines;1,1,1,1;0;0
Node;AmplifyShaderEditor.TimeNode;26;-2997.519,1281.553;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;157;-2993.246,1177.753;Inherit;False;156;Speed;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;169;-3022.51,-122.5578;Inherit;False;2344.672;617.4507;Comment;18;58;57;119;55;63;62;64;68;60;65;59;66;163;158;162;167;166;165;Rim;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;27;-2784.481,1237.729;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode;162;-2972.51,-72.55784;Inherit;False;156;Speed;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;8;-2611.384,1214.243;Inherit;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;10;-2792.819,926.6287;Float;False;Property;_ScanLines;Scan Lines;2;0;Create;True;0;0;False;0;3;6.8;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;2;-2785.312,1006.975;Float;True;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;106;-2491.944,947.3199;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;6.06;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;167;-2790.792,-71.13026;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;1000;False;1;FLOAT;0
Node;AmplifyShaderEditor.TimeNode;165;-2968.947,36.28125;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ComponentMaskNode;105;-2434.701,1217.699;Inherit;False;True;False;False;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;144;-2686.636,589.6163;Float;True;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.TextureCoordinatesNode;158;-2763.859,253.5992;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;166;-2648.954,-42.64458;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleAddOpNode;3;-2217.673,953.4962;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PiNode;107;-2211.692,1195.72;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;11;-1999.611,1052.728;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;163;-2443.413,-48.21709;Inherit;True;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT2;0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;150;-2427.506,666.0811;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;100;False;1;FLOAT;0
Node;AmplifyShaderEditor.TimeNode;143;-2697.425,757.8511;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;58;-2350.962,175.2965;Float;False;Tangent;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.NormalizeNode;57;-2115.904,207.5353;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SamplerNode;119;-2228.099,-27.17607;Inherit;True;Property;_RimNormalMap;Rim Normal Map;4;0;Create;True;0;0;False;0;-1;5b653e484c8e303439ef414b62f969f0;f7f322ea849ea7d41adb6fa8a7d8a3e6;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SinOpNode;13;-1854.322,1124.004;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;145;-2301.449,695.7789;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;137;-2096.432,679.8153;Inherit;True;Simplex2D;False;False;2;0;FLOAT2;100,100;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;14;-1649.093,1145.662;Inherit;True;5;0;FLOAT;0;False;1;FLOAT;-1;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;55;-1846.138,25.56644;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;149;-1848.084,803.696;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;63;-1660.224,18.50534;Inherit;True;1;0;FLOAT;1.23;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;17;-1632.495,762.2489;Float;False;Constant;_Color0;Color 0;2;0;Create;True;0;0;False;0;1,1,1,0;0,0,0,0;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;62;-2158.147,356.4128;Float;False;Property;_RimPower;Rim Power;5;0;Create;True;0;0;False;0;5;4.57;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;15;-1372.346,1144.221;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;18;-1637.394,933.9765;Float;False;Constant;_Color1;Color 1;2;0;Create;True;0;0;False;0;0,0,0,0;0,0,0,0;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleSubtractOpNode;68;-1742.309,285.1235;Inherit;False;2;0;FLOAT;10;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;64;-1497.314,36.8154;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;146;-1656.203,668.247;Float;False;myVarName3;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;16;-1393.578,824.2415;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;155;-1128.875,868.95;Inherit;True;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.PowerNode;60;-1260.316,200.7159;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;35;-2325.665,-480.406;Inherit;False;590.8936;257.7873;Comment;2;32;28;Hologram Color;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;30;-904.4599,898.7253;Float;False;ScanLines;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;28;-2275.665,-430.406;Float;False;Property;_Hologramcolor;Hologram color;0;0;Create;True;0;0;False;0;0.3973832,0.7720588,0.7410512,0;0.8901961,0,0,0;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;65;-911.8385,239.3701;Float;False;Rim;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;114;-797.5459,-475.9459;Inherit;False;30;ScanLines;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;33;-779.9076,-330.7064;Inherit;False;65;Rim;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;32;-2007.731,-391.2617;Float;False;HologramColor;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;127;-557.7931,-444.785;Inherit;False;32;HologramColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;71;-552.487,-382.0593;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;126;-314.9854,-516.7054;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;132;-569.2174,-135.0388;Float;False;Property;_Intensity;Intensity;6;0;Create;True;0;0;False;0;1;1;1;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;66;-1368.539,379.8929;Inherit;False;32;HologramColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;133;-96.82381,-349.1077;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;49;-317.1664,-56.16708;Float;False;Property;_Opacity;Opacity;3;0;Create;True;0;0;False;0;0.5;0.429;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;59;-1100.416,333.5666;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;89.8217,-401.0934;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;ASESampleShaders/Community/TFHC/Hologram Simple;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Transparent;0.5;True;True;0;False;Transparent;;Transparent;All;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;0;4;10;25;False;0.5;True;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;1;False;-1;1;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;156;0;6;0
WireConnection;27;0;157;0
WireConnection;27;1;26;0
WireConnection;8;0;27;0
WireConnection;106;0;10;0
WireConnection;106;1;2;2
WireConnection;167;0;162;0
WireConnection;105;0;8;0
WireConnection;166;0;167;0
WireConnection;166;1;165;0
WireConnection;3;0;106;0
WireConnection;3;1;105;0
WireConnection;11;0;3;0
WireConnection;11;1;107;0
WireConnection;163;0;166;0
WireConnection;163;1;158;0
WireConnection;150;0;144;3
WireConnection;57;0;58;0
WireConnection;119;1;163;0
WireConnection;13;0;11;0
WireConnection;145;0;150;0
WireConnection;145;1;143;1
WireConnection;137;0;145;0
WireConnection;14;0;13;0
WireConnection;55;0;119;0
WireConnection;55;1;57;0
WireConnection;149;0;137;0
WireConnection;149;1;13;0
WireConnection;63;0;55;0
WireConnection;15;0;14;0
WireConnection;68;1;62;0
WireConnection;64;0;63;0
WireConnection;146;0;149;0
WireConnection;16;0;17;0
WireConnection;16;1;18;0
WireConnection;16;2;15;0
WireConnection;155;0;16;0
WireConnection;155;1;146;0
WireConnection;60;0;64;0
WireConnection;60;1;68;0
WireConnection;30;0;155;0
WireConnection;65;0;60;0
WireConnection;32;0;28;0
WireConnection;71;0;114;0
WireConnection;71;1;33;0
WireConnection;126;0;127;0
WireConnection;126;1;71;0
WireConnection;133;0;126;0
WireConnection;133;1;132;0
WireConnection;59;0;60;0
WireConnection;59;1;66;0
WireConnection;0;2;133;0
WireConnection;0;9;49;0
ASEEND*/
//CHKSM=7B4DD4C8CEC20522DF7F0003C5E309A3434C59EA