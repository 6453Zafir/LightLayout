using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Runtime.InteropServices;
using GI;
using GI.Libs;
using UnityEngine;
using UnityEngine.Rendering;

public class ISMTestCS : MonoBehaviour
{
    public int ShadowMapSize = 4096;
    public RenderTexture ISM;
    public FilterMode ShadowMapFilterMode = FilterMode.Trilinear;
    private UnityLmGBuffer _gBuffer;
    private Bounds _sceneAABB;
    public ComputeShader CollectGeomertyShader;
    public ComputeShader IsmCS;

    public int LightMapSize;

    private List<RenderTexture> _lightmaps = new List<RenderTexture>();
    private ComputeBuffer _irradianceBuffer;
    private ComputeBuffer _polygonLightBuffer;

    public RenderTexture ZBuffer;
    // Use this for initialization
    void Start()
    {
        SceneParamater.InitLightmapSetting();
        for (int i = 0; i < LightmapSettings.lightmaps.Length; ++i)
        {
            var lightmap = new RenderTexture(LightMapSize, LightMapSize, 24,
                    RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
                { enableRandomWrite = true };
           
            lightmap.Create();
            _lightmaps.Add(lightmap);
        }
        var boxColliders = GameObject.FindObjectsOfType<MeshRenderer>().Select(x => x.bounds).ToList();
        _sceneAABB = boxColliders[0];
        foreach (var obj in boxColliders)
        {
            _sceneAABB.Encapsulate(obj);
        }
        _gBuffer = new UnityLmGBuffer(CollectGeomertyShader, LightMapSize);
        _gBuffer.PrepareGeometry(false);

        //createShadowMap
        ISM =
            new RenderTexture(ShadowMapSize, ShadowMapSize, 0, RenderTextureFormat.RFloat,
                RenderTextureReadWrite.Linear);
        ISM.filterMode = ShadowMapFilterMode;
        ISM.enableRandomWrite = true;
       // ISM.dimension = TextureDimension.Cube;
        ISM.Create();

        ZBuffer =
            new RenderTexture(ShadowMapSize, ShadowMapSize, 0, RenderTextureFormat.RFloat,
                RenderTextureReadWrite.Linear);
        ZBuffer.filterMode = ShadowMapFilterMode;
        ZBuffer.enableRandomWrite = true;
        // ISM.dimension = TextureDimension.Cube;
        ZBuffer.Create();
        //塞入lumel的信息
        _irradianceBuffer = new ComputeBuffer(1, sizeof(float) * 4);
        UpdateLightInfo();

        _polygonLightBuffer = new ComputeBuffer(_polygonLight.Count, sizeof(float) * 3);


    }
    void OnDisable()
    {
        _irradianceBuffer.Release();
        _polygonLightBuffer.Release();
    }

    private float Intensity => AreaLight_.Intensity;
    public GIAreaLight AreaLight_;
    private List<Vector3> _polygonLight = new List<Vector3>();
    private List<Vector3> _polygonLightWithCenter = new List<Vector3>();
    public Vector3 PolygonLightCenter => AreaLight_.transform.position;
    public Vector3 PolygonLightNormal => AreaLight_.transform.forward;
    private ThreeDPlaneCoordinateSystem _lightCoord;

    public float Width
    {
        set
        {
            AreaLight_.UnityLight.areaSize =  new Vector2(value, AreaLight_.UnityLight.areaSize.y);
        }
        get { return AreaLight_.UnityLight.areaSize.x; }
    }
    public float Height
    {
        set
        {
            AreaLight_.UnityLight.areaSize = new Vector2(AreaLight_.UnityLight.areaSize.x,value);
        }
        get { return AreaLight_.UnityLight.areaSize.y; }
    }

    void UpdateLightInfo()
    {
        var center = PolygonLightCenter;
        // var size = AreaLight_.areaSize;
        //-y
        var normal = AreaLight_.transform.forward;
        _lightCoord = new ThreeDPlaneCoordinateSystem(center, normal);
        Vector2 center2d = _lightCoord.World2Object.MultiplyPoint3x4(center);
        List<Vector2> vertex = new List<Vector2>()
        {
            new Vector2(center2d.x - Width, center2d.y + Height),
            new Vector2(center2d.x + Width, center2d.y + Height),
            new Vector2(center2d.x + Width, center2d.y - Height),
            new Vector2(center2d.x - Width, center2d.y - Height)
        };
        _polygonLight = vertex.Select(p => _lightCoord.Object2World.MultiplyPoint3x4(p)).ToList();
        _polygonLightWithCenter = _polygonLight.ToList();
        _polygonLightWithCenter.Add(GetPolygonCenter(_polygonLight));
    }
   
    public Vector3 GetPolygonCenter(List<Vector3> vertexs)
    {
        Vector3 c  = Vector3.zero;
        foreach (var v in vertexs)
        {
            c += v;
        }

        c /= vertexs.Count;
        return c;
    }
    public SceneParam SceneParamater;
    public ComputeShader IndirectLightPassCS;
    public Color PolygonLightPower => new Color(AreaLight_.UnityLight.color.r * Intensity, AreaLight_.UnityLight.color.g * Intensity,
        AreaLight_.UnityLight.color.b * Intensity);
    void Update()
    {
        //_lightMap.filterMode = LightMapFilterMode;
        ISM.filterMode = ShadowMapFilterMode;
        //MousePick();
        UpdateLightInfo();
        Vector2 farPlane;
        ShadowMapParams shadowMapFactor = new ShadowMapParams()
        {
            ShadowBias = SceneParamater.ShadowBias,
            ShadowSlopeFactor = SceneParamater.ShadowSlopeFactor,
        };

        for (int i = 0; i < LightmapSettings.lightmaps.Length; ++i)
        {
            IsmUseCS(i, shadowMapFactor, out farPlane);

            {
                IrradianceUseComputeShader(IndirectLightPassCS, _gBuffer, ISM,
                    new PolygonLight()
                    {
                        Center = PolygonLightCenter,
                        PolygonHull = _polygonLight,
                        SumPower = PolygonLightPower,
                        Normal = PolygonLightNormal
                    }, _lightmaps[i], farPlane, i, shadowMapFactor);
            }

        }
        SetLightMap();
    }
    public  void IrradianceUseComputeShader(ComputeShader indirectLightPassCS, UnityLmGBuffer gBuffer, RenderTexture shadowmap, PolygonLight polygonLight, RenderTexture rt,
    Vector2 farPlane, int lightmapIndex,  ShadowMapParams shadowFactor)
    {
        //rt.Clear();

        int mainKernel = indirectLightPassCS.FindKernel("CSMain");


        indirectLightPassCS.SetTexture(mainKernel, "_NormalTex", gBuffer.NormalTex[lightmapIndex]);
        indirectLightPassCS.SetTexture(mainKernel, "_PositionTex", gBuffer.PositionTex[lightmapIndex]);

        Matrix4x4 matrix = gBuffer.RenderCameraVPInverse[lightmapIndex];
        //转换成行优先？
        float[] matrixFloats = {
            matrix[0,0], matrix[1, 0], matrix[2, 0], matrix[3, 0],
            matrix[0,1], matrix[1, 1], matrix[2, 1], matrix[3, 1],
            matrix[0,2], matrix[1, 2], matrix[2, 2], matrix[3, 2],
            matrix[0,3], matrix[1, 3], matrix[2, 3], matrix[3, 3]
        };
        float[] matrixFloats1 = {
            matrix[0,0], matrix[0, 1], matrix[0, 2], matrix[0, 3],
            matrix[1,0], matrix[1, 1], matrix[1, 2], matrix[1, 3],
            matrix[2,0], matrix[2, 1], matrix[2, 2], matrix[2, 3],
            matrix[3,0], matrix[3, 1], matrix[3, 2], matrix[3, 3]
        };
        indirectLightPassCS.SetFloats("_GBufferCameraVPInverse", matrixFloats);

        indirectLightPassCS.SetVector("_GBufferCameraPos", gBuffer.RenderCameraPos[lightmapIndex]);


        //塞入polygonlight 多边形的信息
        ComputeBuffer polygonLightBuffer = new ComputeBuffer(_polygonLight.Count, sizeof(float) * 3);
        //polygonLightBuffer.SetData(polygonLight.PolygonHull.ToArray());
        polygonLightBuffer.SetData(_polygonLight.ToArray());
        indirectLightPassCS.SetBuffer(mainKernel, "_PolygonHull", polygonLightBuffer);

        indirectLightPassCS.SetVector("_PolygonCenter", polygonLight.Center);
        indirectLightPassCS.SetVector("_PolygonNormal", polygonLight.Normal);
        indirectLightPassCS.SetVector("_PolygonTotalFlux", polygonLight.SumPower);
        indirectLightPassCS.SetInt("_PolygonVertexCount", _polygonLight.Count);
        indirectLightPassCS.SetFloat("_ShadowBias", shadowFactor.ShadowBias);

        indirectLightPassCS.SetVector("_FarPlane", farPlane);
        indirectLightPassCS.SetInt("_LightMapSize", LightMapSize);
        indirectLightPassCS.SetInt("_ShadowMapSize", ShadowMapSize);
        indirectLightPassCS.SetInt("_IsmCount", MaxVPLPerIsm);
        indirectLightPassCS.SetInt("_IsmIndexOffset", 0);
        indirectLightPassCS.SetFloat("_ShadowSoftness", shadowFactor.ShadowSoftness);
        indirectLightPassCS.SetFloat("_BiasFade", shadowFactor.BiasFade);
        //塞入最后返回的RenderTexture
        indirectLightPassCS.SetTexture(mainKernel, "_LightMap", rt);

        indirectLightPassCS.SetTexture(mainKernel, "_Ism", ISM);

        //  ComputeBuffer outputPosBuffer = new ComputeBuffer(100*1000, sizeof(float));
        //  IndirectLightPassCS.SetBuffer(mainKernel, "_OutputGBufferPos", outputPosBuffer);


        indirectLightPassCS.Dispatch(mainKernel, LightMapSize / 32, LightMapSize / 32, 1);


        //dispose           
        polygonLightBuffer.Dispose();
    }
    public int MaxVPLPerIsm = 1;

    public void IsmUseCS(int lightmapIndex,ShadowMapParams shadowFactor, out Vector2 farPlane)
    {
        ISM.Clear(Color.black);
        ZBuffer.Clear(new Color(10,10,10,10));

        int mainKernel = IsmCS.FindKernel("CSMain");

        IsmCS.SetTexture(mainKernel, "_NormalTex", _gBuffer.NormalTex[lightmapIndex]);
        IsmCS.SetTexture(mainKernel, "_PositionTex", _gBuffer.PositionTex[lightmapIndex]);

        Matrix4x4 matrix = _gBuffer.RenderCameraVPInverse[lightmapIndex];
        //转换成行优先？
        float[] matrixFloats = {
            matrix[0,0], matrix[1, 0], matrix[2, 0], matrix[3, 0],
            matrix[0,1], matrix[1, 1], matrix[2, 1], matrix[3, 1],
            matrix[0,2], matrix[1, 2], matrix[2, 2], matrix[3, 2],
            matrix[0,3], matrix[1, 3], matrix[2, 3], matrix[3, 3]
        };

        IsmCS.SetFloats("_GBufferCameraVPInverse", matrixFloats);

        IsmCS.SetVector("_GBufferCameraPos", _gBuffer.RenderCameraPos[lightmapIndex]);
        //塞入polygonlight 多边形的信息
        // ComputeBuffer vplPositionBuffer = new ComputeBuffer(MaxVPLPerIsm, sizeof(float) * 3);
        //vplPositionBuffer.SetData(Enumerable.Repeat(PolygonLightCenter, MaxVPLPerIsm).ToArray());
        ComputeBuffer vplPositionBuffer = new ComputeBuffer(_polygonLightWithCenter.Count, sizeof(float) * 3);
        vplPositionBuffer.SetData(_polygonLightWithCenter);
        //ComputeBuffer vplNormalBuffer = new ComputeBuffer(MaxVPLPerIsm, sizeof(float) * 3);
        //vplNormalBuffer.SetData(Enumerable.Repeat(PolygonLightNormal, MaxVPLPerIsm).ToArray());
        ComputeBuffer vplNormalBuffer = new ComputeBuffer(_polygonLightWithCenter.Count, sizeof(float) * 3);
        vplNormalBuffer.SetData(Enumerable.Repeat(PolygonLightNormal, _polygonLightWithCenter.Count).ToArray());

        IsmCS.SetBuffer(mainKernel, "_VplPosition", vplPositionBuffer);
        IsmCS.SetBuffer(mainKernel, "_VplNormal", vplNormalBuffer);

        //IsmCS.SetInt("_IsmCount", MaxVPLPerIsm);
        IsmCS.SetInt("_IsmCount", _polygonLightWithCenter.Count);
        IsmCS.SetInt("_GBufferTexSize", LightMapSize);
        IsmCS.SetInt("_ShadowMapSize", ShadowMapSize);
        var farClipPlane = _gBuffer.SceneAABB.size.y * 3;
        farPlane = new Vector2(farClipPlane, 1f / farClipPlane);
        IsmCS.SetVector("_ZFar", farPlane);

        IsmCS.SetTexture(mainKernel, "_Ism", ISM);
        IsmCS.SetTexture(mainKernel, "_ZBuffer", ZBuffer);

        IsmCS.Dispatch(mainKernel, LightMapSize / 32, LightMapSize / 32, 1);

        //dispose           
        vplPositionBuffer.Dispose();
        vplNormalBuffer.Dispose();
    }

    void DisposeLastLightmap()
    {
        if (LightmapSettings.lightmaps.Length > 0)
        {
            foreach (var l in LightmapSettings.lightmaps)
            {
                if (l.lightmapColor != null)
                {
                    DestroyImmediate(l.lightmapColor);
                    GC.Collect();
                }
            }
        }
    }


    void SetLightMap()
    {
        DisposeLastLightmap();
        LightmapSettings.lightmapsMode = LightmapsMode.NonDirectional;
        var lmCount = LightmapSettings.lightmaps.Length;
        LightmapData[] lightmapData = new LightmapData[lmCount];
        if (lmCount > 0)
        {
            for (int i = 0; i < lmCount; ++i)
            {
                //if (d.lightmapColor != null)
                //    Object.DestroyImmediate(d.lightmapColor, true);
                lightmapData[i] = new LightmapData();
                var tex = _lightmaps[i].ToTexture2D();
                tex.filterMode = FilterMode.Bilinear;
                lightmapData[i].lightmapColor = tex;
            }
        }

        LightmapSettings.lightmapsMode = LightmapsMode.NonDirectional;
        LightmapSettings.lightmaps = lightmapData;
        // Shader.se
        //Shader.SetGlobalTexture("_Lightmap", _lightmaps[0]);
        //  Shader.set
    }
    void OnDrawGizmos()
    {
        /*
        Gizmos.color = Color.red;

        for (int i = 0; i < _polygonLight.Count; ++i)
        {
            Gizmos.DrawLine(_polygonLight[i],
                _polygonLight[(i + 1) % _polygonLight.Count]);
        }
        */
    }
    static Matrix4x4 PerspectiveOffCenter(float left, float right, float bottom, float top, float near, float far)
    {
        float x = 2.0F * near / (right - left);
        float y = 2.0F * near / (top - bottom);
        float a = (right + left) / (right - left);
        float b = (top + bottom) / (top - bottom);
        float c = -(far + near) / (far - near);
        float d = -(2.0F * far * near) / (far - near);
        float e = -1.0F;
        Matrix4x4 m = new Matrix4x4();
        m[0, 0] = x;
        m[0, 1] = 0;
        m[0, 2] = a;
        m[0, 3] = 0;
        m[1, 0] = 0;
        m[1, 1] = y;
        m[1, 2] = b;
        m[1, 3] = 0;
        m[2, 0] = 0;
        m[2, 1] = 0;
        m[2, 2] = c;
        m[2, 3] = d;
        m[3, 0] = 0;
        m[3, 1] = 0;
        m[3, 2] = e;
        m[3, 3] = 0;
        return m;
    }
}