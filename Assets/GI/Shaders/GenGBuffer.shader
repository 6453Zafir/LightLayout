// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

//shadertype=unity
Shader "LightMapper/UseUnityLm/GenGBuffer"
{
	//子着色器    
	SubShader
	{
		Pass
		{
			Cull Off
			ZWrite Off
			CGPROGRAM

			//定义函数  
			#pragma vertex vert  
			#pragma fragment frag  
			#pragma geometry geo
			#pragma multi_compile COLLECT_TRI_GEO COLLECT_LINE_GEO COLLECT_POINT_GEO
			#include "UnityCG.cginc"
			
			//定义结构体：应用阶段到vertex shader阶段的数据  
			struct a2v
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 uv2	: TEXCOORD1;
			};

			//定义结构体：vertex shader阶段输出的内容  
			struct v2g
			{
				float4 pos : SV_POSITION;
				float4 clipPos : TEXCOORD0;
				float3 worldNormal: TEXCOORD1;
			};
			struct g2f
			{
				float4 pos : POSITION;
				float4 clipPos : TEXCOORD0;
				float3 worldNormal : TEXCOORD1;
			};
			struct PixelOutput 
			{
				float4 col0 : COLOR0;
				float4 col1 : COLOR1;
			};
			//顶点shader  
			v2g vert(a2v v)
			{
				v2g o;

				//sted uv2
				//unity_LightmapST: xy scale zw translate
				float2 uv2 = v.uv2.xy * unity_LightmapST.xy + unity_LightmapST.zw;
				o.pos = float4(uv2.x * 2 - 1, uv2.y * -2 + 1, 0, 1);
				//o.pos = UnityObjectToClipPos(v.vertex);

				o.worldNormal = mul(float4(v.normal,1), unity_WorldToObject).xyz;
				//o.worldNormal = mul(unity_ObjectToWorld, float4(v.normal,1)).xyz;  
				//裁剪空间是1/w插值
				o.clipPos = UnityObjectToClipPos(v.vertex);  
				//顶点位置转化到世界空间   

				return o;
			}
			
#ifdef COLLECT_TRI_GEO
			[maxvertexcount(3)]
			void geo(triangle v2g p[3], inout TriangleStream<g2f> triStream)
			{
				for (int i = 0; i < 3; i++)
				{
					g2f f;
					f.pos = p[i].pos;
					f.clipPos = p[i].clipPos;
					f.worldNormal = p[i].worldNormal;
					triStream.Append(f);
				}
				triStream.RestartStrip();
			}
#endif
#ifdef COLLECT_LINE_GEO
			[maxvertexcount(2)]
			void geo(line v2g p[2], inout LineStream<g2f> lineStream)
			{
					g2f f;
					f.pos = p[0].pos;
					f.clipPos = p[0].clipPos;
					f.worldNormal = p[0].worldNormal;
					lineStream.Append(f);
					g2f f1;
					f1.pos = p[1].pos;
					f1.clipPos = p[1].clipPos;
					f1.worldNormal = p[1].worldNormal;
					lineStream.Append(f1);
					lineStream.RestartStrip();				
			}
#endif
#ifdef COLLECT_POINT_GEO
			[maxvertexcount(1)]
			void geo(point v2g p[1], inout PointStream<g2f> pointStream)
			{
					g2f f;
					f.pos = p[0].pos;
					f.clipPos = p[0].clipPos;
					f.worldNormal = p[0].worldNormal;
					pointStream.Append(f);
					pointStream.RestartStrip();				
			}
#endif

			//片元shader  
			PixelOutput frag(g2f i)
			{
				float4 output;
				//将normal 单位化 简单的(normal + 1)* 0.5
				//只有深度信息不足以还原出世界坐标
				//因为贴图的uv信息不足以提供xy坐标信息，
				//所以还是得使用mrt搞出两个贴图
				float3 nor = (normalize(i.worldNormal.xyz) + float3(1,1,1)) * float3(0.5,0.5,0.5);

				//转换到单位空间
				float3 ndcPos = i.clipPos.xyz/i.clipPos.w;
				float3 pos = (ndcPos + float3(1,1,1)) * float3(0.5,0.5,0.5);

				//MRT
				//output0: pos
				//output1: normal

				PixelOutput o;

				//depth
				o.col0=float4(pos.xyz, 1);
				//normal
				o.col1=float4(nor.xyz, 1);
				return o;

			}
			ENDCG
		}	
	}
}