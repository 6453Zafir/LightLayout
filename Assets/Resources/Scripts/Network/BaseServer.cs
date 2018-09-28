using UnityEngine;
using System.Collections;
using System.Threading;
using System;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using System.IO;
using System.Collections.Generic;

public class BaseServer
{
    string m_IpAddress;
    int m_Port;

    public virtual void Startup(string strIpAddress, int port)
    {

    }

    public virtual void Update()
    {

    }

    public virtual void SendRawData(CTSMarker ctsMarker, MsgPrefix msgNote, byte[] protoBytes)
    {

    }

    public virtual void Close()
    {

    }
}