using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TToI : MonoBehaviour {
    public RenderTexture rendertexture;

	// Use this for initialization
	void Start () {
        SaveRenderTextureToPNG(rendertexture, "E:\\Study\\UnityProjects\\LightLayout\\Assets\\Images", "testTexture2D");

    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public bool SaveRenderTextureToPNG(RenderTexture rt, string contents, string pngName)
    {
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D png = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
        png.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        byte[] bytes = png.EncodeToPNG();
        if (!Directory.Exists(contents))
            Directory.CreateDirectory(contents);
        FileStream file = File.Open(contents + "/" + pngName + ".png", FileMode.Create);
        BinaryWriter writer = new BinaryWriter(file);
        writer.Write(bytes);
        file.Close();
        Texture2D.DestroyImmediate(png);
        png = null;
        RenderTexture.active = prev;
        return true;
    }





}
