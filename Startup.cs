using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Controls;

public sealed partial class Island {
 DateTime autoRestartNotBefore;
 string FindCloudClient(out int clientId){
  clientId=0;string executable=null;DateTime oldest=DateTime.MaxValue;
  foreach(var process in Process.GetProcessesByName("cloudmusic"))using(process)try{
   if(process.StartTime<oldest){oldest=process.StartTime;clientId=process.Id;executable=process.MainModule.FileName;}
  }catch{}
  if(!String.IsNullOrEmpty(executable)&&File.Exists(executable))return executable;
  foreach(string folder in new[]{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)}){
   string candidate=Path.Combine(folder,"Netease","CloudMusic","cloudmusic.exe");if(File.Exists(candidate))return candidate;
  }
  return null;
 }
 void ConnectionMessage(string message,TextBlock label){nativeConnectionMessage=message;if(label!=null)label.Text=message;}
 async void AutoConnectNative(){
  if(!prefs.AutoNative||stopping||nativeConnecting||nativeBusy||NativeFresh||DateTime.UtcNow<nextAutoNativeAttempt)return;
  nextAutoNativeAttempt=DateTime.UtcNow.AddSeconds(10);
  await ConnectNative(true,null);
 }
 async Task ConnectNative(bool automatic,TextBlock label){
  if(stopping)return;if(nativeConnecting){if(label!=null)label.Text="正在自动连接，请稍候…";return;}nativeConnecting=true;
  try{
   ConnectionMessage("正在自动连接网易云精确控制…",label);
   try{var state=Obj(await nativeControl.Call("state"));if(stopping)return;nativeState=state;nativeStamp=DateTime.UtcNow;nextNativeAttempt=DateTime.MinValue;ConnectionMessage("精确控制已连接 · 已自动对齐真实进度",label);Tick();return;}catch{}
   if(stopping||automatic&&!prefs.AutoNative)return;
   int clientId;string executable=FindCloudClient(out clientId);
   if(executable==null){ConnectionMessage("等待网易云启动，检测到客户端后会自动连接",label);return;}
   if(automatic&&(DateTime.UtcNow<autoRestartNotBefore||clientId!=0&&autoManagedClients.Contains(clientId))){ConnectionMessage("等待网易云控制接口就绪 · 可在连接选项手动重试",label);return;}
   // A failed or incompatible client must never enter a restart loop.
   if(clientId!=0)autoManagedClients.Add(clientId);autoRestartNotBefore=DateTime.UtcNow.AddMinutes(2);
   string previousId=currentId;double previousPosition=hasTimeline?position:-1;bool wasPlaying=playing;
   ConnectionMessage("正在后台启用精确控制，网易云将重新启动一次…",label);
   foreach(var process in Process.GetProcessesByName("cloudmusic"))using(process)try{
    if(String.Equals(process.MainModule.FileName,executable,StringComparison.OrdinalIgnoreCase))process.Kill();
   }catch{}
   await Task.Delay(700);if(stopping)return;
   using(var started=Process.Start(new ProcessStartInfo(executable,"--remote-debugging-address=127.0.0.1 --remote-debugging-port=9223"){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden})){
    if(started!=null)autoManagedClients.Add(started.Id);
   }
   var until=DateTime.UtcNow.AddSeconds(35);
   while(!stopping&&DateTime.UtcNow<until){
    HideClient();
    // Also mark child/startup processes in case the executable hands off to a new main process.
    foreach(var process in Process.GetProcessesByName("cloudmusic"))using(process)autoManagedClients.Add(process.Id);
    try{
     var state=Obj(await nativeControl.Call("state"));if(stopping)return;
     if(!Bool(state,"connected")){await Task.Delay(500);continue;}
     nativeState=state;nativeStamp=DateTime.UtcNow;nextNativeAttempt=DateTime.MinValue;
     // Restore only the same track, and never use an estimated position to seek.
     if(!String.IsNullOrEmpty(previousId)&&Str(state,"id")==previousId){
      if(previousPosition>=0)nativeState=Obj(await nativeControl.Call("seek",previousPosition,previousId));
      if(wasPlaying&&!Bool(nativeState,"playing"))nativeState=Obj(await nativeControl.Call("play",previousId));
      nativeStamp=DateTime.UtcNow;
     }
     HideClient();ConnectionMessage("精确控制已连接 · 已自动对齐真实进度",label);Tick();return;
    }catch{}
    await Task.Delay(600);
   }
   if(!stopping)ConnectionMessage("自动连接暂未成功；保持检测，不会反复重启网易云。可在连接选项手动重试。",label);
  }catch(Exception ex){if(!stopping)ConnectionMessage("自动连接失败："+ex.Message,label);}
  finally{nativeConnecting=false;nextAutoNativeAttempt=DateTime.UtcNow.AddSeconds(10);}
 }
}
