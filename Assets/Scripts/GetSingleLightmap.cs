using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GetSingleLightmap : MonoBehaviour {

    public GameObject TestGO;

    public RenderTexture RT;
    public Texture2D Lightmap;
    public Shader GetSingleLightmapShader;
    // Use this for initialization
    void Start () {
        RT = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32);
        RT.enableRandomWrite = true;
        RT.Create();
    }

    //单个物体包围盒
    public Bounds CalcAABB(List<MeshRenderer> meshes)
    {
        var boxColliders = meshes.Select(x => x.bounds).ToList();
        var aabb = boxColliders[0];
        foreach (var obj in boxColliders)
        {
            aabb.Encapsulate(obj);
        }
        return aabb;
    }
    //所有子物体包围盒
    public Bounds CalcBounds(GameObject GO)
    {

        Vector3 center = Vector3.zero;

        foreach (Transform child in GO.transform)
        {
            center += child.GetComponent<MeshRenderer>().bounds.center;
        }
        center /= GO.transform.childCount; //center is average center of children

        //Now you have a center, calculate the bounds by creating a zero sized 'Bounds', 
        Bounds bounds = new Bounds(center, Vector3.zero);

        foreach (Transform child in GO.transform)
        {
            bounds.Encapsulate(child.GetComponent<MeshRenderer>().bounds);
        }
        return bounds;
    }


// Update is called once per frame
void Update () {
        if (TestGO == null) return;
        var allRenderer = GameObject.FindObjectsOfType<MeshRenderer>();

        foreach (var meshRenderer in allRenderer)
        {
            meshRenderer.gameObject.SetActive(false);
        }

        TestGO.SetActive(true);


        var go = GameObject.Find("GBufferCamera");
        Camera gBufferCamera = null;
        if (go == null)
            gBufferCamera = new GameObject("GBufferCamera").AddComponent<Camera>();
        else
            gBufferCamera = go.GetComponent<Camera>();
       // var aabb = CalcAABB(new List<MeshRenderer>() { TestGO.GetComponentInChildren<MeshRenderer>() });
        //var aabb = CalcAABB(new List<MeshRenderer>() { TestGO.GetComponentsInChildren<MeshRenderer>()});
        var aabb = CalcBounds(TestGO);
        var maxExtend = Mathf.Max(aabb.extents.x, aabb.extents.y, aabb.extents.z); ;
        gBufferCamera.orthographic = true;
        gBufferCamera.aspect = 1;
        gBufferCamera.orthographicSize = maxExtend * 2;
        gBufferCamera.allowMSAA = false;
        gBufferCamera.allowHDR = false;

        gBufferCamera.enabled = false;
        gBufferCamera.clearFlags = CameraClearFlags.SolidColor;
        gBufferCamera.backgroundColor = Color.clear;
        gBufferCamera.farClipPlane = maxExtend * 2;
        var cameraTransform = gBufferCamera.transform;
        cameraTransform.position = aabb.center;
        cameraTransform.forward = aabb.extents.x <= aabb.extents.y
            ? (aabb.extents.x <= aabb.extents.z ? new Vector3(1, 0, 0) : new Vector3(0, 0, 1))
            : (aabb.extents.y <= aabb.extents.z ? new Vector3(0, 1, 0) : new Vector3(0, 0, 1));
        cameraTransform.position = aabb.center - cameraTransform.forward * maxExtend;
        gBufferCamera.targetTexture = RT;
        Shader.SetGlobalTexture("_Lightmap", Lightmap);
        Shader.SetGlobalVector("_LightmspST", TestGO.GetComponentInChildren<MeshRenderer>().lightmapScaleOffset);
        //Shader.SetGlobalVector("_LightmspST", TestGO.GetComponent<MeshRenderer>().lightmapScaleOffset);
        gBufferCamera.RenderWithShader(GetSingleLightmapShader, "");
    }
}
