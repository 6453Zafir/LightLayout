#ifndef PCSS_INCLUDED
#define PCSS_INCLUDED
#define NGSS_BIAS_FADE 0.5//0.015

#define NGSS_POISSON_SAMPLING_NOISE 10.0 	

#define PCSS_ENABLED

static const float2 PoissonDisks[64] =
{
	float2 (0.1187053, 0.7951565),
	float2 (0.1173675, 0.6087878),
	float2 (-0.09958518, 0.7248842),
	float2 (0.4259812, 0.6152718),
	float2 (0.3723574, 0.8892787),
	float2 (-0.02289676, 0.9972908),
	float2 (-0.08234791, 0.5048386),
	float2 (0.1821235, 0.9673787),
	float2 (-0.2137264, 0.9011746),
	float2 (0.3115066, 0.4205415),
	float2 (0.1216329, 0.383266),
	float2 (0.5948939, 0.7594361),
	float2 (0.7576465, 0.5336417),
	float2 (-0.521125, 0.7599803),
	float2 (-0.2923127, 0.6545699),
	float2 (0.6782473, 0.22385),
	float2 (-0.3077152, 0.4697627),
	float2 (0.4484913, 0.2619455),
	float2 (-0.5308799, 0.4998215),
	float2 (-0.7379634, 0.5304936),
	float2 (0.02613133, 0.1764302),
	float2 (-0.1461073, 0.3047384),
	float2 (-0.8451027, 0.3249073),
	float2 (-0.4507707, 0.2101997),
	float2 (-0.6137282, 0.3283674),
	float2 (-0.2385868, 0.08716244),
	float2 (0.3386548, 0.01528411),
	float2 (-0.04230833, -0.1494652),
	float2 (0.167115, -0.1098648),
	float2 (-0.525606, 0.01572019),
	float2 (-0.7966855, 0.1318727),
	float2 (0.5704287, 0.4778273),
	float2 (-0.9516637, 0.002725032),
	float2 (-0.7068223, -0.1572321),
	float2 (0.2173306, -0.3494083),
	float2 (0.06100426, -0.4492816),
	float2 (0.2333982, 0.2247189),
	float2 (0.07270987, -0.6396734),
	float2 (0.4670808, -0.2324669),
	float2 (0.3729528, -0.512625),
	float2 (0.5675077, -0.4054544),
	float2 (-0.3691984, -0.128435),
	float2 (0.8752473, 0.2256988),
	float2 (-0.2680127, -0.4684393),
	float2 (-0.1177551, -0.7205751),
	float2 (-0.1270121, -0.3105424),
	float2 (0.5595394, -0.06309237),
	float2 (-0.9299136, -0.1870008),
	float2 (0.974674, 0.03677348),
	float2 (0.7726735, -0.06944724),
	float2 (-0.4995361, -0.3663749),
	float2 (0.6474168, -0.2315787),
	float2 (0.1911449, -0.8858921),
	float2 (0.3671001, -0.7970535),
	float2 (-0.6970353, -0.4449432),
	float2 (-0.417599, -0.7189326),
	float2 (-0.5584748, -0.6026504),
	float2 (-0.02624448, -0.9141423),
	float2 (0.565636, -0.6585149),
	float2 (-0.874976, -0.3997879),
	float2 (0.9177843, -0.2110524),
	float2 (0.8156927, -0.3969557),
	float2 (-0.2833054, -0.8395444),
	float2 (0.799141, -0.5886372)
};
static const float2 PoissonDisksTest[16] =
{
	float2(0.1232981, -0.03923375),
	float2(-0.5625377, -0.3602428),
	float2(0.6403719, 0.06821123),
	float2(0.2813387, -0.5881588),
	float2(-0.5731218, 0.2700572),
	float2(0.2033166, 0.4197739),
	float2(0.8467958, -0.3545584),
	float2(-0.4230451, -0.797441),
	float2(0.7190253, 0.5693575),
	float2(0.03815468, -0.9914171),
	float2(-0.2236265, 0.5028614),
	float2(0.1722254, 0.983663),
	float2(-0.2912464, 0.8980512),
	float2(-0.8984148, -0.08762786),
	float2(-0.6995085, 0.6734185),
	float2(-0.293196, -0.06289119)
};

//#ifdef COMPUTE_SHADER
TextureCube<float> _CubeShadowMap;
SamplerState sampler_CubeShadowMap;
SamplerState my_linear_clamp_sampler;
//#endif

//#ifdef SHADER
//samplerCUBE_float _CubeShadowMap;
//#endif
float3 randDirOld(float3 seed)
{
	float3 dt = float3 (dot(seed, float3 (12.9898,78.233,45.5432)), dot(seed, float3 (78.233,45.5432,12.9898)), dot(seed, float3 (45.5432,12.9898,78.233)));
	return sin(frac(sin(dt) * 43758.5453)*6.283285);
}
float3 randAxis(float3 normal)
{
	float3 axis;
	if (abs(normal.x) < 0.015625f
		&& abs(normal.y) < 0.015625f)
		axis = cross(float3(0, 1, 0), normal);
	else
		axis = cross(float3(0, 0, 1), normal);
	axis= normalize(axis);
	return axis;
}
float3 randDir(float3 seed)
{
	return (frac(sin(cross(seed, float3 (12.9898, 78.233, 45.5432))) * 43758.5453)*NGSS_POISSON_SAMPLING_NOISE + 0.0001);
}

inline float CubeMapDistance (float3 vec)
{
//#ifdef COMPUTE_SHADER
	return _CubeShadowMap.SampleLevel(sampler_CubeShadowMap, vec, 0);
//#else
	//return texCUBE(_CubeShadowMap, vec);
//#endif
}

//float _SlopeBiasFactor;
float _ShadowSoftness;
int _ShadowMapSize;

float2 _FarPlane; //x:farplane y:1/farplane

//_LightShadowData.x - shadow strength
//_LightShadowData.y - Appears to be unused
//_LightShadowData.z - 1.0 / shadow far distance
//_LightShadowData.w - shadow near distance
//float4 _PolygonLightShadowData;
float PCSS (float3 vec, float lightWidth, float3 objNormal)
{
#ifndef PCSS_ENABLED
	float worldDist = length(vec);
	float closestDepth = CubeMapDistance(vec);
	if (closestDepth < worldDist*_FarPlane.y)
	{
		return 0;
	}
	else
	{
		return 1;
	}
#else
	
	//float _ShadowSoftness = 1;
	float worldDist = length(vec);
	float dist01 = length(vec) *_FarPlane.y;
	float lightWidth01 = lightWidth * _FarPlane.y;
	// Tangent plane
	float3 xaxis = randAxis(objNormal);
	float3 yaxis = normalize (cross (objNormal, xaxis));

	float shadow = 0.0;

	float diskRadius = 0;//(1 - _LightShadowData.r);

	diskRadius *= 0.25;		
	xaxis *= diskRadius;
	yaxis *= diskRadius;

	//Blocker Search
	float blockerCount = 0;
	float avgBlockerDistance = 0.0;	
				
	float cosTheta = dot(objNormal, normalize(-vec));
	cosTheta = saturate(cosTheta);
	float slopeBias = 0.003* tan(acos(cosTheta)) ;//0.03


	for(int i = 0; i < 16; ++i)
	{
		float3 sampleDir = xaxis * PoissonDisksTest[i].x + yaxis * PoissonDisksTest[i].y;
			
		half closestDepth01 = CubeMapDistance(vec + sampleDir);

		if(closestDepth01 - 0.1f< dist01)
		{
			blockerCount++;
			avgBlockerDistance += closestDepth01;
		}
	}
		
	if( blockerCount == 0 )//There are no occluders so early out (this saves filtering) 
		return 1.0f;
	if(blockerCount == 16)
		return 0.0f;
			
	float dist = avgBlockerDistance / blockerCount;
	//clamping the kernel size to avoid blocky shadows at close ranges
	//float penumbra = clamp((worldDist - dist)*lightWidth/dist, 0.1, 10000.0);
	half diskRadiusPCF = ((dist01 - dist)  /(dist01));

	//float filterRadiusUV = penumbra * _ShadowSoftness;
	//PCF	
	for (int i = 0; i < 64; ++i)
	{
		float3 sampleDir = xaxis * PoissonDisks[i].x + yaxis * PoissonDisks[i].y;

		half closestDepth01 = CubeMapDistance(vec + sampleDir* diskRadiusPCF);
		float shadowsFade = NGSS_BIAS_FADE *_FarPlane.y;// _LightPositionRange.w:1/range = 1/farPlane
		shadow += 1 - saturate((dist01 - closestDepth01) / shadowsFade);
		//shadow += (closestDepth01 + 0.01f< dist01) ? 1.0 : 0.0;

	}
	return shadow/64;
#endif
}


#endif