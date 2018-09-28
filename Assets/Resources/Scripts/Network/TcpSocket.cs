using UnityEngine;
using System.Collections;
using System.Threading;
using System;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using System.IO;

enum ESocketError
{
    OverflowReceive = -3,
    OverflowSend = -2,
    PacketError = -1,
}

enum ENetEvent
{
    Connect = 0,
    Receive,
    Close,
    Error,
}

class NetEvent
{
    public ENetEvent eEvent;
    public int nParam;
}

class LoopBuffer
{
    private const int MinBufferLength = 65535;
    private int m_BufferLength;
    private byte[] m_Buffer;
    private int m_In = 0;
    private int m_Out = 0;

    public LoopBuffer(int buffLen)
    {
        if (buffLen <= MinBufferLength)
        {
            m_BufferLength = buffLen;
        }

        m_BufferLength = buffLen;
        m_Buffer = new byte[m_BufferLength];
    }

    public void Reset()
    {
        m_In = 0;
        m_Out = 0;
    }

    public bool Push(byte[] arrInData, int nOffset, int nLength)
    {
        int nLeftSpace = 0;
        if (m_In < m_Out)
        {
            nLeftSpace = m_Out - m_In;
            if (nLeftSpace < nLength)
            {
                Debug.Log("Buffer is full! Input length: " + nLength + ", LeftSpace: " + nLeftSpace);
                // Reset();
                return false;
            }

            Array.Copy(arrInData, nOffset, m_Buffer, m_In, nLength);

            m_In += nLength;
        }
        else
        {
            nLeftSpace = m_BufferLength - (m_In - m_Out);
            if (nLeftSpace < nLength)
            {
                Debug.Log("Buffer is full! Input length: " + nLength + ", LeftSpace: " + nLeftSpace);
                // Reset();
                return false;
            }

            if (m_BufferLength - m_In < nLength)
            {
                int nFirstSectionLength = m_BufferLength - m_In;

                Array.Copy(arrInData, nOffset, m_Buffer, m_In, nFirstSectionLength);

                Array.Copy(arrInData, nOffset + nFirstSectionLength, m_Buffer, 0, nLength - nFirstSectionLength);

                m_In = nLength - nFirstSectionLength;
            }
            else
            {
                Array.Copy(arrInData, nOffset, m_Buffer, m_In, nLength);

                m_In += nLength;
            }
        }

        return true;
    }

    public bool Pop(ref byte[] arrOutData, int nLength)
    {
        int nOccupiedSpace = 0;
        if (m_In < m_Out)
        {
            nOccupiedSpace = m_BufferLength - (m_Out - m_In);
            if (nLength > nOccupiedSpace)
            {
                Debug.Log("Occupied space is little than input nLength! Pop Length: " + nLength + ", OccupiedSpace: " + nOccupiedSpace);
                // Reset();
                return false;
            }

            if (nLength < (m_BufferLength - m_Out))
            {
                Array.Copy(m_Buffer, m_Out, arrOutData, 0, nLength);

                m_Out += nLength;
            }
            else
            {
                int nFirstSectionLength = m_BufferLength - m_Out;
                Array.Copy(m_Buffer, m_Out, arrOutData, 0, nFirstSectionLength);

                Array.Copy(m_Buffer, 0, arrOutData, nFirstSectionLength, nLength - nFirstSectionLength);

                m_Out = nLength - nFirstSectionLength;
            }
        }
        else
        {
            nOccupiedSpace = m_In - m_Out;
            if (nLength > nOccupiedSpace)
            {
                Debug.LogError("Occupied space is little than input nLength! Pop Length: " + nLength + ", OccupiedSpace: " + nOccupiedSpace);
                // Reset();
                return false;
            }

            Array.Copy(m_Buffer, m_Out, arrOutData, 0, nLength);

            m_Out += nLength;
        }

        return true;
    }
}

[StructLayoutAttribute(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public class MsgPrefix
{
    public UInt16 MsgID;
    public UInt16 msgSize;
    public MsgPrefix(UInt16 msgID)
    {
        MsgID = msgID;
    }
}

public class TcpSocket<T> where T : MsgPrefix
{
    public delegate void OnConnectedCallback();
    public delegate void OnReceivedCallback(TcpSocket<T> socket , T msgNote , MemoryStream msgStream);

    OnConnectedCallback mOnConnectedCallback = null;
    OnReceivedCallback mOnReceivedCallback = null;

    const int BufferLength = 1024 * 1024;
    const int ReceiveLoopBufferLength = 1024 * 1024;

    Socket mSocket = null;

    string mIPAddress;
    int m_Port;

    byte[] mDataBuffer;
    int mCurPosition = 0;
    LoopBuffer mReceiveLoopBuffer;
    byte[] mSendBuffer;
    byte[] mReceiveBuffer;

    object mLocker = new object();
    Thread mSendKeepAliveThread;

    ArrayList mEvents = new ArrayList();

    public readonly int MsgPrefixLength;

    public byte[] StructToBytes(object structObj)
    {
        int size = Marshal.SizeOf(structObj);

        byte[] bytes = new byte[size];
        IntPtr structPtr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(structObj, structPtr, false);
        Marshal.Copy(structPtr, bytes, 0, size);
        Marshal.FreeHGlobal(structPtr);
        return bytes;
    }

    public object BytesToStruct(byte[] bytes, Type type)
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

    public TcpSocket(Socket clientSocket, OnReceivedCallback receivedCallback)
    {
        mDataBuffer = new byte[BufferLength];
        mCurPosition = 0;
        mReceiveLoopBuffer = new LoopBuffer(ReceiveLoopBufferLength);
        mSendBuffer = new byte[BufferLength];
        mReceiveBuffer = new byte[BufferLength];

        MsgPrefixLength = Marshal.SizeOf(typeof(T));

        mSocket = clientSocket;

        mOnReceivedCallback = receivedCallback;

        _Receive();
    }

    public TcpSocket()
    {
        mDataBuffer = new byte[BufferLength];
        mCurPosition = 0;
        mReceiveLoopBuffer = new LoopBuffer(ReceiveLoopBufferLength);
        mSendBuffer = new byte[BufferLength];
        mReceiveBuffer = new byte[BufferLength];

        MsgPrefixLength = Marshal.SizeOf(typeof(T));
    }

    void Reset()
    {
        mCurPosition = 0;
        mReceiveLoopBuffer.Reset();
    }

    public void ReuseSocket()
    {
        mSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
    }

    public void BuildConnection(Socket bindSocket)
    {
        mSocket = bindSocket;
    }

    public void Connect(string ip, int port , OnConnectedCallback connectedCallback = null , OnReceivedCallback receivedCallback = null)
    {
        mIPAddress = ip;
        m_Port = port;

        if (connectedCallback != null)
        {
            mOnConnectedCallback = connectedCallback;
        }

        if (receivedCallback != null)
        {
            mOnReceivedCallback = receivedCallback;
        }
        
        _Connect();
    }

    void _Connect()
    {
        if (IsConnected())
        {
            OnConnect();
            return;
        }

        Reset();
        mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            IPAddress[] ipHost = Dns.GetHostAddresses(mIPAddress);
            IPEndPoint ipe = new IPEndPoint(ipHost[0], m_Port);
            mSocket.BeginConnect(ipe, new AsyncCallback(ConnectCallBack), mSocket);
        }
        catch (SocketException ex)
        {
            PushEvent(ENetEvent.Error, (int)(ex.SocketErrorCode));
        }
    }

    public void Close()
    {
        if (mSocket != null)
        {
            mSocket.Disconnect(true);

            mSocket.Close();
            mSocket = null;
        }
    }

    public bool IsConnected()
    {
        if (null == mSocket)
        {
            return false;
        }
        return mSocket.Connected;
    }

    public bool SendData(T msgNote, Byte[] dataBuf)
    {
        if (null == mSocket || !mSocket.Connected)
        {
            return false;
        }

        ushort bufLength = (ushort)(MsgPrefixLength + dataBuf.Length);

        // Check data length.
        if (bufLength > BufferLength - MsgPrefixLength)
        {
            PushEvent(ENetEvent.Error, (int)(ESocketError.OverflowSend));

            if (mSocket != null)
            {
                mSocket.Close();
                mSocket = null;
            }

            PushEvent(ENetEvent.Close, 0);
            return false;
        }

        msgNote.msgSize = bufLength;

        // Msg Note
        Array.Copy(StructToBytes(msgNote), 0, mSendBuffer, 0, MsgPrefixLength);

        // Buffer data
        if (dataBuf != null)
        {
            Array.Copy(dataBuf, 0, mSendBuffer, MsgPrefixLength, bufLength - MsgPrefixLength);
        }

        int timeout = 1000;

        if (mSocket != null)
        {
            mSocket.SendTimeout = 0;
        }

        int startTickCount = Environment.TickCount;
        int Sent = 0;
        do
        {
            if (Environment.TickCount > startTickCount + timeout)
            {
                PushEvent(ENetEvent.Error, (int)(ESocketError.OverflowSend));

                if (mSocket != null)
                {
                    mSocket.Close();
                    mSocket = null;
                }

                PushEvent(ENetEvent.Close, 0);
                return false;
            }

            if (mSocket != null)
            {
                try
                {
                    Sent += mSocket.Send(mSendBuffer, Sent, bufLength - Sent, SocketFlags.None);
                    //Debug.Log("m_LobbySocket.Send return" + Sent);
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.WouldBlock ||
                        ex.SocketErrorCode == SocketError.IOPending ||
                        ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                    {
                        Thread.Sleep(30);
                    }
                    else
                    {
                        PushEvent(ENetEvent.Error, (int)(ESocketError.OverflowSend));
                        mSocket.Close();
                        mSocket = null;
                        PushEvent(ENetEvent.Close, 0);
                    }
                }
            }
        }
        while (Sent < bufLength && mSocket != null);

        return true;
    }

    void PushEvent(ENetEvent eEvent, int nParam)
    {
        NetEvent netEvent = new NetEvent();

        netEvent.eEvent = eEvent;
        netEvent.nParam = nParam;

        lock (mLocker)
        {
            mEvents.Add(netEvent);
        }
    }

    void ConnectCallBack(IAsyncResult AR)
    {
        try
        {
            if (null == mSocket)
            {
                return;
            }
            mSocket.EndConnect(AR);

            _Receive();
        }
        catch (SocketException ex)
        {
            Debug.LogError("Exception: " + ex.ToString());
            PushEvent(ENetEvent.Error, (int)(ex.SocketErrorCode));
        }
    }

    void _Receive()
    {
        PushEvent(ENetEvent.Connect, 0);

        mSocket.BeginReceive(mDataBuffer, mCurPosition, mDataBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallBack), null);
    }

    void ReceiveCallBack(IAsyncResult AR)
    {
        try
        {
            if (null == mSocket)
            {
                return;
            }

            int REnd = mSocket.EndReceive(AR);
            mCurPosition += REnd;

            // Got Message, lock
            do
            {
                if (mCurPosition >= MsgPrefixLength)
                {
                    T head = (T)BytesToStruct(mDataBuffer, typeof(T));
                    int len = (int)head.msgSize;

                    if (len < 0)
                    {
                        PushEvent(ENetEvent.Error, (int)(ESocketError.PacketError));
                        Close();
                        PushEvent(ENetEvent.Close, 0);
                        return;
                    }
                    else if (len > mCurPosition)
                    {
                        break;
                    }
                    else
                    {
                        bool bRes = false;
                        lock (mLocker)
                        {
                            bRes = mReceiveLoopBuffer.Push(mDataBuffer, 0, len);
                        }

                        if (!bRes)
                        {
                            PushEvent(ENetEvent.Error, (int)(ESocketError.OverflowReceive));
                            Close();
                            PushEvent(ENetEvent.Close, 0);

                            return;
                        }
                        PushEvent(ENetEvent.Receive, len);

                        mCurPosition -= len;
                        if (mCurPosition > 0)
                        {
                            Array.Copy(mDataBuffer, len, mDataBuffer, 0, mCurPosition);
                        }
                    }
                }
                else
                {
                    break;
                }
            } while (mCurPosition > 0);

            mSocket.BeginReceive(mDataBuffer, mCurPosition, mDataBuffer.Length - mCurPosition, 0, new AsyncCallback(ReceiveCallBack), null);
        }
        catch (SocketException ex)
        {
            Debug.LogError("Connected: " + mSocket.Connected + ". Exception: " + ex.ToString());
            PushEvent(ENetEvent.Error, (int)(ex.SocketErrorCode));
            PushEvent(ENetEvent.Close, 0);
            if (!IsConnected())
            {
                mSocket.Close();
                mSocket = null;
            }
        }
    }

    public void Run()
    {
        try
        {
            lock (mLocker)
            {
                if (mEvents.Count == 0)
                {
                    return;
                }
                foreach (NetEvent netEvent in mEvents)
                {
                    // Pop
                    switch (netEvent.eEvent)
                    {
                        case ENetEvent.Connect:
                            {
                                //Reset();
                                OnConnect();
                            }
                            break;
                        case ENetEvent.Receive:
                            {
                                if (!mReceiveLoopBuffer.Pop(ref mReceiveBuffer, netEvent.nParam))
                                {
                                    Debug.LogWarning("Pop packet error!");
                                    Close();
                                }
                                else
                                {
                                    OnReceive(mReceiveBuffer, netEvent.nParam);
                                }
                            }
                            break;
                        case ENetEvent.Close:
                            {
                                OnClose();
                            }
                            break;

                        case ENetEvent.Error:
                            {
                                OnError(netEvent.nParam);
                            }
                            break;

                        default:
                            {

                            }
                            break;
                    }
                }

                mEvents.Clear();
            }

        }
        catch (Exception e)
        {
            Debug.LogError("Exception in Run: " + e.ToString());
            mEvents.RemoveAt(0);
        }
    }

    void OnConnect()
    {
        if (mOnConnectedCallback != null)
        {
            mOnConnectedCallback();
        }
    }

    void OnReceive(Byte[] dataBuffer, int length)
    {
        if (mOnReceivedCallback != null)
        {
            T msgNote = (T)BytesToStruct(dataBuffer, typeof(T));

            MemoryStream msgStream = new MemoryStream(dataBuffer, MsgPrefixLength, msgNote.msgSize - MsgPrefixLength);

            mOnReceivedCallback(this , msgNote, msgStream);
        }
    }

    void OnError(int errCode)
    {

    }

    void OnClose()
    {
        if (mSocket != null)
        {
            mSocket.Close();
            mSocket = null;
        }
    }
}