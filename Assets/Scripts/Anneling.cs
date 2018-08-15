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

        //x的移动空间0~-4.53
        const double XMAX = 0;
        const double XMIN = -4.53f;
        //z的移动空间0~3.618
        const double ZMAX = 3.618;
        const double ZMIN = 0;

        //冷却表参数
        int MarkovLength = 10000;          // 马可夫链长度
        double DecayScale = 0.95;          // 衰减参数
        double StepFactor = 0.02;          // 步长因子
        double Temperature = 100;          // 初始温度
        double Tolerance = 1e-8;           // 容差

        double PreX, NextX;                // prior and next value of x
        double PreZ, NextZ;                // prior and next value of y
        double PreBestX, PreBestZ;        // 上一个最优解
        double BestX, BestZ;               // 最终解
        double AcceptPoints = 0.0;         // Metropolis过程中总接受点

        System.Random rnd = new System.Random();


        // Use this for initialization
        void Start()
        {
            //Class1 testClass = new Class1();
            //print(Class1.entropy);
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.L))
            {
                Optimizing();
            }

        }
        
        void Optimizing()
        {
            // 随机选点
            PreX = -XMIN * rnd.NextDouble();
            PreZ = -ZMIN * rnd.NextDouble();
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
                    if (GetEntropy(BestX, BestZ) > GetEntropy(NextX, NextZ))
                    {
                        // 保留上一个最优解
                        PreBestX = BestX;
                        PreBestZ = BestZ;

                        // 此为新的最优解
                        BestX = NextX;
                        BestZ = NextZ;
                    }
                    // 3) Metropolis过程
                    if (GetEntropy(PreX, PreZ) - GetEntropy(NextX, NextZ) > 0)
                    {
                        // 接受, 此处lastPoint即下一个迭代的点以新接受的点开始
                        PreX = NextX;
                        PreZ = NextZ;
                        AcceptPoints++;
                    }
                    else
                    {
                        double change = -1 * (GetEntropy(NextX, NextZ) - GetEntropy(PreX, PreZ)) / Temperature;
                        if (Math.Exp(change) > rnd.NextDouble())
                        {
                            PreX = NextX;
                            PreZ = NextZ;
                            AcceptPoints++;
                        }
                        // 不接受, 保存原解
                    }
                }
            } while (Math.Abs(GetEntropy(BestX, BestZ) - GetEntropy(PreBestX, PreBestZ)) > Tolerance);
            //Console.WriteLine("最小值在点:{0},{1}", BestX, BestZ);
            //Console.WriteLine("最小值为:{0}", GetEntropy(BestX, BestZ));
            TestLight.transform.position = new Vector3((float)BestX, 2.5f, (float)BestZ);
        }

        static double GetEntropy(double x, double z)
        {
            entropy = x - z;
            return entropy;
        }
    }


    
}


