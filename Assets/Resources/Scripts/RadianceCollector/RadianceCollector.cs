using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;

public class RadianceCollector 
{
    static int SIdxCounter = 0;

    protected Camera mSyncedCamera = null;

    protected RenderTexture mRenderTexture = null;

    protected int mIndex = 0;

    public RadianceCollector()
    {
        mIndex = SIdxCounter++;
    }

    public RenderTexture renderTexture
    {
        get
        {
            return mRenderTexture;
        }
    }

    public void Setup()
    {
        mSyncedCamera = Launcher.instance.sceneCamera;
    }

    void PrepareRenderTexture(int width , int height)
    {
        if (mRenderTexture == null || (mRenderTexture.width != width || mRenderTexture.height != height))
        {
            mRenderTexture = new RenderTexture(width, height, 8, RenderTextureFormat.ARGB32);
        }
    }

    void SynchornizeDynamicObjects(ClientObjectAttribute clientObjAttribute)
    {
        Launcher.instance.sceneCamera.transform.position = new Vector3(clientObjAttribute.CameraPosX, clientObjAttribute.CameraPosY, clientObjAttribute.CameraPosZ);
        Launcher.instance.sceneCamera.transform.forward = new Vector3(clientObjAttribute.CameraRotX, clientObjAttribute.CameraRotY, clientObjAttribute.CameraRotZ);

        //Launcher.instance.sceneCamera.transform.rotation = Quaternion.Euler(new Vector3(clientObjAttribute.CameraRotX, clientObjAttribute.CameraRotY, clientObjAttribute.CameraRotZ));

        switch (Launcher.instance.SceneName)
        {
            case "CornellBox":
                {
                    if (Launcher.instance.dynLights.Count > 0)
                    {
                        Launcher.instance.dynLights[0].transform.position = new Vector3(clientObjAttribute.LightPosX, clientObjAttribute.LightPosY, clientObjAttribute.LightPosZ);
                        Launcher.instance.dynLights[0].transform.forward = new Vector3(clientObjAttribute.LightRotX, clientObjAttribute.LightRotY, clientObjAttribute.LightRotZ) - Launcher.instance.dynLights[0].transform.position;
                    }                        
                }
                break;

            case "OfficeInterior":
                {
                    if (Launcher.instance.dynLights.Count > 1)
                    {
                        Launcher.instance.dynLights[0].gameObject.SetActive(clientObjAttribute.LightPosX > 0.0f);
                        Launcher.instance.dynLights[1].gameObject.SetActive(clientObjAttribute.LightPosY > 0.0f);;
                    }  
                }
                break;
        }
        
    }

    protected virtual void Render(int width, int height)
    {

    }

    public virtual void Collect(int width , int height , ClientObjectAttribute clientObjAttribute)
    {
        SynchornizeDynamicObjects(clientObjAttribute);

        PrepareRenderTexture(width, height);

        Render(width, height);
    }

    public byte[] GetRadianceDataInPng()
    {
        RenderTexture.active = mRenderTexture;

        Texture2D png = new Texture2D(mRenderTexture.width, mRenderTexture.height, TextureFormat.RGB24, false);
        png.ReadPixels(new Rect(0, 0, mRenderTexture.width, mRenderTexture.height), 0, 0);
        byte[] dataBytes = png.EncodeToJPG();

        RenderTexture.active = null;

        return dataBytes;
    }

    public static void OutputRt(RenderTexture rt , int idx = 0)
    {
        Texture2D png = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
        png.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        byte[] dataBytes = png.EncodeToPNG();
        string strSaveFile = Application.dataPath + "/rt_" + System.DateTime.Now.Minute + "_" + System.DateTime.Now.Second + "_" + idx + ".png";
        FileStream fs = File.Open(strSaveFile, FileMode.Create);
        fs.Write(dataBytes, 0, dataBytes.Length);
        fs.Close();
        png = null;
    }
}
