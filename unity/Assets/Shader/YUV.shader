Shader "Camerafy/YUV"
{
	Properties
	{
		_MainTex("Texture", 2D) = "black" {}
	}


	CGINCLUDE
	#include "UnityCG.cginc"

	sampler2D	_MainTex;
	float4		_MainTex_ST;

	struct appdata
	{
		float4 vertex	: POSITION;
		float2 uv		: TEXCOORD0;
	};

	//////////////////////////////////////////////////////////////////////////////
	// Vertex shader output
	//////////////////////////////////////////////////////////////////////////////

	struct VertexOutput
	{
		float2 uv		: TEXCOORD0;
		float4 vertex	: SV_POSITION;
	};

	//////////////////////////////////////////////////////////////////////////////
	// Fragment shader outputs
	//////////////////////////////////////////////////////////////////////////////

	struct YOutput
	{
		fixed Y			: SV_Target0;
	};

	struct UVOutput
	{
		fixed U			: SV_Target0;
		fixed V			: SV_Target1;
	};	
	
	//////////////////////////////////////////////////////////////////////////////
	// Vertex shader
	//////////////////////////////////////////////////////////////////////////////

	VertexOutput YUVVertShader(appdata i)
	{
		VertexOutput o;
	
		o.vertex = UnityObjectToClipPos(i.vertex);
		o.uv = TRANSFORM_TEX(1 - i.uv, _MainTex);
	
		return o;
	}

	//////////////////////////////////////////////////////////////////////////////
	// Y - Channel fragment shader
	//////////////////////////////////////////////////////////////////////////////

	YOutput YFragShader(VertexOutput i)
	{
		YOutput o;

		// Sample the source texture.
		half3 rgb_y = tex2D(_MainTex, i.uv).rgb;

#if !UNITY_COLORSPACE_GAMMA
		rgb_y = LinearToGammaSpace(rgb_y);
#endif

		const half3 kY = half3(0.29900, 0.58700, 0.11400);
		 
		// convert rgb to y (luminance)
		o.Y = dot(kY, rgb_y);
		return o;
	}

	//////////////////////////////////////////////////////////////////////////////
	// U, V - Channel fragment shader
	//////////////////////////////////////////////////////////////////////////////

	UVOutput UVFragShader(VertexOutput i)
	{
		UVOutput o;

		// Sample the source texture.
		half3 rgb_uv = tex2D(_MainTex, i.uv).rgb;

#if !UNITY_COLORSPACE_GAMMA
		rgb_uv = LinearToGammaSpace(rgb_y);
#endif

		const half3 kU = half3(-0.14713, -0.28886,  0.43600);
		const half3 kV = half3( 0.61500, -0.51499, -0.10001);

		// convert rgb to u and v (chroma)
		o.U = dot(kU, rgb_uv) + 0.5;
		o.V = dot(kV, rgb_uv) + 0.5;

		return o;
	}

	ENDCG


	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		// first pass - Y channel
		Pass
		{
			CGPROGRAM
			#pragma multi_compile _ UNITY_COLORSPACE_GAMMA
			#pragma vertex YUVVertShader
			#pragma fragment YFragShader
			ENDCG
		}

		// second pass - U and V channel
		Pass
		{
			CGPROGRAM
			#pragma multi_compile _ UNITY_COLORSPACE_GAMMA
			#pragma vertex YUVVertShader
			#pragma fragment UVFragShader
			ENDCG
		}
	}
}