using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
//using System.Windows.Media.Imaging;
//using System.Drawing;
public class entropy : MonoBehaviour
{
    public RenderTexture rendertexture;
    public Texture2D testTexture;
    int PixelNum = 0;
    int[] countPixel = new int[256];
    static double entropyValue = 0;
    Texture2D texture;
    Texture2D newTex;

    bool grab;
    public Renderer my;


    void Start()
    {
        //SaveRenderTextureToPNG(rendertexture, "E:\\Study\\UnityProjects\\LightLayout\\Assets\\Images", "testTexture2D");

    }


    void Update()
    {

        if (Input.GetKeyDown(KeyCode.O))
        {
                grab = true;
        }
    }

    private void OnPostRender()
    {
        if (grab)
        {
            for (int i = 0; i < 255; i++)
            {
                countPixel[i] = 0;
            }
            PixelNum = 0;
            entropyValue = 0;
            texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, false);
            texture.Apply();
            newTex = texture;
            TurnToGray();
            if (my != null)
                my.material.mainTexture = texture;
            grab = false;
        }
    }

    void TurnToGray()
    {
        Color currColor;
        int ret = 0;
        for (int i = 0; i < newTex.width; i++)
        {
            for (int j = 0; j < newTex.height; j++)
            {
                currColor = newTex.GetPixel(i, j);
                ret = (int) (currColor.r * 0.299f*255 + currColor.g * 0.587f * 255 + currColor.b * 0.114f * 255);
                PixelNum += 1;
                countPixel[ret] += 1;
            }
        }
        CalculateEntropy();
    }

    void CalculateEntropy()
    {
        double tempP=0,tempE=0;
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

    }
}

