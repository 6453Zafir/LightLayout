using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
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
        /// <summary>
        /// 测试用单一点光源
        /// </summary>
        public GameObject TestLight;
        /// <summary>
        /// 获得单一视角截图的camera
        /// </summary>
        public Camera TestCam;
        /// <summary>
        /// 获得cubemap的camera
        /// </summary>
        public Camera CenterCam;
        /// <summary>
        /// 基于CenterCam得出的cubemap
        /// </summary>
        public Cubemap centerCubemap;
        /// <summary>
        /// cubemap每个面的贴图宽高
        /// </summary>
        int textureWidth=256, textureHeight = 256;
        Texture2D up, bottom, left, right, back, forward;
        double up_e, bottom_e, left_e, right_e, back_e, forward_e ;
        /// <summary>
        /// cubemap各面权重
        /// </summary>
        double up_w = 0.05f, bottom_w = 0.35f, left_w = 0.15f, right_w = 0.15, forward_w =0.15f, back_w = 0.15f;

        public BakerWithIsm BWIBaker;

        public GetSingleLightmap GSL;
        public GameObject FatherGO;
        public RenderTexture RT;
        public Texture2D Lightmap;
        public Shader GetSingleLightmapShader;

        public Texture2D LD;

        public GameObject bed, besideTable, chest, paintings, curtains, TVStand;
        double bed_w = 0.15f, besideTable_w = 0.15f, chest_w = 0.15f, paintings_w = 0.15, curtains_w = 0.15f, TVStand_w = 0.15f;
        double bed_e, besideTable_e, chest_e, paintings_e, curtains_e, TVStand_e;

        // private string LightMapPath = "Resources/Scene/bedroom";
        //x的移动空间0~-4.53
        const double XMAX = 3;
        const double XMIN = 0;
        //z的移动空间0~3.618
        const double ZMAX = 3;
        const double ZMIN = 0;

        //冷却表参数
        int MarkovLength = 5;          // 马可夫链长度10000
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
        double tempEntropy;

        Color currColor;
        private void Start()
        {
            forward = new Texture2D(textureWidth, textureHeight);
            back = new Texture2D(textureWidth, textureHeight);
            up = new Texture2D(textureWidth, textureHeight);
            bottom = new Texture2D(textureWidth, textureHeight);
            left = new Texture2D(textureWidth, textureHeight);
            right = new Texture2D(textureWidth, textureHeight);



            RT = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32);
            RT.enableRandomWrite = true;
            RT.Create();
        }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SAOptimizeLighting(1);
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SAOptimizeLighting(2);
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                //Lightmapping.realtimeGI = false;
                //Lightmapping.bakedGI = true;
                SAOptimizeLighting(3);
            }
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                SAOptimizeLighting(4);
            }
            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                SAOptimizeLighting(5);
                
            }
        }

        void SAOptimizeLighting(int funcNum)
        {
            float currentTime = Time.realtimeSinceStartup;
            // 随机选点
            // PreX = XMAX * rnd.NextDouble();
            // PreZ = ZMAX * rnd.NextDouble();
            // 改为选中点
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
                    if (GetEntropy(funcNum, BestX, BestZ) > GetEntropy(funcNum, NextX, NextZ))
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
                    if (GetEntropy(funcNum, PreX,PreZ) - GetEntropy(funcNum, NextX,NextZ) < 0)
                    {
                        // 接受, 此处lastPoint即下一个迭代的点以新接受的点开始
                        PreX = NextX;
                        PreZ = NextZ;
                        AcceptPoints++;
                    }
                    else
                    {
                        double change = -1 * (GetEntropy(funcNum, NextX,NextZ) - GetEntropy(funcNum, PreX,PreZ)) / Temperature;
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
            } while (Math.Abs(GetEntropy(funcNum, BestX,BestZ) - GetEntropy(funcNum, PreBestX,PreBestZ)) > Tolerance);
            TestLight.transform.position = new Vector3((float)BestX, 2.5f, (float)BestZ);

            if (funcNum == 5) {
                GSL.showAllFurnitures();
                BWIBaker.Init();
                BWIBaker.SaveLightmapBeforePostprocessing();
                BWIBaker.SetLightMap();
            }
            print("function " + funcNum + " took " + (Time.realtimeSinceStartup - currentTime) + "s, The final entropy value is: " +(-entropyValue));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="FunNum">图像来源 1：矩形 2：全景图 3：lightMap</param>
        /// <param name="x"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        double GetEntropy(int FunNum, double x, double z) {

            switch (FunNum)
            {
                case 1:
                    //相机的矩形视角图
                    TestLight.transform.position = new Vector3((float)x, 2.5f, (float)z);
                    TestCam.Render();
                    newTex = ConvertRTtoT2D(TestCam.targetTexture);
                    return EntropyPerT(newTex);
                case 2:
                    //位于场景中间相机视角的全景图
                    TestLight.transform.position = new Vector3((float)x, 2.5f, (float)z);
                    CenterCam.Render();
                    newTex = ConvertRTtoT2D(CenterCam.targetTexture);
                    return EntropyPerT(newTex);
                case 3:
                    BWIBaker.Init();
                    BWIBaker.SaveLightmapBeforePostprocessing();
                    BWIBaker.SetLightMap();
                    return EntropyPerT(LD);
                case 4:
                    {  
                        //给cubemap的不同面加权
                        TestLight.transform.position = new Vector3((float)x, 2.5f, (float)z);
                        CenterCam.Render();
                        CenterCam.RenderToCubemap(centerCubemap);
                        Color[] CubemapColors = centerCubemap.GetPixels(CubemapFace.PositiveZ);
                        forward.SetPixels(CubemapColors);
                        forward_e = EntropyPerT(forward);
                        CubemapColors = centerCubemap.GetPixels(CubemapFace.PositiveX);
                        right.SetPixels(CubemapColors);
                        right_e = EntropyPerT(right);
                        CubemapColors = centerCubemap.GetPixels(CubemapFace.PositiveY);
                        up.SetPixels(CubemapColors);
                        up_e = EntropyPerT(up);
                        CubemapColors = centerCubemap.GetPixels(CubemapFace.NegativeZ);
                        back.SetPixels(CubemapColors);
                        back_e = EntropyPerT(back);
                        CubemapColors = centerCubemap.GetPixels(CubemapFace.NegativeX);
                        left.SetPixels(CubemapColors);
                        left_e = EntropyPerT(left);
                        CubemapColors = centerCubemap.GetPixels(CubemapFace.NegativeY);
                        bottom.SetPixels(CubemapColors);
                        bottom_e = EntropyPerT(bottom);

                        entropyValue = forward_e * forward_w + back_e * back_w + left_e * left_w + right_e * right_w + up_e * up_w + bottom_e * bottom_w;
                        return -entropyValue;
                    }
                case 5:
                    BWIBaker.Init();
                    BWIBaker.SaveLightmapBeforePostprocessing();
                    BWIBaker.SetLightMap();
                    bed_e= EntropyPerT(GSL.getSingleLightmap(bed));
                    besideTable_e= EntropyPerT(GSL.getSingleLightmap(besideTable));
                    chest_e= EntropyPerT(GSL.getSingleLightmap(chest));
                    paintings_e= EntropyPerT(GSL.getSingleLightmap(paintings));
                    curtains_e= EntropyPerT(GSL.getSingleLightmap(curtains));
                    TVStand_e= EntropyPerT(GSL.getSingleLightmap(TVStand));
                    entropyValue = bed_e * bed_w + besideTable_e * besideTable_w + chest_e * chest_w + paintings_e * paintings_w + curtains_e * curtains_w + TVStand_e * TVStand_w;
                    return entropyValue;
                default:
                    return 0;
            }
        }

        //获得某个texture的熵
        double EntropyPerT(Texture2D tex)
        {
            PixelNum = 0;
            tempEntropy = 0;
            for (int i = 0; i < 255; i++)
            {
                countPixel[i] = 0;
            }
            int ret = 0;
            for (int i = 0; i < tex.width; i++)
            {
                for (int j = 0; j < tex.height; j++)
                {
                    currColor = tex.GetPixel(i, j);
                    ret = (int)(currColor.r * 0.299f * 255 + currColor.g * 0.587f * 255 + currColor.b * 0.114f * 255);

                    PixelNum += 1;
                    if(ret>=0&&ret<=255)
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
                    tempEntropy += tempE;
                }
            }
            return tempEntropy;
        }

        //获得单一传入家具的lightmap
        Texture2D getSingleLightmap(GameObject ObToLightmap)
        {

            if (ObToLightmap == null) return null;
            /*新建空的lightmap以写入
            var LightMapTexture = new Texture2D(512, 512, TextureFormat.ARGB32, false);
            var fillColorArray = LightMapTexture.GetPixels();

            for (var i = 0; i < fillColorArray.Length; ++i)
            {
                fillColorArray[i] = Color.black;
            }

            LightMapTexture.SetPixels(fillColorArray);

            LightMapTexture.Apply();
            */

            var allRenderer = GameObject.FindObjectsOfType<MeshRenderer>();
            foreach (var meshRenderer in allRenderer)
            {
                meshRenderer.gameObject.SetActive(false);
            }
            ObToLightmap.SetActive(true);
            for (int i = 0; i < ObToLightmap.transform.childCount; i++)
            {
                ObToLightmap.transform.GetChild(i).gameObject.SetActive(true);
            }

            var go = GameObject.Find("GBufferCamera");
            Camera gBufferCamera = null;
            if (go == null)
                gBufferCamera = new GameObject("GBufferCamera").AddComponent<Camera>();
            else
                gBufferCamera = go.GetComponent<Camera>();
            // var aabb = CalcAABB(new List<MeshRenderer>() { ObToLightmap.GetComponent<MeshRenderer>() });
            var aabb = CalcBounds(FatherGO);
            var maxExtend = Mathf.Max(aabb.extents.x, aabb.extents.y, aabb.extents.z); ;
            gBufferCamera.orthographic = true;
            //gBufferCamera.aspect = 1;
            //gBufferCamera.orthographicSize = maxExtend * 2;
            gBufferCamera.allowMSAA = false;
            gBufferCamera.allowHDR = false;

            gBufferCamera.enabled = false;
            gBufferCamera.clearFlags = CameraClearFlags.SolidColor;
            gBufferCamera.backgroundColor = Color.clear;
            //gBufferCamera.farClipPlane = maxExtend * 2;
            var cameraTransform = gBufferCamera.transform;
            //cameraTransform.position = aabb.center;
            //cameraTransform.forward = aabb.extents.x <= aabb.extents.y
            //    ? (aabb.extents.x <= aabb.extents.z ? new Vector3(1, 0, 0) : new Vector3(0, 0, 1))
            //    : (aabb.extents.y <= aabb.extents.z ? new Vector3(0, 1, 0) : new Vector3(0, 0, 1));
            //cameraTransform.position = aabb.center - cameraTransform.forward * maxExtend;
            gBufferCamera.targetTexture = RT;
            Shader.SetGlobalTexture("_Lightmap", Lightmap);
            //Shader.SetGlobalVector("_LightmspST", ObToLightmap.GetComponent<MeshRenderer>().lightmapScaleOffset);
            gBufferCamera.RenderWithShader(GetSingleLightmapShader, "");
            return ConvertRTtoT2D(RT);
        }

        //所有子物体包围盒
        public Bounds CalcBounds(GameObject GO)
        {

            Vector3 center = Vector3.zero;

            foreach (Transform child in GO.transform)
            {
                center += child.GetComponent<MeshRenderer>().bounds.center;
            }
            center /= GO.transform.childCount; //center is average center of children

            //Now you have a center, calculate the bounds by creating a zero sized 'Bounds', 
            Bounds bounds = new Bounds(center, Vector3.zero);

            foreach (Transform child in GO.transform)
            {
                bounds.Encapsulate(child.GetComponent<MeshRenderer>().bounds);
            }
            return bounds;
        }

        //将renderTexture转为Texture2D
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


//伪代码
//随机选点赋予preX（现改为取最大值的一半）
//preX=全局最优解=局部最优解
//do
//{
//	降温
//    acceptPoint = 0 //记录取值的接受个数
//	for（马尔科夫链长度）
//	{
//		do
//		{
//			根据步长随机选取下一个解nextX
//		}while（x不在合理范围区间）	
//		if（全局最优解求值>nextX求值）
//		{
//			全局最优解=局部最优解
//		}
//		else
//		{
//			全局最优解=nextX
//		}


//		if（preX求值<nextX求值）
//		{
//			preX=nextX
//            acceptPoint+=1
//		}
//		else//挣脱局部最优解
//		{
//			if(根据结果差和当前温度计算挣脱条件满足）
//			{
//				接受新解，preX=nextX	
//			}else
//			{
//				不接受，preX保持不变
//			}		
//		}
//	}

//}while(全局最优解求值与局部最优解求值之差的绝对值大于容差)
//得到最优解bestX

////计算熵
//更新灯光位置
//更新相机渲染贴图
//将渲染贴图转换为2D图像
//灰度化该2D图像
//根据渲染图计算所有灰度值的pi
//根据pi算得熵

