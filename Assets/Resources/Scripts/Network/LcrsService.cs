using UnityEngine;
using System.Collections;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Collections.Generic;

public class LcrsService : WebSocketService
{
    Dictionary<string, CTSMarker> m_MarkerDic = new Dictionary<string, CTSMarker>();

    string mUserEndPointInfo = string.Empty;

    protected override void OnOpen()
    {
        //base.OnOpen();

        CTSMarker marker = new CTSMarker(null, this.ID);

        m_MarkerDic.Add(this.ID, marker);

        Launcher.instance.connectionMgr.BuildConnection(new CloudSocket(), marker);

        mUserEndPointInfo = this.Context.UserEndPoint.ToString();

        LCRS.Log("========================================================== Connect client: " + this.ID + ", " + this.Context.UserEndPoint);

        Launcher.instance.stats.Log("Build connection with " + this.Context.UserEndPoint);
    }

    protected override void OnMessage(MessageEventArgs args)
    {
        // Process the message
        Launcher.instance.connectionMgr.ProcessAttributeStream(m_MarkerDic[this.ID] , args.RawData);

    }

    protected override void OnClose(CloseEventArgs e)
    {
        Launcher.instance.stats.Log("Close connection " + mUserEndPointInfo);

        Launcher.instance.connectionMgr.RemoveConnection(m_MarkerDic[this.ID]);
    }

    protected override void OnError(WebSocketSharp.ErrorEventArgs e)
    {
        Launcher.instance.stats.Log("Close connection " + mUserEndPointInfo+ " " + e.Message);

        Launcher.instance.connectionMgr.RemoveConnection(m_MarkerDic[this.ID]);
    }
}
