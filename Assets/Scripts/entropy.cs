using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
//using System.Windows.Media.Imaging;
//using System.Drawing;
public class entropy : MonoBehaviour
{
    public Camera TestCam;
    public GameObject TestLight;
    int PixelNum = 0;
    int[] countPixel = new int[256];
    static double entropyValue = 0;
    Texture2D texture;
    Texture2D newTex;

    bool turnOn = false;
    bool isSwitch = false;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            TestLight.SetActive(false);

            StartCoroutine(waitForRender());
           // CatchCamSightAndTurnToGray();
        }
        if (Input.GetKeyDown(KeyCode.C))
        {

            TestLight.SetActive(true);

            StartCoroutine(waitForRender());
            //CatchCamSightAndTurnToGray();
        }
    }

    //private void OnPreRender()
    //{
    //    if (isSwitch)
    //    {
    //        if (turnOn)
    //        {
    //            TestLight.SetActive(true);
    //        }
    //        else
    //        {
    //            TestLight.SetActive(false);
    //        }
    //        turnOn = !turnOn;
    //    }
    //    isSwitch = false;
    //}
   


    IEnumerator waitForRender() {
        yield return new WaitForSeconds(0.01f);
        CatchCamSightAndTurnToGray();
    }

    double CatchCamSightAndTurnToGray()
    {

        newTex = ConvertRTtoT2D(TestCam.targetTexture);

        for (int i = 0; i < 255; i++)
        {
            countPixel[i] = 0;
        }
        PixelNum = 0;
        entropyValue = 0;
        RenderTexture rt;
        Color currColor;
        int ret = 0;
        for (int i = 0; i < newTex.width; i++)
        {
            for (int j = 0; j < newTex.height; j++)
            {
                currColor = newTex.GetPixel(i, j);
                ret = (int)(currColor.r * 0.299f * 255 + currColor.g * 0.587f * 255 + currColor.b * 0.114f * 255);
                PixelNum += 1;
                countPixel[ret] += 1;
            }
        }

        double tempP = 0, tempE = 0;
        for (int i = 0; i < 255; i++)
        {
            tempP = (float)countPixel[i] / (float)PixelNum;
            if (tempP != 0)
            {
                tempE = tempP * Math.Log(tempP);
                entropyValue += tempE;
            }
        }

        print("the entropyValue is : " + -entropyValue);
        return -entropyValue;
    }
    

    Texture2D ConvertRTtoT2D(RenderTexture rt)
    {
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        return tex;
    }
}

