using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public sealed partial class NetEaseControl : IDisposable {
 readonly HttpClient http=new HttpClient{Timeout=TimeSpan.FromSeconds(3)};
 readonly JavaScriptSerializer json=new JavaScriptSerializer{MaxJsonLength=8*1024*1024};string target;
 public async Task<object> Evaluate(string expression,bool awaitPromise=true){
  using(var cancel=new CancellationTokenSource(12000))using(var socket=new ClientWebSocket()){
   try{
    if(target==null){var pages=json.DeserializeObject(await http.GetStringAsync("http://127.0.0.1:9223/json"))as object[];foreach(var p in pages??new object[0]){var d=Island.Obj(p);if(Island.Str(d,"type")=="page"&&Island.Str(d,"url").StartsWith("orpheus://",StringComparison.OrdinalIgnoreCase)){target=Island.Str(d,"webSocketDebuggerUrl");break;}}if(String.IsNullOrEmpty(target))throw new IOException("网易云本机控制通道未开启");}
    Uri uri=new Uri(target);if(!uri.IsLoopback||uri.Port!=9223)throw new IOException("拒绝连接非本机控制地址");await socket.ConnectAsync(uri,cancel.Token);
    var request=json.Serialize(new{id=1,method="Runtime.evaluate",@params=new{expression=expression,returnByValue=true,awaitPromise=awaitPromise}});var bytes=Encoding.UTF8.GetBytes(request);await socket.SendAsync(new ArraySegment<byte>(bytes),WebSocketMessageType.Text,true,cancel.Token);
    var buffer=new byte[16384];while(true){using(var stream=new MemoryStream()){WebSocketReceiveResult part;do{part=await socket.ReceiveAsync(new ArraySegment<byte>(buffer),cancel.Token);if(part.MessageType==WebSocketMessageType.Close)throw new IOException("网易云控制连接已关闭");stream.Write(buffer,0,part.Count);if(stream.Length>8*1024*1024)throw new IOException("网易云响应过大");}while(!part.EndOfMessage);
     var message=Island.Obj(json.DeserializeObject(Encoding.UTF8.GetString(stream.ToArray())));if(Island.Num(message,"id")!=1)continue;var error=Island.Get(message,"error");if(error!=null)throw new IOException(Island.Str(Island.Obj(error),"message"));var result=Island.Obj(Island.Get(message,"result"));if(Island.Get(result,"exceptionDetails")!=null)throw new IOException(Island.Str(Island.Obj(Island.Get(result,"result")),"description"));return Island.Get(Island.Obj(Island.Get(result,"result")),"value");
    }}
   }catch{target=null;throw;}
  }
 }
 public static string RuntimePrefix="if(!window.__cloudIslandRequire){webpackJsonp.push([[999071],{999071:function(m,e,r){window.__cloudIslandRequire=r}},[[999071]]]);}const r=window.__cloudIslandRequire;";
 public void Dispose(){http.Dispose();}
}
