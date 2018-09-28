using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GetLightmap : MonoBehaviour
{

    public GameObject GO;

    public RenderTexture RT;
    public Texture2D Lightmap;
    public Shader GetSingleLightmap;
	// Use this for initialization
	void Start () {
	    RT = new RenderTexture(1024,1024,24, RenderTextureFormat.ARGB32);
	    RT.enableRandomWrite = true;
	    RT.Create();

	}
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

    // Update is called once per frame
    void Update()
    {
        if (GO == null) return;
        var allRenderer = GameObject.FindObjectsOfType<MeshRenderer>();

        foreach (var meshRenderer in allRenderer)
        {
            meshRenderer.gameObject.SetActive(false);
        }

        GO.SetActive(true);


        var go = GameObject.Find("GBufferCamera");
        Camera gBufferCamera = null;
        if (go == null)
            gBufferCamera = new GameObject("GBufferCamera").AddComponent<Camera>();
        else
            gBufferCamera = go.GetComponent<Camera>();
        var aabb = CalcAABB(new List<MeshRenderer>() {GO.GetComponent<MeshRenderer>()});
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
        Shader.SetGlobalVector("_LightmspST", GO.GetComponent<MeshRenderer>().lightmapScaleOffset);
        gBufferCamera.RenderWithShader(GetSingleLightmap, "");
    }
}
