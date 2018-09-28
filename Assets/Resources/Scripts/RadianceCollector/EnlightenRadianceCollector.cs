using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;

public class EnlightenRadianceCollector : RadianceCollector
{
    protected override void Render(int width, int height)
    {
        Shader.EnableKeyword("UNITY_ONLY_OUTPUT_GI");

        Screen.SetResolution(width, height, false);

        mSyncedCamera.targetTexture = mRenderTexture;

        mSyncedCamera.Render();

        //RenderTexture.active = mRenderTexture;

        //OutputRt(mRenderTexture);

        //RenderTexture.active = null;

        Shader.DisableKeyword("UNITY_ONLY_OUTPUT_GI");

        Screen.SetResolution(Launcher.instance.consoleWidth, Launcher.instance.consoleHeight, false);
    }
}
