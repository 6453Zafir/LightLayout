#define DEBUG

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GI;
public class Baker : MonoBehaviour
{
    private VplUseUnityLm _gi;

    public int LightMapSize=1024;

    public int ShadowmapSize=2048;
    
    public int TotalSampleCount=8000;

    public float IndirectWight = 1;
    public float DirectWight = 1;
    public int Bounce=0;

    public ComputeShader DirectLightPass;

    public ComputeShader IndirectLightPass;

    public ComputeShader AccumlatePass;
    public SceneParam SceneParamater;
    public ComputeShader EncodeRgbm;

    public ComputeShader FloodIllegal;

    public ComputeShader CollectGeomerty;
    public bool SaveGBufferTexture = false;

    public bool SaveLightmapBeforePostprocessing;

    public bool SaveLightmapAfterPostprocessing;

    public bool BuildDirectLight = false;
    public bool BuildIndirectLight = false;

    private UnityLmGBuffer _geometryBuffer;

    private List<Light> _bakedLights;

    public static Baker Instance => _instance;
    
    void Awake()
    {
        _instance = this;
    }
    private static Baker _instance=null;
    public void Init()
    {
        var unityAPIInit = UnityAPICaller.Instance;
        _geometryBuffer?.Release();
        _gi?.Release();
        SceneParamater.InitLightmapSetting();
        Debug.Log("GI Init Begin");

        var meshRenderers = GameObject.FindObjectsOfType<MeshRenderer>();
        //gen mesh collider
        /*
        foreach (var go in meshRenderers)
        {
            if (go.GetComponent<MeshCollider>() == null)
            {
                go.gameObject.AddComponent<MeshCollider>();
            }
        }*/
        Debug.LogWarning("Has Added MeshCollider");
        var boxColliders = meshRenderers.Select(x => x.bounds).ToList();
        var sceneAABB = boxColliders[0];
        foreach (var obj in boxColliders)
        {
            sceneAABB.Encapsulate(obj);
        }

        _geometryBuffer = new UnityLmGBuffer(CollectGeomerty, LightMapSize);
        _geometryBuffer.PrepareGeometry(SaveGBufferTexture);

        _gi = new VplUseUnityLm(_geometryBuffer, LightMapSize, ShadowmapSize, TotalSampleCount, Bounce,
            RenderTextureFormat.ARGBFloat,
            DirectLightPass, IndirectLightPass, AccumlatePass);
        _gi.EncodeRgbmCS = EncodeRgbm;
        _gi.DirectLightWeight = DirectWight;
        _gi.IndirectLightWeight = IndirectWight;
        _gi.ShadowSlopeFactor = SceneParamater.ShadowSlopeFactor;
        _gi.ShadowBias = SceneParamater.ShadowBias;
        _gi.BuildFinish = false;

        _gi.Init();
        List<GILightBase> lights = new List<GILightBase>();
        lights.AddRange(FindObjectsOfType<GIPointLight>());
        lights.AddRange(FindObjectsOfType<GIAreaLight>());
        lights.AddRange(FindObjectsOfType<GIDirectionalLight>());
        _gi.EmitVPL(lights);
        //1.准备G-buffer
        //3.聚类成polygonlight
        _gi.Cluster2Polygon();

      //  ThreadPool.QueueUserWorkItem((state) =>
     //   {
            _gi.BuildLightMap(BuildDirectLight, BuildIndirectLight);

            Debug.Log("GI Init Finish");
      //  });
    }

    public void SyncObjectPosition(ClientSyncObjectData data)
    {
        var objToSync = GameObject.Find(data.name);
        if (objToSync != null)
        {
            LCRS.Log("====SYNC OBJECT: " + data.name);
            objToSync.transform.position = new Vector3(data.positionX, data.positionY, data.positionZ);
        }
    }

    public bool EnableHDR = true;
    [HideInInspector]
    public Texture2D[] LightmapData;
    public void SetLightMap()
    {
        LightmapSettings.lightmapsMode = LightmapsMode.NonDirectional;
        
        LightmapData[] lightmapData = new LightmapData[_geometryBuffer.LightmapCount];
        LightmapData = new Texture2D[_geometryBuffer.LightmapCount];
        if (LightmapSettings.lightmaps.Length > 0)
        {
            for (int i = 0; i < _geometryBuffer.LightmapCount; ++i)
            {
                //if (d.lightmapColor != null)
                //    Object.DestroyImmediate(d.lightmapColor, true);
                lightmapData[i] = new LightmapData();
                var lmTex2D = !EnableHDR ? _gi.EncodeRgba32(_gi.LightmapBeforePostProcessing[i].ToTexture2D()) 
                    : _gi.LightmapBeforePostProcessing[i].ToTexture2D();
                LightmapData[i] = lmTex2D;
                lightmapData[i].lightmapColor = lmTex2D;
            }
        }
        LightmapSettings.lightmapsMode = LightmapsMode.NonDirectional;
        LightmapSettings.lightmaps = lightmapData;
        //  Shader.set
    }

    // LightmapSettings.lightmaps = new LightmapData[_geometryBuffer.LightmapCount];
    // for (int i = 0; i < _geometryBuffer.LightmapCount; ++i)
    // {
    //    LightmapData data = new LightmapData();
    //     data.lightmapColor  = LightmapBeforePostProcessing[i].ToTexture2D();
    //    LightmapSettings.lightmaps[i] = data;
    // 
    public bool IsBuildFinish => _gi.BuildFinish;
    
    public byte[] ExtractLightmapBytes()
    {
        return null;
        //return ExtractLightmapBytes(_gi.LightmapBeforePostProcessing);
    }

    public static byte[] ExtractRTBytes(RenderTexture rt)
    {
        var tex2D = rt.ToTexture2D(RenderTextureFormat.ARGB32);
#if DEBUG
        //bytes = tex2D.EncodeToEXR();
        
        var bytes = tex2D.EncodeToPNG();
        //System.IO.File.WriteAllBytes("C:\\Test.png", bytes);
        return bytes;
#else
        return tex2D.GetRawTextureData();
#endif
    }

    public static byte[] ExtractLightmapBytes(Texture2D tex)
    {
#if DEBUG
        //bytes = tex2D.EncodeToEXR();

        var bytes = tex.EncodeToPNG();
        //System.IO.File.WriteAllBytes("C:\\Test.png", bytes);
        return bytes;
#else
        return tex2D.GetRawTextureData();
#endif
    }

    public static byte[] CopyToBig(byte[] bBig, byte[] bSmall)
    {
        byte[] tmp = new byte[bBig.Length + bSmall.Length];
        System.Buffer.BlockCopy(bBig, 0, tmp, 0, bBig.Length);
        System.Buffer.BlockCopy(bSmall, 0, tmp, bBig.Length, bSmall.Length);
        return tmp;
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(Screen.width * 3.0f / 5-10, 50, Screen.width *2 / 5.0f, Screen.height));

        if (GUILayout.Button("Init"))
        {
            Init();
        }
        if (GUILayout.Button("SaveLightmap"))
        {
            for(int i = 0; i < _geometryBuffer.LightmapCount; ++i)
            {
                if (EncodeRgbm)
                {
                    _gi.EncodeRgba32(_gi.LightmapBeforePostProcessing[i].ToTexture2D()).WriteToFile("", "Lightmap" + i);
                }
                else
                {
                    _gi.LightmapBeforePostProcessing[i].ToTexture2D().WriteToFile("", "Lightmap" + i);
                }
            }
        }
        if (GUILayout.Button("SetLightmap"))
        {
            SetLightMap();
        }
        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal();
        GUILayout.Label("LightmapSize: " + LightMapSize);
        LightMapSize = int.Parse(GUILayout.TextField(LightMapSize.ToString()));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("ShadowmapSize: " + ShadowmapSize);
        ShadowmapSize = int.Parse(GUILayout.TextField(ShadowmapSize.ToString()));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("TotalSampleCount");
        TotalSampleCount = int.Parse(GUILayout.TextField(TotalSampleCount.ToString()));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("IndirectWight: " + IndirectWight);
        IndirectWight = GUILayout.HorizontalSlider(IndirectWight, 0, 2);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Bounce: " + Bounce);
        Bounce = (int)GUILayout.HorizontalSlider(Bounce, 1, 4);
        GUILayout.EndHorizontal();

        ShowPolygonLight = GUILayout.Toggle(ShowPolygonLight, "Draw PolygonLight");
        ShowPointLight = GUILayout.Toggle(ShowPointLight, "Draw PointLight");

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
    public bool ShowPolygonLight;
    public bool ShowPointLight;

    public Material LineMaterial;

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (ShowPolygonLight)
        {

            foreach (var b in _gi.PolygonLightBounces)
            {
                foreach (var p in b)
                {
                    for (int i = 0; i < p.PolygonHull.Count; ++i)
                    {
                        Gizmos.DrawLine(p.PolygonHull[i], p.PolygonHull[(i + 1) % p.PolygonHull.Count]);
                    }
                }
            }
        }

        if (ShowPointLight)
        {
            foreach (var l in _gi.SceneLights.Values)
            {
                foreach (var b in l)
                {
                    foreach (var p in b)
                    {
                        Gizmos.DrawSphere(p.Position, 0.01f);
                    }
                }
            }

        }

    }

    void Update()
    {
        
#else
        if (ShowPolygonLight)
        {
            foreach (var b in _gi.PolygonLightBounces)
            {
                foreach (var p in b)
                {
                    for (int i = 0; i < p.PolygonHull.Count; ++i)
                    {
                        GL.Begin(GL.LINES);
                        LineMaterial.SetPass(0);
                        GL.Color(Color.white);
                        GL.Vertex(p.PolygonHull[i]);
                        GL.Vertex(p.PolygonHull[(i + 1) % p.PolygonHull.Count]);
                        GL.End();
                    }
                }
            }
        }
    
#endif

    }
}
