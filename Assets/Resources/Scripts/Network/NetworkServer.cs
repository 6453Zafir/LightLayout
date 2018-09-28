using UnityEngine;
using System.Collections;
using System.Threading;
using System;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using System.IO;
using System.Collections.Generic;

class NetworkServer : MonoBehaviour
{
    static BaseServer m_Server = null;

    void Awake()
    {
        //m_Server = new TcpServer();
        m_Server = new WebServer();
    }

    public void Startup(string strIpAddress, int port)
    {
        if (m_Server != null)
        {
            m_Server.Startup(strIpAddress, port);
        }
    }

    public void Update()
    {
        if (m_Server != null)
        {
            m_Server.Update();
        }
    }

    public static void SendRawData(CTSMarker ctsMarker, MsgPrefix msgNote, byte[] protoBytes)
    {
        if (m_Server != null)
        {
            m_Server.SendRawData(ctsMarker, msgNote, protoBytes);
        }
    }

    void OnDestroy()
    {
        if (m_Server != null)
        {
            m_Server.Close();
        }
    }
}