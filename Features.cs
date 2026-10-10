using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;

public sealed partial class NetEaseControl {
 readonly string runtime=File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"ClientRuntime.js"),Encoding.UTF8);
 public Task<object> Call(string method,params object[] args){return Evaluate(runtime+";window.__cloudIslandV3."+method+"("+String.Join(",",args.Select(x=>json.Serialize(x)))+")");}
}
public sealed class SearchSong : System.ComponentModel.INotifyPropertyChanged {
 public string Id {get;set;} public string Name {get;set;} public string Detail {get;set;}
 public string CoverUrl {get;set;}
 ImageSource coverImage;
 public ImageSource Cover {get{return coverImage;}set{coverImage=value;var changed=PropertyChanged;if(changed!=null)changed(this,new System.ComponentModel.PropertyChangedEventArgs("Cover"));}}
 public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
}
public sealed partial class Island {
 readonly NetEaseControl nativeControl=new NetEaseControl();
 Dictionary<string,object> nativeState=new Dictionary<string,object>();DateTime nativeStamp,nextNativeAttempt;
 bool nativeBusy,seekSync,seekDragging,seekBusy,seekDirty,playBusy;DispatcherTimer seekTimer;Window searchWindow;
 bool NativeFresh {get{return (DateTime.UtcNow-nativeStamp).TotalSeconds<2&&Bool(nativeState,"connected");}}
 void BrandWindow(Window w){w.Icon=BitmapFrameForBrand();}
 void PanelWindow(Window w,UIElement content,string caption){
  w.Owner=window;w.Topmost=true;
  w.WindowStyle=WindowStyle.None;w.AllowsTransparency=true;w.Background=Brushes.Transparent;w.ResizeMode=ResizeMode.CanResize;
  var theme=(ResourceDictionary)XamlReader.Parse(File.ReadAllText(Path.Combine(source,"WindowTheme.xaml"),Encoding.UTF8));w.Resources.MergedDictionaries.Add(theme);
  var card=new Border{CornerRadius=new CornerRadius(22),Margin=new Thickness(12),Background=new LinearGradientBrush(Color.FromRgb(31,33,38),Color.FromRgb(27,28,33),90),BorderBrush=new SolidColorBrush(Color.FromArgb(42,220,232,228)),BorderThickness=new Thickness(1),Effect=new System.Windows.Media.Effects.DropShadowEffect{BlurRadius=22,ShadowDepth=6,Opacity=.32}};
  var grid=new Grid();grid.RowDefinitions.Add(new RowDefinition{Height=new GridLength(48)});grid.RowDefinitions.Add(new RowDefinition{Height=new GridLength(1,GridUnitType.Star)});card.Child=grid;w.Content=card;
  var header=new Grid{Background=Brushes.Transparent,Margin=new Thickness(20,0,20,0)};grid.Children.Add(header);
  header.Children.Add(new TextBlock{Text=caption,FontSize=12,FontWeight=FontWeights.Medium,Foreground=new SolidColorBrush(Color.FromRgb(161,166,174)),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,IsHitTestVisible=false});
  var dots=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Center};header.Children.Add(dots);
  string[] colors={"#FF6059","#FEBD2E","#28C840"},glyphs={"×","−","+"},tips={"关闭","最小化","放大 / 还原"};
  for(int i=0;i<3;i++){int index=i;var dot=new Button{Style=(Style)w.FindResource("WindowDot"),Background=(Brush)new BrushConverter().ConvertFromString(colors[i]),ToolTip=tips[i],Content=new TextBlock{Text=glyphs[i],FontSize=11,FontWeight=FontWeights.Bold,Foreground=Brushes.Black,Margin=new Thickness(0,-2,0,0)}};System.Windows.Automation.AutomationProperties.SetName(dot,tips[i]);System.Windows.Automation.AutomationProperties.SetAutomationId(dot,new[]{"WindowClose","WindowMinimize","WindowMaximize"}[i]);dot.Click+=(s,e)=>{if(index==0)w.Close();else if(index==1)w.WindowState=WindowState.Minimized;else w.WindowState=w.WindowState==WindowState.Maximized?WindowState.Normal:WindowState.Maximized;};dots.Children.Add(dot);}
  header.MouseLeftButtonDown+=(s,e)=>{if(e.ClickCount==2)w.WindowState=w.WindowState==WindowState.Maximized?WindowState.Normal:WindowState.Maximized;else try{w.DragMove();}catch{}};
  var body=new Border{Child=content,Margin=new Thickness(0,0,0,15)};Grid.SetRow(body,1);grid.Children.Add(body);
  var grip=new System.Windows.Shapes.Path{Data=Geometry.Parse("M0 10 L10 0 M5 10 L10 5"),Stroke=new SolidColorBrush(Color.FromRgb(86,91,98)),StrokeThickness=1,Width=14,Height=14,Margin=new Thickness(0,0,6,6),HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Bottom,Cursor=Cursors.SizeNWSE};Grid.SetRowSpan(grip,2);grid.Children.Add(grip);grip.MouseLeftButtonDown+=(s,e)=>{e.Handled=true;Native.ReleaseCapture();Native.SendMessage(new System.Windows.Interop.WindowInteropHelper(w).Handle,0xA1,new IntPtr(17),IntPtr.Zero);};
  w.StateChanged+=(s,e)=>{bool full=w.WindowState==WindowState.Maximized;card.Margin=new Thickness(full?0:12);card.CornerRadius=new CornerRadius(full?0:22);grip.Visibility=full?Visibility.Collapsed:Visibility.Visible;};
  w.KeyDown+=(s,e)=>{if(e.Key==Key.Escape){e.Handled=true;w.Close();}};
 }
 bool HasVisiblePanel(){return Application.Current.Windows.Cast<Window>().Any(w=>w!=window&&w.Owner==window&&w.IsVisible&&w.WindowState!=WindowState.Minimized);}
 void BuildAboutTab(StackPanel panel){
  panel.Children.Add(BrandHeading("云词岛","Cloud Lyric Island  ·  v"+AppInfo.Version));
  panel.Children.Add(Label("开发者：陈小铭",20));panel.Children.Add(Label("本软件由 B 站 UP 主「陈小铭」开发。",13));
  panel.Children.Add(Button("B 站主页 · 陈小铭",()=>Process.Start(new ProcessStartInfo(AppInfo.AuthorUrl){UseShellExecute=true})));
  panel.Children.Add(Label(AppInfo.Copyright,12));panel.Children.Add(Label("使用声明",18));
  panel.Children.Add(Label("独立开发的第三方歌词与播放辅助工具，与网易云音乐、网易或 Apple 无隶属、授权或合作关系。相关品牌、音乐、歌词、封面等内容归各自权利人所有。",13));
  panel.Children.Add(Label("搜索与播放依赖已安装的官方客户端及你的账号权限，不提供歌曲下载，不绕过会员、试听或版权限制。使用时请遵守服务条款及适用规则，不得用于侵权行为。",13));
  panel.Children.Add(Label("精确控制会启用仅监听本机的调试接口；同一电脑上的其他程序也可能访问。关闭网易云可关闭此接口。个人设置、歌词及封面缓存保存在本机；本工具不读取登录凭据或上传本地歌单数据库。",13));
  panel.Children.Add(Label("本软件按现状提供，不保证适配所有客户端版本、游戏或电脑环境。客户端升级可能导致功能失效。建议保留配置备份；本声明不排除适用规则下不可排除的责任。",13));
  panel.Children.Add(Label("完整声明与参考项目见发布包内 DISCLAIMER.md、THIRD_PARTY_NOTICES.md。",12));
 }
 readonly System.Threading.SemaphoreSlim coverDownloads=new System.Threading.SemaphoreSlim(4);
 readonly Dictionary<string,ImageSource> searchCovers=new Dictionary<string,ImageSource>();
 async Task LoadSearchCover(SearchSong song){
  Uri uri;if(!Uri.TryCreate(song.CoverUrl,UriKind.Absolute,out uri)||!uri.Host.EndsWith(".music.126.net",StringComparison.OrdinalIgnoreCase))return;
  string url="https://"+uri.Host+uri.AbsolutePath+"?param=112y112";
  await coverDownloads.WaitAsync();
  try{if(stopping)return;ImageSource image;if(!searchCovers.TryGetValue(url,out image)){var bytes=await http.GetByteArrayAsync(url);var bitmap=new System.Windows.Media.Imaging.BitmapImage();using(var stream=new MemoryStream(bytes)){bitmap.BeginInit();bitmap.CacheOption=System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;bitmap.StreamSource=stream;bitmap.DecodePixelWidth=112;bitmap.EndInit();bitmap.Freeze();}image=bitmap;if(searchCovers.Count>=180)searchCovers.Clear();searchCovers[url]=image;}if(!stopping)song.Cover=image;}catch{}finally{coverDownloads.Release();}
 }
 async void LoadSearchCovers(IEnumerable<SearchSong> songs){await Task.WhenAll(songs.Select(LoadSearchCover));}
 System.Windows.Media.Imaging.BitmapFrame BitmapFrameForBrand(){return System.Windows.Media.Imaging.BitmapFrame.Create(new Uri(Path.Combine(source,"assets","cloud-island.ico")));}
 FrameworkElement BrandHeading(string heading,string subtitle){
  var row=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,6,0,18)};
  var text=new StackPanel{VerticalAlignment=VerticalAlignment.Center};text.Children.Add(new TextBlock{Text=heading,FontSize=23,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White});text.Children.Add(new TextBlock{Text=subtitle,FontSize=11,Foreground=new SolidColorBrush(Color.FromRgb(151,166,175)),Margin=new Thickness(0,6,0,0)});row.Children.Add(text);return row;
 }
 void HideClient(){var h=Native.CloudWindow();if(h!=IntPtr.Zero)Native.ShowWindow(h,0);}
 async void PollNative(){
  if(nativeBusy||stopping||DateTime.UtcNow<nextNativeAttempt)return;nativeBusy=true;
  try{var state=Obj(await nativeControl.Call("state"));if(stopping)return;nativeState=state;nativeStamp=DateTime.UtcNow;}
  catch{nextNativeAttempt=DateTime.UtcNow.AddSeconds(3);}finally{nativeBusy=false;}
 }
 Dictionary<string,object> NativeTrack(){return new Dictionary<string,object>{{"id",Str(nativeState,"id")},{"duration",Num(nativeState,"duration")*1000},{"album",new Dictionary<string,object>{{"picUrl",Str(nativeState,"cover")}}}};}
 void SetProgress(double value){if(seekDragging||seekBusy||seekDirty)return;seekSync=true;progress.Value=value;seekSync=false;}
 void SetupSeeking(){
  seekTimer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(350)};seekTimer.Tick+=(s,e)=>{seekTimer.Stop();if(!seekDragging)CommitSeek();};
  progress.PreviewMouseLeftButtonDown+=(s,e)=>{seekDragging=true;};
  progress.PreviewMouseLeftButtonUp+=(s,e)=>{seekDragging=false;seekTimer.Stop();seekTimer.Start();};
  progress.AddHandler(Thumb.DragCompletedEvent,new DragCompletedEventHandler((s,e)=>{seekDragging=false;seekTimer.Stop();seekTimer.Start();}));
  progress.LostMouseCapture+=(s,e)=>{seekDragging=false;if(seekDirty){seekTimer.Stop();seekTimer.Start();}};
  progress.ValueChanged+=(s,e)=>{if(seekSync)return;seekDirty=true;time.Text=Clock(progress.Value*duration)+" / "+Clock(duration);seekTimer.Stop();seekTimer.Start();};
 }
 async void CommitSeek(){
  if(!seekDirty||seekBusy)return;seekDirty=false;
  if(!NativeFresh||duration<=0){Notify("精确控制尚未连接，请在搜索窗口启用本机控制");return;}
  double target=progress.Value*duration;string id=currentId;seekBusy=true;
  try{nativeState=Obj(await nativeControl.Call("seek",target,id));nativeStamp=DateTime.UtcNow;}
  catch(Exception ex){Notify("进度跳转失败："+ex.Message);}finally{seekBusy=false;if(!stopping)Tick();}
 }
 async void PlaySong(string id){
  if(playBusy||!System.Text.RegularExpressions.Regex.IsMatch(id??"",@"^\d+$"))return;playBusy=true;
  try{
   nativeState=Obj(await nativeControl.Call("play",id));nativeStamp=DateTime.UtcNow;lastError="";
  }catch(Exception ex){Notify("后台播放失败："+ex.Message+"；可在搜索窗口启用精确控制后重试");}finally{playBusy=false;}
 }
 async void EnableNative(TextBlock state){
  state.Text="正在连接…";
  try{await nativeControl.Call("state");state.Text="精确控制已连接，无需重启";return;}catch{}
  var executable=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"Netease","CloudMusic","cloudmusic.exe");
  foreach(var p in Process.GetProcessesByName("cloudmusic")){try{if(p.MainModule!=null)executable=p.MainModule.FileName;}catch{}p.Dispose();}
  if(!File.Exists(executable)){state.Text="未找到网易云安装位置，请先启动客户端";return;}
  try{
   foreach(var p in Process.GetProcessesByName("cloudmusic")){using(p){p.Kill();}}
   await Task.Delay(600);Process.Start(new ProcessStartInfo(executable,"--remote-debugging-address=127.0.0.1 --remote-debugging-port=9223"){WindowStyle=ProcessWindowStyle.Hidden});
   nextNativeAttempt=DateTime.MinValue;state.Text="网易云正在重新启动，请播放一首歌后重试";
  }catch(Exception ex){state.Text="连接失败："+ex.Message;}
 }
 void OpenSearch(){
  if(searchWindow!=null){searchWindow.Show();searchWindow.Activate();return;}
  var w=new Window{Title="云词岛 · 搜索播放",Width=760,Height=680,MinWidth=540,MinHeight=440,Topmost=true,Background=new SolidColorBrush(Color.FromRgb(25,25,33)),Foreground=Brushes.White,FontFamily=new FontFamily("Microsoft YaHei UI"),WindowStartupLocation=WindowStartupLocation.CenterScreen};searchWindow=w;
  BrandWindow(w);w.Resources=(ResourceDictionary)XamlReader.Parse(File.ReadAllText(Path.Combine(source,"SettingsTheme.xaml"),Encoding.UTF8));
  var root=new DockPanel{Margin=new Thickness(26,4,26,0)};PanelWindow(w,root,"搜索播放");var head=new StackPanel();DockPanel.SetDock(head,Dock.Top);root.Children.Add(head);
  head.Children.Add(BrandHeading("搜索音乐","找到想听的下一首"));var input=new TextBox{MinHeight=44,ToolTip="搜索歌名或歌手，按 Enter 搜索"};head.Children.Add(input);
  var info=Label(NativeFresh?"已连接网易云 · 双击结果即可播放":"精确控制未连接 · 下方可启用（将重启网易云）",12);
  var list=new ListBox{Background=Brushes.Transparent,BorderThickness=new Thickness(0)};ScrollViewer.SetHorizontalScrollBarVisibility(list,ScrollBarVisibility.Disabled);
  list.ItemTemplate=(DataTemplate)XamlReader.Parse("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Grid Margin='4,6'><Grid.ColumnDefinitions><ColumnDefinition Width='66'/><ColumnDefinition Width='*'/></Grid.ColumnDefinitions><Border Width='54' Height='54' CornerRadius='9' Background='#343842' VerticalAlignment='Center' HorizontalAlignment='Left'><Grid><TextBlock Text='♪' Foreground='#91AAAE' FontSize='24' HorizontalAlignment='Center' VerticalAlignment='Center'/><Border CornerRadius='9'><Border.Background><ImageBrush ImageSource='{Binding Cover}' Stretch='UniformToFill'/></Border.Background></Border></Grid></Border><StackPanel Grid.Column='1' VerticalAlignment='Center'><TextBlock Text='{Binding Name}' Foreground='#F0F0F7' FontSize='15' TextTrimming='CharacterEllipsis'/><TextBlock Text='{Binding Detail}' Foreground='#9797A8' FontSize='12' Margin='0,5,0,0' TextWrapping='Wrap'/></StackPanel></Grid></DataTemplate>");
  bool busy=false;int offset=0,total=0;string lastQuery="";
  Func<int,Task> run=async page=>{
   if(busy||input.Text.Trim().Length==0)return;busy=true;string query=input.Text.Trim();info.Text="正在搜索…";
   try{var result=Obj(await nativeControl.Call("search",query,page));if(searchWindow!=w)return;var songs=Get(result,"songs")as object[]??new object[0];var rows=songs.Select(x=>{var t=Obj(x);return new SearchSong{Id=Str(t,"id"),Name=Str(t,"name"),CoverUrl=Str(t,"cover"),Detail=Str(t,"artist")+"  ·  "+Str(t,"album")+"  ·  "+Clock(Num(t,"duration"))+(Num(t,"fee")==1?"  ·  VIP":"")};}).ToList();list.ItemsSource=rows;LoadSearchCovers(rows);offset=page;total=(int)Num(result,"total");lastQuery=query;info.Text=songs.Length==0?"没有找到相关歌曲":"共 "+total+" 首 · 当前 "+(offset+1)+"–"+(offset+songs.Length)+" · 双击播放";}
   catch(Exception ex){info.Text="搜索失败："+ex.Message+"。若未开启控制，请点击下方启用按钮。";}finally{busy=false;}
  };
  var actions=new WrapPanel();actions.Children.Add(Button("搜索",async()=>await run(0)));actions.Children.Add(Button("播放选中",()=>{var song=list.SelectedItem as SearchSong;if(song!=null)PlaySong(song.Id);}));actions.Children.Add(Button("我喜欢的音乐",()=>{OpenSettings();settingsTabs.SelectedIndex=3;}));actions.Children.Add(Button("收起网易云窗口",HideClient));head.Children.Add(actions);head.Children.Add(info);
  input.KeyDown+=async(s,e)=>{if(e.Key==Key.Enter){e.Handled=true;await run(0);}};list.MouseDoubleClick+=(s,e)=>{var song=list.SelectedItem as SearchSong;if(song!=null)PlaySong(song.Id);};
  var footer=new StackPanel();DockPanel.SetDock(footer,Dock.Bottom);root.Children.Add(footer);var paging=new WrapPanel();paging.Children.Add(Button("上一页",async()=>{if(offset>0&&input.Text.Trim()==lastQuery)await run(Math.Max(0,offset-30));}));paging.Children.Add(Button("下一页",async()=>{if(offset+30<total&&input.Text.Trim()==lastQuery)await run(offset+30);}));var connection=new StackPanel{Visibility=Visibility.Collapsed};connection.Children.Add(Button("启用精确控制（未连接时将重启网易云）",()=>EnableNative(info)));paging.Children.Add(Button("连接选项",()=>connection.Visibility=connection.Visibility==Visibility.Visible?Visibility.Collapsed:Visibility.Visible));footer.Children.Add(paging);footer.Children.Add(connection);root.Children.Add(list);
  w.Closed+=(s,e)=>searchWindow=null;w.Show();input.Focus();
 }
}
