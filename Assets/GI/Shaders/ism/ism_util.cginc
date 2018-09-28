#ifndef ISM_UTIL_INCLUDED
#define ISM_UTIL_INCLUDED

 float4x4 inverse(float4x4 input)
 {
     #define minor(a,b,c) determinant(float3x3(input.a, input.b, input.c))
     //determinant(float3x3(input._22_23_23, input._32_33_34, input._42_43_44))
     
     float4x4 cofactors = float4x4(
          minor(_22_23_24, _32_33_34, _42_43_44), 
         -minor(_21_23_24, _31_33_34, _41_43_44),
          minor(_21_22_24, _31_32_34, _41_42_44),
         -minor(_21_22_23, _31_32_33, _41_42_43),
         
         -minor(_12_13_14, _32_33_34, _42_43_44),
          minor(_11_13_14, _31_33_34, _41_43_44),
         -minor(_11_12_14, _31_32_34, _41_42_44),
          minor(_11_12_13, _31_32_33, _41_42_43),
         
          minor(_12_13_14, _22_23_24, _42_43_44),
         -minor(_11_13_14, _21_23_24, _41_43_44),
          minor(_11_12_14, _21_22_24, _41_42_44),
         -minor(_11_12_13, _21_22_23, _41_42_43),
         
         -minor(_12_13_14, _22_23_24, _32_33_34),
          minor(_11_13_14, _21_23_24, _31_33_34),
         -minor(_11_12_14, _21_22_24, _31_32_34),
          minor(_11_12_13, _21_22_23, _31_32_33)
     );
     #undef minor
     return transpose(cofactors) / determinant(input);
 }
float4x4 lookAtRH(float3 eye, float3 forward)
{
    float3 up;
    if (abs(forward.x) < 0.015625f 
    && abs(forward.y) < 0.015625f)
        up = float3(0, 1, 0);
    else
        up = float3(0, 0, 1);
    float3 z = normalize(forward);
    float3 x = normalize(cross(z, up));
    float3 y = normalize(cross(z, x));
    
    //这nm又是行优先了？
    return float4x4(x.x, y.x, z.x, 0,
                    x.y, y.y, z.y,  0,
                    x.z, y.z, z.z, 0,
                    -dot(x,eye), -dot(y, eye), -dot(z, eye), 1);
}

float4x4 lookAtLH(float3 eye, float3 normalizedNormal)
{
    float3 up;
    if (abs(normalizedNormal.x) < 0.015625f 
    && abs(normalizedNormal.y) < 0.015625f)
        up = float3(0, 1, 0);
    else
        up = float3(0, 0, 1);
    float3 z= normalizedNormal;
    float3 x = normalize(cross(z, up));
    float3 y = cross(x, z);

    return float4x4(x.x, y.x, z.x, 0,
                    x.y, y.y, z.y, 0,
                    x.z, y.z, z.z, 0,
                    -dot(x,eye), -dot(y, eye), -dot(z, eye), 1);
}
static float Pi = 3.1415;

float3 sphericalMap(float3 position,float3 vplPos, float3 vplNormal, float zFar, float ismIndex, int ismIndices1d) {
   // float4x4 vplView = lookAtLH(vplPos, normalize(vplNormal));
    float4x4 vplView = lookAtRH(vplPos, normalize(vplNormal));

    //float4 posCam = mul(float4(position,1), vplView);
    float4 posCam = mul(vplView, float4(position,1));
    posCam.xyz /= posCam.w;
    //float3 posCam = positionRelativeToCamera;

    float3 pos = posCam / zFar;
    float pz = length(pos);
    pz = 1-pz;
    pz += 0.1;
    pos = normalize(pos);
    float theta = acos(-pos.z);
    if (theta > Pi * 0.5) {
        theta = Pi - theta;
    }

    float len = sqrt(pos.x * pos.x + pos.y * pos.y + pos.z*pos.z);
    float3 v =  float3(pos.xy / len * theta / (Pi * 0.5), pz);
   
    v.xy += 1.0;
    v.xy /= 2.0;
    int y = int(ismIndex / ismIndices1d);
    int2 ismIndex2d = int2(int(ismIndex - y * ismIndices1d) , y);
    v.xy += float2(ismIndex2d);
    v.xy /= ismIndices1d;


    return v;
}

float3 paraboloid_project(float3 position,float3 vplPos, float3 vplNormal, float zFar, float ismIndex, int ismIndices1d)
{
    float4x4 vplView = lookAtRH(vplPos, normalize(vplNormal));
    float4 v = mul(vplView, float4(position,1));

     float signOfV = sign(v.z);
    v.xyz /= v.w;
    float distToCamera = length(v);
    

    // paraboloid projection
    v.xyz /= distToCamera;
    v.z = 1.0 - v.z;

    v.xy /= v.z;
    v.z = distToCamera /zFar;
    //if (length(v.xy) >1) return float3(1,1,1);
   // if(length(v.xy) > 0.95) return float3(NaN,NaN,NaN);
   // if (preserveSign)
    //\\    v.z *= -signOfV;
    // scale and bias to texcoords
    v.xy += 1.0;
    v.xy /= 2.0;
    // scale and bias to texcoords

                    
    // offset to respective ISM
    //float offsetX = ismIndex%36-1;
    //float offsetY = (ismIndex-offsetX)/36;
    // v.xy /= 36;
    // v.xy += float2(offsetX/18, offsetY/18);
    float y = int(ismIndex / ismIndices1d);
    int2 ismIndex2d = int2(int(ismIndex - y * ismIndices1d) , y);
    v.xy += float2(ismIndex2d);
    v.xy /= ismIndices1d;

    //v.xy += 1.0;
    //v.xy /= 2.0;
    return v;
}
float3 dual_paraboloid(float3 position, float3 vplPos, float3 vplNormal, float zFar, float ismIndex, int ismIndices1d)
{
    float4x4 vplView = lookAtRH(vplPos, normalize(vplNormal));
    float4 v = mul(float4(position,1), vplView);
    v.xyz /= v.w;

    float len = length(v);
    v.xyz /= len;
    v.z  += 1;

    v.xy /= v.z;
    v.y = -v.y;
    v.z = (len-0.1) /(zFar-0.1)+0.1;
    //if (length(v.xy) >1) return float3(1,1,1);
   // if(length(v.xy) > 0.95) return float3(NaN,NaN,NaN);
  //  if (preserveSign)
    // scale and bias to texcoords

    v.xy += 1.0;
    v.xy /= 2.0;
    // scale and bias to texcoords

                    
    // offset to respective ISM
    //float offsetX = ismIndex%36-1;
    //float offsetY = (ismIndex-offsetX)/36;
    // v.xy /= 36;
    // v.xy += float2(offsetX/18, offsetY/18);
    float y = int(ismIndex / ismIndices1d);
    int2 ismIndex2d = int2(int(ismIndex - y * ismIndices1d) , y);
    v.xy += float2(ismIndex2d);
    v.xy /= ismIndices1d;
    //v.xy += 1.0;
    //v.xy /= 2.0;
    return v;
}
#endif