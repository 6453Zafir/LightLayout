//KlayGE的做法：将点转到camera为原点的坐标系计算距离，但是unity有问题不能这么算：
//TM这个RenderCubeMap有坑：绘制出来的CubeMap之和Camera的位置有关，
//不能通过将点转到Camera的MV里面提取坐标！

//简单的在世界坐标系里面计算两点的距离就好了
Shader "VirtualPolygonLights/DepthCubeMap"
{
	SubShader
	{
		Pass
		{
			Cull Off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
#pragma enable_d3d11_debug_symbols

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;		
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 worldPos : TEXCOORD0;
				float3 worldNormal: TEXCOORD1;
			};	

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);

				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.worldNormal = mul(unity_ObjectToWorld, v.normal);
				
				return o;
			}


			float2 _FarPlane;
			float _SlopeFactor;
			float _Bias;
			float frag(v2f i) : SV_Target
			{
				float dist = distance(_WorldSpaceCameraPos, i.worldPos);
				float vec = i.worldNormal - _WorldSpaceCameraPos;
				//根据斜率计算bias
				float3 biasedPos = i.worldPos + _SlopeFactor * i.worldNormal;
				float biasedDist = distance(_WorldSpaceCameraPos, biasedPos);
				//float cosTheta = dot(i.worldNormal, normalize(-vec));
				//cosTheta = saturate(cosTheta);
				//float slopeBias = 0.00001 * tan(acos(cosTheta));//0.03

				return (biasedDist) *_FarPlane.y + _Bias;
			}
			ENDCG
		}
	}
}
