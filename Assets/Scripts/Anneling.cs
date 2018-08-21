using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace SimulateAnneling {
    //class Class1 {
    //    public static double entropy = 0;
    //    static double GetEntropy(double x, double z) {
    //        entropy = x-z;
    //        return entropy;
    //    }

    //    [STAThread]
    //    public static void Main(string[] args)
    //    {
    //        //x的移动空间0~-4.53
    //        const double XMAX = 0;
    //        const double XMIN = -4.53f;
    //        //z的移动空间0~3.618
    //        const double ZMAX = 3.618;
    //        const double ZMIN = 0;

    //        //冷却表参数
    //        int MarkovLength = 10000;          // 马可夫链长度
    //        double DecayScale = 0.95;          // 衰减参数
    //        double StepFactor = 0.02;          // 步长因子
    //        double Temperature = 100;          // 初始温度
    //        double Tolerance = 1e-8;           // 容差

    //        double PreX, NextX;                // prior and next value of x
    //        double PreZ, NextZ;                // prior and next value of y
    //        double PreBestX, PreBestZ;        // 上一个最优解
    //        double BestX, BestZ;               // 最终解
    //        double AcceptPoints = 0.0;         // Metropolis过程中总接受点

    //        System.Random rnd = new System.Random();

    //        // 随机选点
    //        PreX = -XMIN * rnd.NextDouble();
    //        PreZ = -ZMIN * rnd.NextDouble();
    //        PreBestX = BestX = PreX;
    //        PreBestZ = BestZ = PreZ;

    //        // 每迭代一次退火一次(降温), 直到满足迭代条件为止
    //        do
    //        {
    //            Temperature *= DecayScale;
    //            AcceptPoints = 0.0;
    //            // 在当前温度T下迭代loop(即MARKOV链长度)次
    //            for (int i = 0; i < MarkovLength; i++)
    //            {
    //                do
    //                {
    //                    NextX = PreX + StepFactor * XMAX * (rnd.NextDouble() - 0.5);
    //                    NextZ = PreZ + StepFactor * ZMAX * (rnd.NextDouble() - 0.5);

    //                } while (!(NextX >= XMIN && NextX <= XMAX && NextZ >= ZMIN && NextZ <= ZMAX));

    //                // 2) 是否全局最优解
    //                if (GetEntropy(BestX, BestZ) > GetEntropy(NextX, NextZ))
    //                {
    //                    // 保留上一个最优解
    //                    PreBestX = BestX;
    //                    PreBestZ = BestZ;

    //                    // 此为新的最优解
    //                    BestX = NextX;
    //                    BestZ = NextZ;
    //                }
    //                // 3) Metropolis过程
    //                if (GetEntropy(PreX, PreZ) - GetEntropy(NextX, NextZ) > 0)
    //                {
    //                    // 接受, 此处lastPoint即下一个迭代的点以新接受的点开始
    //                    PreX = NextX;
    //                    PreZ = NextZ;
    //                    AcceptPoints++;
    //                }
    //                else
    //                {
    //                    double change = -1 * (GetEntropy(NextX, NextZ) - GetEntropy(PreX, PreZ)) / Temperature;
    //                    if (Math.Exp(change) > rnd.NextDouble())
    //                    {
    //                        PreX = NextX;
    //                        PreZ = NextZ;
    //                        AcceptPoints++;
    //                    }
    //                    // 不接受, 保存原解
    //                }
    //            }
    //        } while (Math.Abs(GetEntropy(BestX, BestZ) - GetEntropy(PreBestX, PreBestZ)) > Tolerance);
    //        Console.WriteLine("最小值在点:{0},{1}", BestX, BestZ);
    //        Console.WriteLine("最小值为:{0}", GetEntropy(BestX, BestZ));
    //    }
    //}

    public class Anneling : MonoBehaviour
    {
        public static double entropy = 0;
        public GameObject TestLight;
        public Camera TestCam;
        //x的移动空间0~-4.53
        const double XMAX = 3;
        const double XMIN = 0;
        //z的移动空间0~3.618
        const double ZMAX = 3;
        const double ZMIN = 0;

        //冷却表参数
        int MarkovLength = 500;          // 马可夫链长度10000
        double DecayScale = 0.95;          // 衰减参数0.95
        double StepFactor = 0.2;          // 步长因子0.02
        double Temperature = 100;          // 初始温度
        //double Tolerance = 1e-8;           // 容差
        double Tolerance = 0.001f;           // 容差

        double PreX, NextX;                // prior and next value of x
        double PreZ, NextZ;                // prior and next value of y
        double PreBestX, PreBestZ;        // 上一个最优解
        double BestX, BestZ;               // 最终解
        double AcceptPoints = 0.0;         // Metropolis过程中总接受点

        System.Random rnd = new System.Random();
        
        int PixelNum = 0;
        double entropyValue = 0;
        int[] countPixel = new int[256];
        Texture2D newTex;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.L))
            {
                GetTheCamEntropy(0,0);
                SAOptimizeLighting();
            }
        }
        void SAOptimizeLighting()
        {
            // 随机选点
            //PreX = XMAX * rnd.NextDouble();
            //PreZ = ZMAX * rnd.NextDouble();
            PreX = XMAX * 0.5f;
            PreZ = ZMAX * 0.5f;
            PreBestX = BestX = PreX;
            PreBestZ = BestZ = PreZ;
            // 每迭代一次退火一次(降温), 直到满足迭代条件为止
            do
            {
                Temperature *= DecayScale;
                AcceptPoints = 0.0;
                // 在当前温度T下迭代loop(即MARKOV链长度)次
                for (int i = 0; i < MarkovLength; i++)
                {
                    do
                    {
                        NextX = PreX + StepFactor * XMAX * (rnd.NextDouble() - 0.5);
                        NextZ = PreZ + StepFactor * ZMAX * (rnd.NextDouble() - 0.5);

                    } while (!(NextX >= XMIN && NextX <= XMAX && NextZ >= ZMIN && NextZ <= ZMAX));

                    // 2) 是否全局最优解
                    if (GetTheCamEntropy(BestX, BestZ) > GetTheCamEntropy(NextX, NextZ))
                    {
                        // 保留上一个最优解
                        PreBestX = BestX;
                        PreBestZ = BestZ;
                    }
                    else {
                        // 此为新的最优解
                        BestX = NextX;
                        BestZ = NextZ;
                    }
                    
                    // 3) Metropolis过程
                    if (GetTheCamEntropy(PreX,PreZ) - GetTheCamEntropy(NextX,NextZ) < 0)
                    {
                        // 接受, 此处lastPoint即下一个迭代的点以新接受的点开始
                        PreX = NextX;
                        PreZ = NextZ;
                        AcceptPoints++;
                    }
                    else
                    {
                        double change = -1 * (GetTheCamEntropy(NextX,NextZ) - GetTheCamEntropy(PreX,PreZ)) / Temperature;
                        if (Math.Exp(change) > rnd.NextDouble())
                        {

                            // 以一定的概率接受新的解
                            PreX = NextX;
                            PreZ = NextZ;
                            AcceptPoints++;
                        }
                        else {
                            // 不接受, 保存原解
                        }
                    }
                }
            } while (Math.Abs(GetTheCamEntropy(BestX,BestZ) - GetTheCamEntropy(PreBestX,PreBestZ)) > Tolerance);
            TestLight.transform.position = new Vector3((float)BestX, 2.5f, (float)BestZ);
        }



        double GetTheCamEntropy(double x,double z)
        {
            TestLight.transform.position = new Vector3((float)x, 2.5f, (float)z);
            TestCam.Render();
            for (int i = 0; i < 255; i++)
            {    
                countPixel[i] = 0;
            }
            PixelNum = 0;
            entropyValue = 0;
            newTex = ConvertRTtoT2D(TestCam.targetTexture);
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
            return tex;
        }

    }

   
}


