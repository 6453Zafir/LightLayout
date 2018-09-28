#ifndef GBUFFER_INCLUDED
#define GBUFFER_INCLUDED

Texture2D<float4> _NormalTex;
Texture2D<float4> _PositionTex;
SamplerState sampler_NormalTex;
SamplerState sampler_PositionTex;

float4x4 _GBufferCameraVPInverse;
float3 _GBufferCameraPos;

inline float4 restore(float4 x)
{ 
    return x / 0.5f -1.0f;
}

inline float3 SampleGBufferPosition(float2 uv, int texSize, out bool isLegal)
{
	float4 color = _PositionTex.SampleLevel(sampler_PositionTex, uv/texSize, 0);
	//float4 color = _PositionTex[uv];
	isLegal = any(color);
	float4 ndcPos = float4(restore(color));

	float4 worldSpaceHPos = mul(_GBufferCameraVPInverse, ndcPos);
	return worldSpaceHPos.xyz / worldSpaceHPos.w;
}

inline float3 SampleGBufferNormal(float2 uv, int texSize)
{
	float4 color = _NormalTex.SampleLevel(sampler_NormalTex, uv / texSize, 0);
    //float4 color = _NormalTex[uv];
    return float3(restore(color).xyz);
}
#endif