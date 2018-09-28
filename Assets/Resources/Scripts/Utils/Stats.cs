using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class Stats : MonoBehaviour 
{
    public Text statsText = null;
    public Text logText = null;

    public Image UIImage = null;

    public GameObject statsRoot = null;

    public Text sceneText = null;

    Texture2D mRadianceTexture = null;

    const int MaxLogNums = 30;
    List<string> mLogBuffer = new List<string>();

    string mLogString = string.Empty;

    bool mNeedToUpdateLog = false;

	// Use this for initialization
	void Start () 
    {
        Show(false);

        if (sceneText != null)
        {
            sceneText.text = Launcher.instance.SceneName + ":" + Launcher.instance.serverPort;
        }
	}
	
	// Update is called once per frame
	void Update () 
    {
        if (mNeedToUpdateLog && logText != null)
        {
            mNeedToUpdateLog = false;

            logText.text = mLogString;
        }
	}

    public void Show(bool bShow)
    {
        if (statsRoot != null)
        {
            statsRoot.SetActive(bShow);
        }
    }

    void OnGUI()
    {
        if (mRadianceTexture != null)
        {
            GUI.DrawTexture(new Rect(0, 0, mRadianceTexture.width, mRadianceTexture.height), mRadianceTexture);
        }
    }

    public void ShowStats(string strString)
    {
        if (statsText != null)
        {
            statsText.text = strString;
        }
    }

    public void Log(string strLog)
    {
        mLogBuffer.Add(strLog);

        if (mLogBuffer.Count > MaxLogNums)
        {
            mLogBuffer.RemoveAt(mLogBuffer.Count - 1);
        }

        mLogString = string.Empty;

        for (int i = 0; i < mLogBuffer.Count; ++i)
        {
            mLogString += mLogBuffer[i] + "\n";
        }

        mNeedToUpdateLog = true;
    }

    public void ShowRadianceTexture(ushort width , ushort height , byte[] dataBytes)
    {
        if (mRadianceTexture == null)
        {
            mRadianceTexture = new Texture2D(width, height, TextureFormat.ARGB32, false, false);
        }

        mRadianceTexture.LoadImage(dataBytes, false);
        mRadianceTexture.Apply();
    }
}
