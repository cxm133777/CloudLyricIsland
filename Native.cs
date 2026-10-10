using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

internal static class Native {
 [DllImport("user32.dll")] internal static extern bool ReleaseCapture();
 [DllImport("user32.dll")] internal static extern IntPtr SendMessage(IntPtr h,int msg,IntPtr w,IntPtr l);
 [DllImport("user32.dll")] internal static extern bool ShowWindow(IntPtr h,int command);
 [DllImport("user32.dll")] internal static extern bool RegisterHotKey(IntPtr h,int id,uint modifiers,uint key);
 [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr h,int id);
 [DllImport("user32.dll")] internal static extern int GetWindowLong(IntPtr h,int index);
 [DllImport("user32.dll")] internal static extern int SetWindowLong(IntPtr h,int index,int value);
 [DllImport("user32.dll")] internal static extern bool PostMessage(IntPtr h,int msg,IntPtr w,IntPtr l);
 [DllImport("user32.dll")] internal static extern short GetAsyncKeyState(int key);
 [DllImport("user32.dll")] internal static extern void keybd_event(byte key,byte scan,uint flags,UIntPtr extra);
 [DllImport("user32.dll")] internal static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int cx,int cy,uint flags);
 internal static void PassThrough(IntPtr h,bool on){int s=GetWindowLong(h,-20);SetWindowLong(h,-20,on?s|0x20:s&~0x20);}
 internal static IntPtr CloudWindow(){foreach(var p in Process.GetProcessesByName("cloudmusic")){using(p){if(p.MainWindowHandle!=IntPtr.Zero)return p.MainWindowHandle;}}return IntPtr.Zero;}
 internal static bool Media(int command){var h=CloudWindow();return h!=IntPtr.Zero && PostMessage(h,0x319,h,new IntPtr(command<<16));}
 internal static bool Released(){return GetAsyncKeyState(0x11)>=0 && GetAsyncKeyState(0x12)>=0 && GetAsyncKeyState(0x10)>=0;}
 internal static void Shortcut(uint mods,uint key){var keys=new List<byte>();if((mods&2)!=0)keys.Add(0x11);if((mods&1)!=0)keys.Add(0x12);if((mods&4)!=0)keys.Add(0x10);foreach(var k in keys)keybd_event(k,0,0,UIntPtr.Zero);keybd_event((byte)key,0,0,UIntPtr.Zero);keybd_event((byte)key,0,2,UIntPtr.Zero);keys.Reverse();foreach(var k in keys)keybd_event(k,0,2,UIntPtr.Zero);}
}

[ComImport,Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")] internal class DeviceEnumerator {}
[ComImport,Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] internal interface IDevices {
 [PreserveSig]int EnumAudioEndpoints(int flow,int state,out IntPtr collection);
 [PreserveSig]int GetDefaultAudioEndpoint(int flow,int role,out IDevice device);
}
[ComImport,Guid("D666063F-1587-4E43-81F1-B948E807363F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] internal interface IDevice {
 [PreserveSig]int Activate(ref Guid iid,int context,IntPtr parameters,[MarshalAs(UnmanagedType.IUnknown)]out object instance);
}
[ComImport,Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] internal interface ISessions {
 [PreserveSig]int GetAudioSessionControl(IntPtr guid,uint flags,out IntPtr control);
 [PreserveSig]int GetSimpleAudioVolume(IntPtr guid,uint flags,out IntPtr volume);
 [PreserveSig]int GetSessionEnumerator(out ISessionList list);
}
[ComImport,Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] internal interface ISessionList {
 [PreserveSig]int GetCount(out int count);
 [PreserveSig]int GetSession(int index,out ISession session);
}
[ComImport,Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] internal interface ISession {
 [PreserveSig]int GetState(out int state);
 [PreserveSig]int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)]out string name);
 [PreserveSig]int SetDisplayName(string name,IntPtr context);
 [PreserveSig]int GetIconPath([MarshalAs(UnmanagedType.LPWStr)]out string path);
 [PreserveSig]int SetIconPath(string path,IntPtr context);
 [PreserveSig]int GetGroupingParam(out Guid guid);
 [PreserveSig]int SetGroupingParam(ref Guid guid,IntPtr context);
 [PreserveSig]int RegisterAudioSessionNotification(IntPtr client);
 [PreserveSig]int UnregisterAudioSessionNotification(IntPtr client);
 [PreserveSig]int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)]out string name);
 [PreserveSig]int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)]out string name);
 [PreserveSig]int GetProcessId(out uint pid);
 [PreserveSig]int IsSystemSoundsSession();
 [PreserveSig]int SetDuckingPreference(bool optOut);
}
[ComImport,Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] internal interface IVolume {
 [PreserveSig]int SetMasterVolume(float level,ref Guid context);
 [PreserveSig]int GetMasterVolume(out float level);
 [PreserveSig]int SetMute(bool mute,ref Guid context);
 [PreserveSig]int GetMute(out bool mute);
}
internal static class CloudVolume {
 internal static float Access(float? desired){
  object enumerator=null,manager=null; IDevice device=null; ISessionList list=null;
  try {
   enumerator=new DeviceEnumerator();Marshal.ThrowExceptionForHR(((IDevices)enumerator).GetDefaultAudioEndpoint(0,1,out device));
   var guid=new Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F");Marshal.ThrowExceptionForHR(device.Activate(ref guid,23,IntPtr.Zero,out manager));
   Marshal.ThrowExceptionForHR(((ISessions)manager).GetSessionEnumerator(out list));int count;list.GetCount(out count);float result=-1;
   for(int i=0;i<count;i++){ISession s=null;try{list.GetSession(i,out s);int state;s.GetState(out state);if(state==2)continue;uint pid;s.GetProcessId(out pid);bool match=false;try{using(var p=Process.GetProcessById((int)pid)){match=p.ProcessName.Equals("cloudmusic",StringComparison.OrdinalIgnoreCase);}}catch{}
    if(match){var v=(IVolume)s;float level;v.GetMasterVolume(out level);result=level;if(desired.HasValue){var ctx=Guid.Empty;Marshal.ThrowExceptionForHR(v.SetMasterVolume(Math.Max(0,Math.Min(1,desired.Value)),ref ctx));result=desired.Value;}}
   }finally{if(s!=null)Marshal.ReleaseComObject(s);}}
   return result;
  }finally{if(list!=null)Marshal.ReleaseComObject(list);if(manager!=null)Marshal.ReleaseComObject(manager);if(device!=null)Marshal.ReleaseComObject(device);if(enumerator!=null)Marshal.ReleaseComObject(enumerator);}
 }
}
