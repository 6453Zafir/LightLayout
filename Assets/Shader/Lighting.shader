// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Warsoup/Lighting" {
	Properties{
		_Color("Base Color", Color) = (1,1,1,0.5)
		DiffuseTexture("Diffuse Texture", 2D) = "white" { }
	_LightMap("LightMap", 2D) = "white" {}
	}
		SubShader{
		Pass{

		CGPROGRAM
// Upgrade NOTE: excluded shader from DX11; has structs without semantics (struct v2f members uv1,uv2)
#pragma exclude_renderers d3d11
#pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"

		float4      _Color;
	sampler2D   DiffuseTexture;
	sampler2D   _LightMap;

	float4 DiffuseTexture_ST;
	float4 _LightMap_ST;

	struct v2f {
		float4  pos : SV_POSITION;
		float2  uv1;
		float2  uv2;
	};

	struct InputVS {
		float4 vertex : POSITION;
		float4 tangent : TANGENT;
		float3 normal : NORMAL;
		float4 texcoord : TEXCOORD0;
		float4 texcoord1 : TEXCOORD1;
	};

	v2f vert(InputVS v)
	{
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv1 = TRANSFORM_TEX(v.texcoord, DiffuseTexture);
		o.uv2 = TRANSFORM_TEX(v.texcoord1, _LightMap);
		return o;
	}

	half4 frag(v2f i) : COLOR
	{
		half4 texcol = tex2D(DiffuseTexture, i.uv1);
		half4 lightmap = tex2D(_LightMap, i.uv2);
		return texcol * lightmap;
	}
		ENDCG

	}
	}
		Fallback "VertexLit"
}