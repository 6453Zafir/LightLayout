#ifndef POLYGON_INCLUDED

#define POLYGON_INCLUDED

float GetPolygonArea(int n, float3 p[10])
{
	float area = 0;
	for (int i = 1; i <= n - 2; i++)
		area += length(cross(p[i] - p[0], p[i + 1] - p[0]));
	return area / 2;
}

float GetPolygonArea(int n, StructuredBuffer<float3> p)
{
	float area = 0;
	for (int i = 1; i <= n - 2; i++)
		area += length(cross(p[i] - p[0], p[i + 1] - p[0]));
	return area / 2;
}

inline bool IsBelowPlane(float3 p, float3 planeNormal, float3 planeCenter)
{
	float t = dot(p - planeCenter, normalize(planeNormal));
	if (t < 0.03f) return true;
	return false;
}

inline float3 GetPlaneLineIntersect(float3 planeCenter, float3 planeNormal, float3 p0, float3 dirToPlane)
{
	float3 v0Minp0 = planeCenter - p0;
	float s = dot(planeNormal, v0Minp0) / dot(planeNormal, dirToPlane);
	return p0 + s * dirToPlane;
}

//Sutherland-Hodgman多边形裁剪算法
void ClipRectBelowLumel(float3 clipPlaneNormal, float3 clipPlaneCenter, StructuredBuffer<float3> rect, float3 rectNormal, uint rectIndexCount, out float3 clippedRect[10], out uint vertexCount)
{
	int idxCount = 0;
	for (uint i = 0; i < rectIndexCount; ++i)
	{
		float3 v1 = rect[i];
		float3 v2 = rect[(i + 1) % rectIndexCount];
		bool side1 = IsBelowPlane(v1, clipPlaneNormal, clipPlaneCenter);
		bool side2 = IsBelowPlane(v2, clipPlaneNormal, clipPlaneCenter);

		//都在平面上方
		if (!side1 && !side2)
		{
			clippedRect[idxCount++] = v1;
			//clipRect.Add(v2);
		}
		//a在内侧，b在外侧，则先输出a，再输出ab和分界线的交点；
		else if (!side1 && side2)
		{
			clippedRect[idxCount++] = v1;
			clippedRect[idxCount++] = GetPlaneLineIntersect(clipPlaneCenter, clipPlaneNormal, v1, v2 - v1);
		}
		//a,b都在外侧，则跳过这条边； 
		//else if (side1 && side2)
		//{

		//}
		//a在外侧，b在内侧，则输出ab和分界线的交点。
		else if (side1 && !side2)
		{
			clippedRect[idxCount++] = GetPlaneLineIntersect(clipPlaneCenter, clipPlaneNormal, v1, v2 - v1);
		}
	}
	vertexCount = idxCount;
}

void RearrangePolygonStrip(float3 polygon[10], float count, out float3 arrangedStrip[10])
{
	if (count == 3)
	{
		arrangedStrip[0] = polygon[0];
		arrangedStrip[1] = polygon[1];
		arrangedStrip[2] = polygon[2];
	}
	else if (count == 4)
	{
		arrangedStrip[0] = polygon[0];
		arrangedStrip[1] = polygon[1];
		arrangedStrip[2] = polygon[3];
		arrangedStrip[3] = polygon[2];
	}
	else if (count == 5)
	{
		arrangedStrip[0] = polygon[0];
		arrangedStrip[1] = polygon[1];
		arrangedStrip[2] = polygon[3];
		arrangedStrip[3] = polygon[4];
		arrangedStrip[4] = polygon[2];
	}
	else if(count == 6)
	{
		arrangedStrip[0] = polygon[0];
		arrangedStrip[1] = polygon[1];
		arrangedStrip[2] = polygon[3];
		arrangedStrip[3] = polygon[5];
		arrangedStrip[4] = polygon[4];
		arrangedStrip[5] = polygon[2];
	}
}
#endif