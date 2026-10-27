using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
namespace FontFilter.Desktop;
public sealed partial class MainWindow
{
 void WireWheelAdjustments()
 {
  faceBox.AddHandler(InputElement.PointerWheelChangedEvent,(_,e)=>{if(faceBox.IsDropDownOpen||faceBox.ItemCount==0||e.Delta.Y==0)return;faceBox.SelectedIndex=Math.Clamp(faceBox.SelectedIndex-(e.Delta.Y>0?1:-1),0,faceBox.ItemCount-1);e.Handled=true;},RoutingStrategies.Tunnel);
  void NumericWheel(NumericUpDown input,decimal fallback){input.AddHandler(InputElement.PointerWheelChangedEvent,(_,e)=>{if(e.Delta.Y==0)return;input.Value=Math.Clamp((input.Value??fallback)+(e.Delta.Y>0?input.Increment:-input.Increment),input.Minimum,input.Maximum);e.Handled=true;},RoutingStrategies.Tunnel);}
  NumericWheel(sizeInput,32);NumericWheel(lineInput,1.6m);
 }
 async Task RevealCurrentFont()
 {
  if(State.Selected is not {} font){await Dialogs.Message(this,"先选择字体","从左侧选择字体后，可打开它在本机的位置。");return;}
  var path=await FontFileLocator.FindAsync(font);if(path==null){await Dialogs.Message(this,"未找到可打开的字体文件","该字体可能是系统内置、受保护或暂不可定位的字体。请尝试其他字体。");return;}
  var start=new ProcessStartInfo{UseShellExecute=false};
  if(OperatingSystem.IsMacOS()){start.FileName="/usr/bin/open";start.ArgumentList.Add("-R");start.ArgumentList.Add(path);}
  else if(OperatingSystem.IsWindows()){start.FileName="explorer.exe";start.Arguments="/select,\""+path+"\"";}
  else{await OpenFolder(Path.GetDirectoryName(path)!);return;}
  Process.Start(start);status.Text="已定位："+Path.GetFileName(path);
 }
}
