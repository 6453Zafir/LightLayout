using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class CTSMarker
{
    public TcpSocket<MsgPrefix> tcpSocket = null;
    public string sessionId = null;

    public CTSMarker(TcpSocket<MsgPrefix> _tcpSocket, string _sessionId)
    {
        tcpSocket = _tcpSocket;
        sessionId = _sessionId;
    }
}

public class ConnectionMgr : MonoBehaviour
{
    Dictionary<CTSMarker, CloudSocket> mTcpToCloudSocketMap = new Dictionary<CTSMarker, CloudSocket>();
    Dictionary<CloudSocket, CTSMarker> mCloudToTcpSocketMap = new Dictionary<CloudSocket, CTSMarker>();

    List<CloudConnection> mCloudConnections = new List<CloudConnection>();

    Dictionary<CTSMarker, CloudConnection> mMarkerToCloudConnectionMap = new Dictionary<CTSMarker, CloudConnection>();

    public void BuildConnection(CloudSocket cloudSocket , CTSMarker ctsMarker)
    {
        CloudConnection cloudConnection = new CloudConnection(cloudSocket);
        mCloudConnections.Add(cloudConnection);

        mMarkerToCloudConnectionMap.Add(ctsMarker, cloudConnection);

        mTcpToCloudSocketMap.Add(ctsMarker, cloudSocket);
        mCloudToTcpSocketMap.Add(cloudSocket, ctsMarker);
    }

    public void RemoveConnection(CTSMarker ctsMarker)
    {
        if (mMarkerToCloudConnectionMap.ContainsKey(ctsMarker))
        {
            mCloudConnections.Remove(mMarkerToCloudConnectionMap[ctsMarker]);

            mMarkerToCloudConnectionMap.Remove(ctsMarker);
        }
    }

    public void ProcessAttributeStream(CTSMarker ctsMarker, byte[] msgStream)
    {
        if (mTcpToCloudSocketMap.ContainsKey(ctsMarker))
        {
            mTcpToCloudSocketMap[ctsMarker].EnqueueAttributeData(new AttributeData(msgStream));

            LCRS.Log("============================================= Enqueue attribute");
        }
    }

	void Update()
    {
        // Update and synchronize each connection
        for (int i = 0; i < mCloudConnections.Count; ++i)
        {
            mCloudConnections[i].Update();
        }

        SyncrhonizeRadiance();

        // Cleanup disconnected connection
        GC();

        // Show some debug information
        ShowInfo();
    }

    void SyncrhonizeRadiance()
    {
        for (int i = 0; i < mCloudConnections.Count; ++i)
        {
            RadianceData radianceData = null;

            while ((radianceData = mCloudConnections[i].cloudSocket.DequeueRadianceData()) != null)
            {
                LCRS.Log("================================ Send data with: " + radianceData.Width + ", " + radianceData.Height);
                NetworkServer.SendRawData(mCloudToTcpSocketMap[mCloudConnections[i].cloudSocket], new XPacket((ushort)eMsgID.S2C_RadianceStream , radianceData.Width , radianceData.Height), radianceData.packedData);
            }

            JsonData jsonData = null;
            while ((jsonData = mCloudConnections[i].cloudSocket.DequeueJsonData()) != null)
            {
                LCRS.Log("================================ Send json data : " + jsonData.MsgID);
                var jsonPack = new JsonPacket((ushort) jsonData.MsgID);
                NetworkServer.SendRawData(mCloudToTcpSocketMap[mCloudConnections[i].cloudSocket], jsonPack, jsonPack.AddMsgHeader(jsonData.Pack()));
            }
        }
    }

    void ShowInfo()
    {
        //if (mCloudConnections.Count > 0)
        {
            Launcher.instance.stats.ShowStats("Remote client count: " + mCloudConnections.Count);
        }
    }

    void GC()
    {
        for (int i = 0; i < mCloudConnections.Count; ++i)
        {
            if (mCloudConnections[i].isDisconnected)
            {
                mCloudConnections.RemoveAt(i);

                break;
            }
        }
    }
}
