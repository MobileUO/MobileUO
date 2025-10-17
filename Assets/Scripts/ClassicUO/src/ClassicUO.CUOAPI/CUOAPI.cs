//using System;
//using System.Runtime.InteropServices;

//namespace CUO_API
//{
//    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//    public delegate void OnCastSpell(int idx);
//    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//    public delegate void OnFocusLost();
//    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//    public delegate void OnFocusGained();
//    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//    public delegate void OnDisconnected();
//    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//    public delegate void OnConnected();
//    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//    public delegate void OnClientClose();
//    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//    public delegate short OnGetPacketLength(int id);
//    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//    public delegate bool OnGetPlayerPosition(out int x, out int y, out int z);
//    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//    public delegate void OnGetStaticImage(ushort g, ref ArtInfo art);
//    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//    public delegate string OnGetUOFilePath();
//    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//    public delegate bool OnHotkey(int key, int mod, bool pressed);
//    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//    public delegate void OnInitialize();
//    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//    public delegate void OnMouse(int button, int wheel);
//    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//    public delegate bool OnPacketSendRecv(ref byte[] data, ref int length);
//    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//    public delegate void OnSetTitle(string title);
//    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//    public delegate void OnTick();
//    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//    public delegate void OnUpdatePlayerPosition(int x, int y, int z);
//    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
//    public delegate bool RequestMove(int dir, bool run);

//    public struct PluginHeader
//    {
//        public int ClientVersion;
//        public IntPtr HWND;
//        public IntPtr OnRecv;
//        public IntPtr OnSend;
//        public IntPtr OnHotkeyPressed;
//        public IntPtr OnMouse;
//        public IntPtr OnPlayerPositionChanged;
//        public IntPtr OnClientClosing;
//        public IntPtr OnInitialize;
//        public IntPtr OnConnected;
//        public IntPtr OnDisconnected;
//        public IntPtr OnFocusGained;
//        public IntPtr OnFocusLost;
//        public IntPtr GetUOFilePath;
//        public IntPtr Recv;
//        public IntPtr Send;
//        public IntPtr GetPacketLength;
//        public IntPtr GetPlayerPosition;
//        public IntPtr CastSpell;
//        public IntPtr GetStaticImage;
//        public IntPtr Tick;
//        public IntPtr RequestMove;
//        public IntPtr SetTitle;
//    }

//    [StructLayout(LayoutKind.Sequential, Pack = 1)]
//    public struct ArtInfo
//    {
//        public long Address;
//        public long Size;
//        public long CompressedSize;
//    }
//}