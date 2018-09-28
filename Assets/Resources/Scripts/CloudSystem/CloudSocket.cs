using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;
using LitJson;


public  class JsonData
{
    public eMsgID MsgID;

    public virtual byte[] Pack()
    {
        return new []{(byte)MsgID};
    }

    public JsonData(eMsgID id)
    {
        MsgID = id;
    }
}

/*
  {
    "objects": [
        {
            "obj1": {
                "lightmapIndex": 0,
                "offsetX": -0.2,
                "offsetY": 0.4,
                "scaleX": 0.5,
                "scaleY": 0.6
            }
        },
        {
            "obj2": {
                "lightmapIndex": 0,
                "offsetX": -0.2,
                "offsetY": 0.4,
                "scaleX": 0.5,
                "scaleY": 0.6
            }
        }
    ]
}
*/

public class LightmapSTJsonData : JsonData
{
    public Dictionary<string, Tuple<int, Vector4>> STDict = new Dictionary<string, Tuple<int, Vector4>>();

    public LightmapSTJsonData(MeshRenderer[] meshRenderers,eMsgID id) : base(id)
    {
        foreach (var mesh in meshRenderers)
        {
            if(STDict.ContainsKey(mesh.gameObject.name))
                Debug.Log("Same Name: " + mesh.gameObject.name);
            STDict.Add(mesh.gameObject.name, new Tuple<int, Vector4>(mesh.lightmapIndex, mesh.lightmapScaleOffset));
        }
    }
    static void LightmapST2Json(JsonWriter writer, int index, Vector4 st)
    {
        writer.WriteObjectStart();
        writer.WritePropertyName("lightmapIndex");
        writer.Write(index);
        writer.WritePropertyName("scaleX");
        writer.Write(st.x);
        writer.WritePropertyName("scaleY");
        writer.Write(st.y);
        writer.WritePropertyName("offsetX");
        writer.Write(st.z);
        writer.WritePropertyName("offsetY");
        writer.Write(st.w);
        writer.WriteObjectEnd();
   //     Camera.main.
    }

    public override byte[] Pack()
    {
        StringBuilder sb = new StringBuilder();
        JsonWriter writer = new JsonWriter(sb);

        writer.WriteObjectStart();
        //writer.WritePropertyName("objects");
       // writer.WriteArrayStart();
        foreach (var st in STDict)
        {
           // writer.WriteObjectStart();
            writer.WritePropertyName(st.Key);
            LightmapST2Json(writer, st.Value.Item1,  st.Value.Item2);
          //  writer.WriteObjectEnd();

        }
     //   writer.WriteArrayEnd();
        writer.WriteObjectEnd();
        var jsonStr = sb.ToString();
        byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonStr);
        FileStream file = new FileStream("d:/lightmapST.json", FileMode.Create);
       
        file.Write(jsonBytes, 0, jsonBytes.Length);
        if (file != null)
        {
            file.Close();
        }
        return jsonBytes;
    }
}

/// 
/// {
///     "lightmap"=[
///                 {
///                  "index"=0,
///                  "base64""dsl;ifunjiawevy"
///                 },
///                 {
///                  "index"=1,
///                  "base64""dsl;ifunjiawevy"
///                 }
///                ]
/// }
public class LightmapArrayJsonData : JsonData
{
    public LightmapArrayJsonData(eMsgID id) : base(id)
    {

    }

    public override byte[] Pack()
    {
        StringBuilder sb = new StringBuilder();
        JsonWriter writer = new JsonWriter(sb);

        writer.WriteObjectStart();
        writer.WritePropertyName("lightmap");
        writer.WriteArrayStart();
        for (int i = 0; i < LightmapSettings.lightmaps.Length; ++i)
        {
            writer.WriteObjectStart();
            writer.WritePropertyName("index");
            writer.Write(i.ToString());
            writer.WritePropertyName("base64");

           // writer.Write(Convert.ToBase64String(Baker.ExtractLightmapBytes(LightmapSettings.lightmaps[i].lightmapColor)));
            writer.Write(Convert.ToBase64String(Baker.ExtractLightmapBytes(Baker.Instance.LightmapData[i])));
            writer.WriteObjectEnd();
        }
        writer.WriteArrayEnd();
        writer.WriteObjectEnd();

        var jsonStr = sb.ToString();
        return Encoding.UTF8.GetBytes(jsonStr);
    }
}

public class RadianceData
{
    public UInt16 Header;
    public UInt16 Width;
    public UInt16 Height;
    
    byte[] mRawData;

    public RadianceData(UInt16 width , UInt16 height , byte[] rawData)
    {
        Width = width;
        Height = height;
        mRawData = rawData;
    }

    public RadianceData(UInt16 header, UInt16 width, UInt16 height, byte[] rawData)
    {
        Header = header;
        Width = width;
        Height = height;
        mRawData = rawData;
    }

    public byte[] packedData
    {
        get
        {
            byte[] headerBytes = BitConverter.GetBytes(Header);

            byte[] newData = new byte[headerBytes.Length + mRawData.Length];

            LCRS.Log("======================================== header length: " + headerBytes.Length + ", #$34F: " + Header);

            Array.Copy(headerBytes, newData, headerBytes.Length);
            Array.Copy(mRawData, 0, newData, headerBytes.Length, mRawData.Length);

            return newData;
        }
    }

    public byte[] rawData
    {
        get
        {
            return mRawData;
        }
    }
}

public class AttributeData
{
    public byte[] RawData;

    public AttributeData(byte[] rawData)
    {
        RawData = rawData;
    }
}

public class CloudSocket
{
    Queue<AttributeData> mAttributeDataList = new Queue<AttributeData>();
    Queue<RadianceData> mRadianceDataList = new Queue<RadianceData>();
    Queue<JsonData> mJsonDataList = new Queue<JsonData>();

    public RadianceData DequeueRadianceData()
    {
        if (mRadianceDataList.Count > 0)
        {
            return mRadianceDataList.Dequeue();
        }

        return null;
    }

    public AttributeData DequeueAttributeData()
    {
        if (mAttributeDataList.Count > 0)
        {
            return mAttributeDataList.Dequeue();
        }

        return null;
    }

    public void EnqueueRadianceData(RadianceData dataBytes)
    {
        mRadianceDataList.Enqueue(dataBytes);
    }

    public void EnqueueAttributeData(AttributeData dataBytes)
    {
        mAttributeDataList.Enqueue(dataBytes);
    }

    public void EnqueueJsonData(JsonData dataBytes)
    {
        mJsonDataList.Enqueue(dataBytes);
    }

    public JsonData DequeueJsonData()
    {
        if (mJsonDataList.Count > 0)
        {
            return mJsonDataList.Dequeue();
        }

        return null;
    }
}
