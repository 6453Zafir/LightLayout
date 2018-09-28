// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

//shadertype=unity
Shader "LightMapper/ISM"
{
	//子着色器    
	SubShader
	{
		Pass
		{
			Cull Off
		//	ZWrite On
		//	ZTest LEqual
			CGPROGRAM
            #pragma hull HS
            #pragma domain DS
					
			#pragma geometry geo
			#pragma fragment frag
			//定义函数  
			#pragma vertex vert  
            #pragma target 5.0
			#include "./ism_util.cginc"
			#include "./random.cginc"
			#include "UnityCG.cginc"
			#define PI 3.14159265358979
			
			//定义结构体：应用阶段到vertex shader阶段的数据  
			struct a2v
			{
				float4 vertex : POSITION;
                float4 normal : NORMAL;
			};

			//定义结构体：vertex shader阶段输出的内容  

			struct VertexOut
			{
				float4 pos : POSITION;
				//float3 worldPos : TEXCOORD0;
				float3 worldNormal : TEXCOORD1;
			};

            struct PatchTess
            {
                float edge[3]:SV_TESSFACTOR;
                float inside[1] : SV_INSIDETESSFACTOR;
            };

            struct HullOut
            {
                float4 posL : POSITION;
				float3 normal : TEXCOORD0;
            };

            struct DomainOut
            {
                float4 pos : SV_POSITION;
				float3 normal : TEXCOORD0;
            };

        	struct g2f
			{
				float4 pos : POSITION;
				float depth : TEXCOORD0;
             //   float pointSize : PSIZE; in dx11 pointSize always=1
			};
			//顶点shader  
			VertexOut vert(a2v v)
			{
				VertexOut o;

				//sted uv2
				//unity_LightmapST: xy scale zw translate
				//o.pos = v.vertex;
            	o.worldNormal = v.normal;
				//o.pos = mul(unity_ObjectToWorld, v.vertex);
				o.pos = v.vertex;
				return o;
			}

            PatchTess ConstantHS(InputPatch<VertexOut, 3> patch,
                                uint patchID : SV_PRIMITIVEID)
            {
                PatchTess t;
                float length0 = length(patch[1].pos - patch[2].pos);
                float length1 = length(patch[2].pos - patch[0].pos);
                float length2 = length(patch[0].pos - patch[1].pos);
                t.edge[0] = length0 * 3;
                t.edge[1] = length1 * 3;
                t.edge[2] = length2 * 3;

                t.inside[0] = max(max(t.edge[0], t.edge[1]), t.edge[2]);
                return t;
            }

            [domain("tri")]
            [partitioning("fractional_odd")]
            [outputtopology("triangle_cw")]
            [outputcontrolpoints(3)]
            [patchconstantfunc("ConstantHS")]
            [maxtessfactor(64.0f)]
            HullOut HS(InputPatch<VertexOut, 3> p, 
                       uint i : SV_OUTPUTCONTROLPOINTID,
                       uint patchId : SV_PRIMITIVEID)
            {
                HullOut hout;
                hout.posL = p[i].pos;
                hout.posL.w = 1;
				hout.normal = p[i].worldNormal;
                return hout;
            } 

            float3 interpolate3D(float3 bary, float3 v0, float3 v1, float3 v2)
            {
                return bary.x * v0 + bary.y * v1 + bary.z * v2;
            }

            [domain("tri")]
            DomainOut DS(PatchTess patchTess,
                        float3 bary : SV_DOMAINLOCATION,
                        const OutputPatch<HullOut, 3> tri)
            {
                DomainOut dout;
                dout.pos.xyz = interpolate3D(bary,
                        tri[0].posL.xyz,
                        tri[1].posL.xyz,
                        tri[2].posL.xyz
                    );
                dout.pos.w = 1.0;
				dout.pos = mul(unity_ObjectToWorld, dout.pos);
				dout.normal = mul(unity_ObjectToWorld,tri[0].normal);
                return dout;
            }
			int _IsmCount;
            float4 _VplNormal[1024];
            float4 _VplPosition[1024];
            float2 _ZFar;
			int _ShadowMapSize;
			g2f createVertex(float2 uv, float depth)
			{
				g2f f;
				uv = uv *2.0 - 1.0;

				f.pos.xy = uv;
				f.pos.z = depth; 
				f.pos.w = 1;
				f.depth = depth;
				return f;
			}

			uint wang_hash(uint seed)
			{
				seed = (seed ^ 61) ^ (seed >> 16);
				seed *= 9;
				seed = seed ^ (seed >> 4);
				seed *= 0x27d4eb2d;
				seed = seed ^ (seed >> 15);
				return seed;
			}
			inline bool IsBelowPlane(float3 p, float3 planeNormal, float3 planeCenter)
			{
				float t = dot(normalize(p - planeCenter), normalize(planeNormal));
				if (t < -0.01f) return true;
				return false;
			}
			[maxvertexcount(32)]
			void geo(triangle DomainOut p[3],
				uint primID : SV_PRIMITIVEID,
			 	inout TriangleStream<g2f> pStream)
			{

				float3 centerPos = (p[0].pos + p[1].pos + p[2].pos) / 3;
				float3 centerNor = (p[0].normal + p[1].normal + p[2].normal) / 3;
				//pos bias
			//	centerPos += normalize(centerNor) * 0.1;
				//float3 centerPos = p[i].pos;
				//for(int i = 0; i < _IsmCount; ++i)
				//{
				int vplID;
				float3 vplPos;
				float3 vplNormal;

				NumberGenerator gen;
				gen.SetSeed(primID);
				for(int c = 0; c < 8; ++c)
				{
				for(int i = 0; i < _IsmCount; ++i)
				{
					int vplID_ = (random(centerPos+int2(i+c,i+c))*_IsmCount)%_IsmCount;
					//vplID_ = 0;

					//需要裁剪？	
					float3 vplPos_ = _VplPosition[vplID_];
					float3 vplNormal_ = _VplNormal[vplID_];
					float3 vertToLightWS = centerPos - vplPos;
					float dist2Camera = length(vertToLightWS);
					if(IsBelowPlane(centerPos, vplNormal_, vplPos_)

					|| dist2Camera < 0.01)
					{
						continue;
					}
					else
					{
						vplID = vplID_;
						vplPos=vplPos_;
						vplNormal=vplNormal_;
						break;
					}
				}

				//vplID = uint(random(float2(primID+i,primID+i)) * 1020) % 1020;
				//vplID = 2;
				 //vplID = 0;
				// int ismIndices1d = int(pow(2, ceil(log2(1024) / 2))); // next even power of two 长
				int ismIndices1d = 16;//32;
				//float3 vplPos = _VplPosition[vplID];
			//	float3 vplNormal = _VplNormal[vplID];
				float3 vertToLightWS = centerPos - vplPos;
				float dist2Camera = length(vertToLightWS);

				float4 pos = float4(dual_paraboloid(centerPos, vplPos, vplNormal, _ZFar.x, vplID, ismIndices1d), 1.0);
			//	if(pos.z > 1 || pos.z < 0) return;
				//	if(pos.x < 0 || pos.y < 0) return;
				pos.z = 1 - pos.z;
				float maxDist = max(max(length(centerPos - p[0].pos), length(centerPos - p[1].pos)), length(centerPos - p[2].pos));
				float pointSize = 1*(maxDist * 2.0) / dist2Camera / 3.14;
			//	float r = (maxDist) / dist2Camera/10 ;///11：看效果调出来的
			//	r = min(r, (maxDist) / dist2Camera/10);
			//

				float r=(pos.z)*(pos.z)*10/_ShadowMapSize;
				r = clamp(r, 1.0f/_ShadowMapSize,4.0f/_ShadowMapSize);

				//float r = pointSize;
				//create quad radius base on distance
				float2 v1 = pos.xy + float2(-r, +r);
				float2 v2 = pos.xy + float2(+r, +r);
				float2 v3 = pos.xy + float2(+r, -r);
				float2 v4 = pos.xy + float2(-r, -r);

				//dx: y向下为正
				//1st tri v1 v2 v3W
				pStream.Append(createVertex(v1, pos.z));
				pStream.Append(createVertex(v4, pos.z));
				pStream.Append(createVertex(v2, pos.z));
				//pStream.RestartStrip();
				//2nd v1 v3 v4
				//pStream.Append(createVertex(v4, pos.z));
				//pStream.Append(createVertex(v2, pos.z));
				pStream.Append(createVertex(v3, pos.z));
				pStream.RestartStrip();
			}
			}

			//片元shader  
			float frag(g2f i):SV_TARGET
			{
			 	if (i.depth < 0.0) discard;
					//return float4(i.depth, i.depth, i.depth,1);
					return i.depth;
			//return 1;

			}
			ENDCG
		}	
	}
}