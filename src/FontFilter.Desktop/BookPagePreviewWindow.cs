using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
namespace FontFilter.Desktop;
public sealed class BookPagePreviewWindow:Window
{
 readonly UserSettings settings;readonly Action save;BookPageDocument? document;
 readonly ComboBox mode=new(){ItemsSource=new[]{"等大 100%","适应窗口"},SelectedIndex=0,Width=160};
 readonly CheckBox guides=new(){Content="显示版心线",IsChecked=false,VerticalAlignment=VerticalAlignment.Center};
 readonly NumericUpDown measured=new(){Value=100,Minimum=20,Maximum=300,Increment=.1m,Width=120,FormatString="0.#"};
 readonly TextBlock scaleInfo=Ui.Text("",11),pageInfo=Ui.Text("",12);
 readonly Viewbox actual=new(){Stretch=Stretch.Uniform,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Top,Margin=new Thickness(24)},fit=new(){Stretch=Stretch.Uniform,Margin=new Thickness(24)};
 readonly ScrollViewer scroll;readonly ContentControl ruler=new();DisplayMeasurement? monitor;
 public double ActualScale{get;private set;}=1;public bool ShowsGuides=>guides.IsChecked==true;public Viewbox ActualPage=>actual;
 public BookPagePreviewWindow(Window owner,BookPageDocument page,UserSettings userSettings,Action saveSettings)
 {
  settings=userSettings;save=saveSettings;Title="书页独立预览";Width=1100;Height=900;MinWidth=680;MinHeight=500;WindowStartupLocation=WindowStartupLocation.CenterOwner;
  FontFamily=owner.FontFamily;var screen=owner.Screens.ScreenFromWindow(owner);if(screen!=null){Width=Math.Min(1400,screen.WorkingArea.Width/screen.Scaling*.9);Height=Math.Min(1300,screen.WorkingArea.Height/screen.Scaling*.9);}
  scroll=new ScrollViewer{Content=actual,HorizontalScrollBarVisibility=Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,VerticalScrollBarVisibility=Avalonia.Controls.Primitives.ScrollBarVisibility.Auto};
  var toolbar=Ui.Stack(Ui.Text("浏览设置",18),mode,guides);toolbar.Spacing=16;
  var instructions=Ui.Text("用实物尺测量下方标尺的两端距离，填入实测毫米数后校准。同一显示器会记住校准结果。",11);
  var calibration=Ui.Stack(instructions,new ScrollViewer{Content=ruler,HorizontalScrollBarVisibility=Avalonia.Controls.Primitives.ScrollBarVisibility.Auto},Ui.Stack(Ui.Text("实测 mm",11),measured,Ui.Button("校准并记住",Calibrate),Ui.Button("恢复屏幕估算",ResetCalibration)));calibration.Spacing=12;
  var calibrator=new Expander{Header="屏幕标尺校准",Content=calibration,IsExpanded=false};
  var sidebar=Ui.Stack(toolbar,pageInfo,scaleInfo,calibrator,Ui.Button("关闭预览",Close));sidebar.Margin=new Thickness(20,24);sidebar.Spacing=20;sidebar.Name="PreviewSidebar";
  var viewport=new Grid{Background=new SolidColorBrush(Color.Parse("#E7EBE8"))};viewport.Children.Add(scroll);viewport.Children.Add(fit);
  var root=Ui.Grid("*,260","*");Ui.Put(root,viewport);Ui.Put(root,new ScrollViewer{Content=sidebar,HorizontalScrollBarVisibility=Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled},1);Content=root;
  mode.SelectionChanged+=(_,_)=>ApplyMode();guides.IsCheckedChanged+=(_,_)=>RefreshPage();Opened+=(_,_)=>RefreshMonitor(true);PositionChanged+=(_,_)=>RefreshMonitor();SizeChanged+=(_,_)=>RefreshMonitor();
  document=page;RefreshMonitor(true);ApplyMode();
 }
 public void SetPage(BookPageDocument? page){document=page;RefreshPage();}
 void ApplyMode(){scroll.IsVisible=mode.SelectedIndex==0;fit.IsVisible=mode.SelectedIndex==1;}
 void RefreshMonitor(bool force=false)
 {
  var next=DisplayMeasurement.Read(this);if(!force&&next.Key==monitor?.Key)return;monitor=next;UpdateScale();
 }
 void UpdateScale()
 {
  if(monitor==null)return;var calibrated=settings.DisplayCalibrations.TryGetValue(monitor.Key,out var correction);ActualScale=monitor.Scale*(calibrated?correction:1);
  scaleInfo.Text=$"{monitor.Name} · "+(calibrated?"已按标尺校准":monitor.Estimated?"按屏幕尺寸估算，实物等大请先校准":"请用标尺校准实物尺寸")+"。等大时超出窗口的部分可滚动查看。";
  ruler.Content=CreateRuler(ActualScale);RefreshPage();
 }
 void Calibrate(){if(monitor==null)return;settings.DisplayCalibrations[monitor.Key]=PhysicalPageScale.Correct(settings.DisplayCalibrations.GetValueOrDefault(monitor.Key,1),(double)(measured.Value??100));save();measured.Value=100;UpdateScale();}
 void ResetCalibration(){if(monitor==null)return;settings.DisplayCalibrations.Remove(monitor.Key);save();UpdateScale();}
 void RefreshPage()
 {
  if(document==null){actual.Child=fit.Child=null;pageInfo.Text="当前没有可预览的书页";return;}
  actual.Child=document.CreateSheet(ShowsGuides);fit.Child=document.CreateSheet(ShowsGuides);actual.Width=document.Width*ActualScale;actual.Height=document.Height*ActualScale;
  pageInfo.Text=$"1 P · {document.Width/PhysicalPageScale.DipPerMillimeter:0.#} × {document.Height/PhysicalPageScale.DipPerMillimeter:0.#} mm · 独立预览";
 }
 static Control CreateRuler(double scale)
 {
  var width=100*PhysicalPageScale.DipPerMillimeter*scale;var canvas=new Canvas{Width=width+1,Height=40,Margin=new Thickness(2,8)};
  canvas.Children.Add(new Border{Width=width,Height=1,Background=Brushes.DarkSlateGray});
  for(var i=0;i<=10;i++){var tick=new Border{Width=1,Height=i%5==0?15:9,Background=Brushes.DarkSlateGray};Canvas.SetLeft(tick,width*i/10);canvas.Children.Add(tick);}
  foreach(var value in new[]{0,50,100}){var label=Ui.Text(value==100?"100 mm":value.ToString(),10,Brushes.DarkSlateGray);Canvas.SetLeft(label,value==0?0:value==100?width-37:width/2-8);Canvas.SetTop(label,19);canvas.Children.Add(label);}return canvas;
 }
}
