using UnityEngine;
using System.Collections;

public class RemoteClient
{
    CloudSocket mCloudSocket = null;

    public RemoteClient(CloudSocket cloudSocket)
    {
        mCloudSocket = cloudSocket;
    }
}
