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
  var current=tabs.SelectedIndex==2?bookCandidate??State.Selected:State.Selected;
  if(current is not {} font){await Dialogs.Message(this,"先选择字体","从左侧选择字体后，可打开它在本机的位置。");return;}
  status.Text="正在定位 "+font.Label+" 的字体文件…";
  string? path;
  try{path=await FontFileLocator.FindAsync(font);}
  catch(Exception ex){status.Text="定位失败："+ex.Message;await Dialogs.Message(this,"定位失败","读取该字体的本机信息时出错：\n"+ex.Message);return;}
  if(path==null)
  {
   status.Text="未找到 "+font.Label+" 的字体文件";
   await Dialogs.Message(this,"未找到可打开的字体文件","该字体可能是系统内置、受保护或暂不可定位的字体。\n可以先用右侧「打开数据文件夹」查看索引，或尝试其他字体。");
   return;
  }
  if(!File.Exists(path))
  {
   status.Text="字体文件已不存在："+Path.GetFileName(path);
   await Dialogs.Message(this,"字体文件已不存在","索引里的位置已经失效，可能字体被卸载或移动。请重新扫描字体库后再试。");
   return;
  }
  var outcome=FileReveal.Reveal(path,out var detail);
  var fileName=Path.GetFileName(path);
  switch(outcome)
  {
   case FileReveal.Outcome.Selected:
    status.Text="已在文件管理器中定位："+fileName;
    return;
   case FileReveal.Outcome.OpenedFontLibrary:
    status.Text="已打开系统字体库 "+detail+"；字体文件是 "+fileName+"，在库中按「"+font.Family+"」查找（Windows 不允许程序在字体库里高亮单个文件）";
    return;
   case FileReveal.Outcome.OpenedFolder:
    status.Text="已打开字体所在文件夹："+detail;
    return;
   default:
    status.Text="无法打开字体位置："+detail;
    await Dialogs.Message(this,"无法打开字体位置",detail+"\n\n字体文件路径：\n"+path);
    return;
  }
 }
}
