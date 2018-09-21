// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "LightMapper/GetSingleLM"  
{  
    //属性  
    Properties  
    {
    }  
  
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
			#include "UnityCG.cginc"
            //定义结构体：应用阶段到vertex shader阶段的数据  
            struct a2v  
            {  
                float4 vertex : POSITION;  
				float4 uv2	: TEXCOORD1;
            };  
  
            //定义结构体：vertex shader阶段输出的内容  
            struct v2f  
            {  
                float4 pos : SV_POSITION; 			
				float2 uv2 : TEXCOORD2;
            };  
			
            //顶点shader  
            v2f vert(a2v v)  
            {  
                v2f o;  
                o.pos = float4(v.uv2.x * 2 - 1, v.uv2.y * -2 + 1, 0, 1);
				o.uv2 = v.uv2;
                return o;  
            }  
			sampler2D _Lightmap;
			float4 _LightmspST;
			#define pi 3.14159265358979 
			
            //片元shader  
            fixed4 frag(v2f i) : SV_Target  
            { 		

				float4 color = tex2D(_Lightmap, i.uv2* _LightmspST.xy + _LightmspST.zw);
				return float4(color.xyz, 1);
            }  
            ENDCG  
        }  
    }  
} 