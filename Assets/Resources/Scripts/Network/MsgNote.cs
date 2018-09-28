using UnityEngine;
using System.Collections;
using System.Threading;
using System;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;


[StructLayoutAttribute(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public class XPacket : MsgPrefix
{

    public UInt16 ScreenWidth;
    public UInt16 ScreenHeight;

    public XPacket(UInt16 msgID): base(msgID)
    {
        
    }

    public XPacket(UInt16 msgID , UInt16 width , UInt16 height): base(msgID)
    {
        ScreenWidth = width;
        ScreenHeight = height;
    }
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public class JsonPacket : MsgPrefix
{

    //public UInt16 LightmapCount;

    public JsonPacket(UInt16 msgID) : base(msgID)
    {

    }
    public byte[] AddMsgHeader(byte[] bSmall)
    {
        var bBig = BitConverter.GetBytes(MsgID);
        byte[] tmp = new byte[bBig.Length + bSmall.Length];
        System.Buffer.BlockCopy(bBig, 0, tmp, 0, bBig.Length);
        System.Buffer.BlockCopy(bSmall, 0, tmp, bBig.Length, bSmall.Length);
        return tmp;
    }
}

[StructLayoutAttribute(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public class LmPacket : MsgPrefix
{

    public UInt16 LightmapCount;

    public LmPacket(UInt16 msgID) : base(msgID)
    {

    }

    public LmPacket(UInt16 msgID, UInt16 lightmapCount) : base(msgID)
    {
        LightmapCount = lightmapCount;
    }
}

public class MsgNoteUtils
{
    public static byte[] StructToBytes(object structObj)
    {
        int size = Marshal.SizeOf(structObj);

        byte[] bytes = new byte[size];
        IntPtr structPtr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(structObj, structPtr, false);
        Marshal.Copy(structPtr, bytes, 0, size);
        Marshal.FreeHGlobal(structPtr);
        return bytes;
    }

    public static object BytesToStruct(byte[] bytes, Type type)
    {
        int size = Marshal.SizeOf(type);

        if (size > bytes.Length)
        {
            return null;
        }

        IntPtr structPtr = Marshal.AllocHGlobal(size);
        Marshal.Copy(bytes, 0, structPtr, size);
        object obj = Marshal.PtrToStructure(structPtr, type);
        Marshal.FreeHGlobal(structPtr);
        return obj;
    }

    public static eMsgID GetMessageHead(byte[] bytes)
    {
        int size = 1;
        byte[] tmp = new byte[1];
        System.Buffer.BlockCopy(bytes, 0, tmp, 0, size);
        return (eMsgID)(int)tmp[0];
    }

    public static byte[] GetMessageBody(byte[] bytes)
    {
        Debug.Assert(bytes.Length >= 1);
        byte[] body = new byte[bytes.Length - 1];
        Buffer.BlockCopy(bytes, 1, body, 0, bytes.Length-1);
        return body;
    }
}

public enum eMsgID: byte
{
    Common,
    C2S_AttributeStream,
    S2C_RadianceStream,
    C2S_DGI_Init,
    S2C_Json_LightmapST,
    S2C_Json_LightmapArray,
    C2S_Json_UpdateObjectPosition,
    C2D_DGI_RecalcLightmap
  //  S2C_LightmapArray
}

[StructLayoutAttribute(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct ClientObjectAttribute
{
    //public ushort ScreenWidth;
    //public ushort ScreenHeight;
    public float Msg;
    public float Param;

    // Camera position
    public float CameraPosX;
    public float CameraPosY;
    public float CameraPosZ;

    // Camera rotation
    public float CameraRotX;
    public float CameraRotY;
    public float CameraRotZ;

    // Light position
    public float LightPosX;
    public float LightPosY;
    public float LightPosZ;

    // Light rotation
    public float LightRotX;
    public float LightRotY;
    public float LightRotZ;
}

[Serializable]
public struct ClientSyncObjectData
{
    public string name;
    public float positionX;
    public float positionY;
    public float positionZ;
}