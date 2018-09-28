using System;
using UnityEngine;
using System.Collections;
using System.Text;
using LitJson;
using Object = UnityEngine.Object;

public class LocalServer
{
    CloudSocket mCloudSocket = null;

    RadianceCollector mRadianceCollector = null;

    int mWebClientExchangeCode = 2000;

    public LocalServer(CloudSocket cloudSocket)
    {
        mCloudSocket = cloudSocket;

        switch (Launcher.instance.GIMode)
        {
            case eGiMode.Enlighten:
                mRadianceCollector = new EnlightenRadianceCollector();
                break;

            case eGiMode.RSM:
                break;

            case eGiMode.LPV:
                break;
        }

        mRadianceCollector.Setup();
    }

    Utils.int2 ExtractScreenSize(ClientObjectAttribute clientData)
    {
        int height = (int)clientData.Param % mWebClientExchangeCode;
        int weight = (int)clientData.Param / mWebClientExchangeCode;

        return new Utils.int2(weight, height);
    }

    public void Update()
    {
        if (mRadianceCollector != null)
        {
            AttributeData attributeData = null;
            while ((attributeData = mCloudSocket.DequeueAttributeData()) != null)
            {
                var msgHeader = MsgNoteUtils.GetMessageHead(attributeData.RawData);
                LCRS.Log("msgHeader: " + msgHeader);
                if (msgHeader ==  eMsgID.C2S_DGI_Init)
                {
                    Baker.Instance.Init();
                    Baker.Instance.SetLightMap();
                    //不需要传ST了：导出json的时候已经计算好了
                    //mCloudSocket.EnqueueJsonData(new LightmapSTJsonData(Object.FindObjectsOfType<MeshRenderer>(), eMsgID.S2C_Json_LightmapST));
                    mCloudSocket.EnqueueJsonData(new LightmapArrayJsonData(eMsgID.S2C_Json_LightmapArray));
                }
                else if (msgHeader == eMsgID.C2D_DGI_RecalcLightmap)
                {
                    Baker.Instance.Init();
                    Baker.Instance.SetLightMap();
                    mCloudSocket.EnqueueJsonData(new LightmapArrayJsonData(eMsgID.S2C_Json_LightmapArray));
                }
                else if (msgHeader == eMsgID.C2S_Json_UpdateObjectPosition)
                {
                    byte[] clientObjBytes = MsgNoteUtils.GetMessageBody(attributeData.RawData);
                    string jsonStr = Encoding.UTF8.GetString(clientObjBytes);

                    JsonMapper.RegisterExporter<float>((obj, writer) => writer.Write(Convert.ToDouble(obj)));
                    JsonMapper.RegisterImporter<double, float>(input => Convert.ToSingle(input));
                    ClientSyncObjectData syncObject = JsonMapper.ToObject<ClientSyncObjectData>(jsonStr);
                    Baker.Instance.SyncObjectPosition(syncObject);
                    Baker.Instance.Init();
                    Baker.Instance.SetLightMap();
                    mCloudSocket.EnqueueJsonData(new LightmapArrayJsonData(eMsgID.S2C_Json_LightmapArray));

                }
                else
                {
                    ClientObjectAttribute clientObjAttribute = (ClientObjectAttribute)MsgNoteUtils.BytesToStruct(attributeData.RawData, typeof(ClientObjectAttribute));

                    int height = (int)clientObjAttribute.Param % mWebClientExchangeCode;
                    int width = (int)clientObjAttribute.Param / mWebClientExchangeCode;

                    LCRS.Log("===================================================== Log: " + width + "==324234234235^%$^%@#R@3: " + (ushort)clientObjAttribute.LightPosZ);

                  //  mRadianceCollector.Collect(width, height, clientObjAttribute);

                  //  mCloudSocket.EnqueueRadianceData(new RadianceData((ushort)clientObjAttribute.LightPosZ, (ushort)width, (ushort)height, mRadianceCollector.GetRadianceDataInPng()));
                }
               
            }
        }
    }
}
