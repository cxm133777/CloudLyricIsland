using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Web.Script.Serialization;

public static class ReadOnlySqlite {
 [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)]static extern int sqlite3_open_v2(byte[] path,out IntPtr db,int flags,IntPtr vfs);
 [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)]static extern int sqlite3_close(IntPtr db);
 [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)]static extern int sqlite3_busy_timeout(IntPtr db,int ms);
 [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)]static extern int sqlite3_prepare_v2(IntPtr db,byte[] sql,int size,out IntPtr stmt,IntPtr tail);
 [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)]static extern int sqlite3_step(IntPtr stmt);
 [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)]static extern int sqlite3_finalize(IntPtr stmt);
 [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)]static extern int sqlite3_column_count(IntPtr stmt);
 [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)]static extern IntPtr sqlite3_column_text(IntPtr stmt,int column);
 [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)]static extern int sqlite3_column_bytes(IntPtr stmt,int column);
 public static List<string[]> Query(string path,string query){IntPtr db=IntPtr.Zero,stmt=IntPtr.Zero;try{
  if(sqlite3_open_v2(Encoding.UTF8.GetBytes(path+"\0"),out db,1,IntPtr.Zero)!=0)throw new IOException("无法只读打开网易云歌单缓存");sqlite3_busy_timeout(db,300);
  if(sqlite3_prepare_v2(db,Encoding.UTF8.GetBytes(query+"\0"),-1,out stmt,IntPtr.Zero)!=0)throw new IOException("网易云缓存表结构不兼容");
  int rc;var rows=new List<string[]>();while((rc=sqlite3_step(stmt))==100){int count=sqlite3_column_count(stmt);var row=new string[count];for(int i=0;i<count;i++){int size=sqlite3_column_bytes(stmt,i);if(size>16*1024*1024)throw new IOException("缓存行过大");IntPtr p=sqlite3_column_text(stmt,i);var b=new byte[size];if(size>0)Marshal.Copy(p,b,0,size);row[i]=Encoding.UTF8.GetString(b);}rows.Add(row);}if(rc!=101)throw new IOException("网易云缓存忙，请稍后刷新");return rows;
 }finally{if(stmt!=IntPtr.Zero)sqlite3_finalize(stmt);if(db!=IntPtr.Zero)sqlite3_close(db);}}
}

public sealed class LikedSong {
 public string Id {get;set;} public string Name {get;set;} public string Artist {get;set;}
 public bool MetadataKnown {get;set;}
 public override string ToString(){return Name+"  ·  "+Artist;}
}
public sealed class LikedPlaylist {
 public string Id {get;set;} public string Name {get;set;} public string Owner {get;set;}
 public override string ToString(){return Name+(String.IsNullOrEmpty(Owner)?"":" · "+Owner);}
}
public sealed class LikedSnapshot {
 public List<LikedPlaylist> Choices=new List<LikedPlaylist>();
 public List<LikedSong> Songs=new List<LikedSong>();
 public HashSet<string> Ids=new HashSet<string>();
 public string PlaylistId="",Message="";
 public bool Available;
 public DateTime ReadAt;
}
public static class LikedLibrary {
 static Dictionary<string,object> Map(object o){return o as Dictionary<string,object>??new Dictionary<string,object>();}
 static string Text(Dictionary<string,object>d,string key){object v;return d.TryGetValue(key,out v)?Convert.ToString(v):"";}
 static object Value(Dictionary<string,object>d,string key){object v;return d.TryGetValue(key,out v)?v:null;}
 public static HashSet<string> ReadIds(string id){if(!System.Text.RegularExpressions.Regex.IsMatch(id??"",@"^\d+$"))return null;var db=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Netease","CloudMusic","Library","webdb.dat");var rows=ReadOnlySqlite.Query(db,"SELECT jsonStr FROM playlistTrackIds WHERE id='"+id+"' LIMIT 1");if(rows.Count==0)return null;var json=new JavaScriptSerializer{MaxJsonLength=16*1024*1024};var items=Value(Map(json.DeserializeObject(rows[0][0])),"trackIds")as object[];if(items==null)return null;return new HashSet<string>(items.Select(x=>Text(Map(x),"id")).Where(x=>System.Text.RegularExpressions.Regex.IsMatch(x,@"^\d+$")));}
 static void Collect(object node,Dictionary<string,LikedPlaylist> choices,JavaScriptSerializer json,int depth){
  if(depth>30||node==null)return;var d=node as Dictionary<string,object>;
  if(d!=null){string id=Text(d,"id");if(Text(d,"specialType")=="5"&&System.Text.RegularExpressions.Regex.IsMatch(id,@"^\d+$")&&!choices.ContainsKey(id))choices[id]=new LikedPlaylist{Id=id,Name=Text(d,"name"),Owner=Text(Map(Value(d,"creator")),"nickname")};foreach(var pair in d)Collect(pair.Value,choices,json,depth+1);return;}
  var a=node as object[];if(a!=null){foreach(var n in a)Collect(n,choices,json,depth+1);return;}
  var text=node as string;if(text!=null&&text.Contains("specialType")&&(text.StartsWith("{")||text.StartsWith("["))){try{Collect(json.DeserializeObject(text),choices,json,depth+1);}catch{}}
 }
 public static LikedSnapshot Read(string selectedId){
  var result=new LikedSnapshot{ReadAt=DateTime.Now};var json=new JavaScriptSerializer{MaxJsonLength=16*1024*1024,RecursionLimit=64};
  string root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Netease","CloudMusic");string db=Path.Combine(root,"Library","webdb.dat");
  try {
   if(!File.Exists(db)){result.Message="没有找到网易云歌单缓存，请先在网易云打开“我喜欢的音乐”。";return result;}
   var choices=new Dictionary<string,LikedPlaylist>();
   // Only inspect playlist metadata. Never read login/session tables or credentials.
   var rows=ReadOnlySqlite.Query(db,"SELECT jsonStr FROM requestCache WHERE jsonStr LIKE '%specialType' || char(92) || char(34) || ':5%' OR jsonStr LIKE '%specialType' || char(34) || ':5%' ORDER BY rowid DESC LIMIT 24");
   foreach(var row in rows){try{Collect(json.DeserializeObject(row[0]),choices,json,0);}catch{}}
   result.Choices=choices.Values.ToList();
   if(String.IsNullOrEmpty(selectedId)&&choices.Count==1)selectedId=choices.Keys.First();
   if(String.IsNullOrEmpty(selectedId)){result.Message=choices.Count>1?"找到多个喜欢歌单，请选择你要关联的歌单。":"请先在网易云打开“我喜欢的音乐”，然后刷新。";return result;}
   if(!System.Text.RegularExpressions.Regex.IsMatch(selectedId,@"^\d+$")){result.Message="歌单 ID 无效";return result;}
   var ids=ReadOnlySqlite.Query(db,"SELECT jsonStr FROM playlistTrackIds WHERE id='"+selectedId+"' LIMIT 1");
   if(ids.Count==0){result.Message="该歌单尚未缓存，请在网易云打开歌单后刷新。";return result;}
   var payload=Map(json.DeserializeObject(ids[0][0]));var entries=Value(payload,"trackIds")as object[];
   if(entries==null){result.Message="歌单缓存格式不兼容";return result;}
   var metadata=new Dictionary<string,Dictionary<string,object>>();
   try{var playing=Map(json.DeserializeObject(File.ReadAllText(Path.Combine(root,"webdata","file","playingList"),Encoding.UTF8)));foreach(var obj in Value(playing,"list")as object[]??new object[0]){var t=Map(Value(Map(obj),"track"));metadata[Text(t,"id")]=t;}}catch{}
   foreach(var entry in entries){string id=Text(Map(entry),"id");if(System.Text.RegularExpressions.Regex.IsMatch(id,@"^\d+$"))result.Ids.Add(id);}
   if(result.Ids.Count>0){string list=String.Join(",",result.Ids.Select(x=>"'"+x+"'"));foreach(var row in ReadOnlySqlite.Query(db,"SELECT id,jsonStr FROM dbTrack WHERE id IN ("+list+")")){try{metadata[row[0]]=Map(json.DeserializeObject(row[1]));}catch{}}}
   // Preserve the player's liked-playlist order, rather than HashSet order.
   var seen=new HashSet<string>();foreach(var entry in entries){string id=Text(Map(entry),"id");if(!result.Ids.Contains(id)||!seen.Add(id))continue;Dictionary<string,object> t;bool known=metadata.TryGetValue(id,out t)&&Text(t,"name").Length>0;var artists=known?Value(t,"artists")as object[]:null;result.Songs.Add(new LikedSong{Id=id,Name=known?Text(t,"name"):"歌曲 "+id,Artist=known?String.Join(" / ",(artists??new object[0]).Select(x=>Text(Map(x),"name"))):"尚无本地歌曲信息",MetadataKnown=known});}
   result.Available=true;result.PlaylistId=selectedId;result.Message="已读取 "+result.Songs.Count+" 首 · 来自网易云本地缓存 · "+result.ReadAt.ToString("HH:mm:ss");
  }catch(Exception ex){result.Message="读取歌单失败："+ex.Message;}
  return result;
 }
}
