using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CloudConnection
{
    LocalServer mLocalServer = null;

    RemoteClient mRemoteClient = null;

    CloudSocket mCloudSocket = null;

    public CloudConnection(CloudSocket cloudSocket)
    {
        mCloudSocket = cloudSocket;

        mLocalServer = new LocalServer(cloudSocket);

        mRemoteClient = new RemoteClient(cloudSocket);
    }

    public CloudSocket cloudSocket
    {
        get
        {
            return mCloudSocket;
        }
    }

    public bool isDisconnected
    {
        get
        {
            return false;
        }
    }

    public void Update()
    {
        mLocalServer.Update();
    }
}
