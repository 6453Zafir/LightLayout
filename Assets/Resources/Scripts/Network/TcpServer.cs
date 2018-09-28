using UnityEngine;
using System.Collections;
using System.Threading;
using System;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using System.IO;
using System.Collections.Generic;

class TcpServer : BaseServer
{
    string m_IpAddress;
    int m_Port;

    Socket mListenServerSocket = null;

    List<TcpSocket<MsgPrefix>> mClientSockets = new List<TcpSocket<MsgPrefix>>();

    public override void Startup(string strIpAddress, int port)
    {
        m_IpAddress = strIpAddress;
        m_Port = port;
        mListenServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        mListenServerSocket.Bind(new IPEndPoint(IPAddress.Parse(m_IpAddress), m_Port));
        mListenServerSocket.Listen(32);
        Thread listenThread = new Thread(ListenClientConnect);
        listenThread.Start();
    }

    void ListenClientConnect()
    {
        while (true)
        {
            Socket clientSocket = mListenServerSocket.Accept();

            if (clientSocket != null)
            {
                //mClientSockets.Add(new TcpSocket<XPacket>(clientSocket, HandleReceiveData));

                TcpSocket<MsgPrefix> tcpSocket = new TcpSocket<MsgPrefix>(clientSocket, HandleReceiveData);
                CloudSocket cloudSocket = new CloudSocket();

                mClientSockets.Add(tcpSocket);

                // Add new Cloud connection
                Launcher.instance.connectionMgr.BuildConnection(cloudSocket, new CTSMarker(tcpSocket , null));
            }
        }
    }

    public override void Update()
    {
        for (int i = 0; i < mClientSockets.Count; ++i)
        {
            mClientSockets[i].Run();
        }

        Launcher.instance.stats.ShowStats("Client Nums: " + mClientSockets.Count);
    }

    public override void SendRawData(CTSMarker ctsMarker, MsgPrefix msgNote, byte[] protoBytes)
    {
        msgNote.msgSize = (ushort)(ctsMarker.tcpSocket.MsgPrefixLength + (protoBytes != null ? protoBytes.Length : 0));

        ctsMarker.tcpSocket.SendData(msgNote, protoBytes);

        LCRS.Log("================================================= Send data to client");
    }

    public void HandleReceiveData(TcpSocket<MsgPrefix> tcpSocket, MsgPrefix msgNote, MemoryStream msgStream)
    {
        if (msgNote.MsgID == (ushort)eMsgID.C2S_AttributeStream)
        {
            Launcher.instance.connectionMgr.ProcessAttributeStream(new CTSMarker(tcpSocket , null), msgStream.ToArray());
        }
        else
        {
            // Common message 
        }
    }

    public override void Close()
    {

    }
}