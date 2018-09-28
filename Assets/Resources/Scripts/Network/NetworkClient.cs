using UnityEngine;
using System.Collections;
using System.Threading;
using System;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using System.IO;
using System.Collections.Generic;

class NetworkClient : MonoBehaviour
{
    TcpSocket<XPacket> mClientSocket = null;

    string m_IpAddress;
    int m_Port;

    public void Startup(string strIpAddress , int port)
    {
        Connect(strIpAddress, port);
    }

    void OnConnectedCallback()
    {
        LCRS.Log("Server connected: " + m_IpAddress + ", " + m_Port);

        StartCoroutine(SyncClientAttribute());
    }

    public void Connect(string strIpAddress, int port)
    {
        m_IpAddress = strIpAddress;
        m_Port = port;

        mClientSocket = new TcpSocket<XPacket>();

        mClientSocket.Connect(m_IpAddress, m_Port, OnConnectedCallback, HandleReceiveData);

        Debug.Log("Net# Connect to " + strIpAddress + ": " + port);
    }

    public void Disconnect()
    {
        if (mClientSocket != null)
        {
            mClientSocket.Close();
        }
    }

    public void Update()
    {
        if (mClientSocket != null)
        {
            mClientSocket.Run();
        }

        InteractDynObject();
    }

    public void SendRawData(XPacket msgNote, byte[] protoBytes)
    {
        msgNote.msgSize = (ushort)(mClientSocket.MsgPrefixLength + (protoBytes != null ? protoBytes.Length : 0));

        mClientSocket.SendData(msgNote, protoBytes);
    }

    void HandleReceiveData(TcpSocket<XPacket> srcSocket, XPacket msgNote, MemoryStream msgStream)
    {
        if (msgNote.MsgID == (ushort)eMsgID.S2C_RadianceStream)
        {
            LCRS.Log("======================================= Receive radiance from sever: " + msgNote.MsgID + ", " + msgNote.msgSize);

            Launcher.instance.stats.ShowRadianceTexture(msgNote.ScreenWidth , msgNote.ScreenHeight , msgStream.ToArray());
        }
        else
        {
            // Common message
        }
    }

    void InteractDynObject()
    {
        Vector3 delteaVec = Vector3.zero;

        float epsilon = 1.0f * Time.deltaTime;

        if (Input.GetKey(KeyCode.R))
        {
            delteaVec.x += epsilon;
        }
        if (Input.GetKey(KeyCode.T))
        {
            delteaVec.x -= epsilon;
        }

        if (Input.GetKey(KeyCode.F))
        {
            delteaVec.y += epsilon;
        }
        if (Input.GetKey(KeyCode.G))
        {
            delteaVec.y -= epsilon;
        }

        if (Input.GetKey(KeyCode.V))
        {
            delteaVec.z += epsilon;
        }
        if (Input.GetKey(KeyCode.B))
        {
            delteaVec.z -= epsilon;
        }

        //Launcher.instance.dynLight.transform.position += delteaVec;
    }

    IEnumerator SyncClientAttribute()
    {
        while (true)
        {
            ClientObjectAttribute clientObjAttribute = new ClientObjectAttribute();

            clientObjAttribute.CameraPosX = Launcher.instance.sceneCamera.transform.position.x;
            clientObjAttribute.CameraPosY = Launcher.instance.sceneCamera.transform.position.y;
            clientObjAttribute.CameraPosZ = Launcher.instance.sceneCamera.transform.position.z;

            clientObjAttribute.CameraRotX = Launcher.instance.sceneCamera.transform.rotation.eulerAngles.x;
            clientObjAttribute.CameraRotY = Launcher.instance.sceneCamera.transform.rotation.eulerAngles.y;
            clientObjAttribute.CameraRotZ = Launcher.instance.sceneCamera.transform.rotation.eulerAngles.z;

            //clientObjAttribute.LightPosX = Launcher.instance.dynLight.transform.position.x;
            //clientObjAttribute.LightPosY = Launcher.instance.dynLight.transform.position.y;
            //clientObjAttribute.LightPosZ = Launcher.instance.dynLight.transform.position.z;

            SendRawData(new XPacket((ushort)eMsgID.C2S_AttributeStream, 300, 300), MsgNoteUtils.StructToBytes(clientObjAttribute));

            yield return new WaitForSeconds(0.1f);
        }

        yield return new AsyncOperation();
    }
}