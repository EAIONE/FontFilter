using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using SkiaSharp;
namespace FontFilter.Desktop;
public sealed partial class MainWindow
{
 readonly StackPanel bookPages=new(){Spacing=20,HorizontalAlignment=HorizontalAlignment.Center};
 readonly NumericUpDown pageTop=PageNumber(20,0,70),pageBottom=PageNumber(20,0,70),pageInner=PageNumber(22,0,60),pageOuter=PageNumber(18,0,60),pageSize=PageNumber(12,6,48),pageLeading=PageNumber(1.6m,1,3,.1m),pageTracking=PageNumber(0,-2,10,.1m);
 static NumericUpDown PageNumber(decimal value,decimal min,decimal max,decimal increment=1)=>new(){Value=value,Minimum=min,Maximum=max,Increment=increment,Width=126,FormatString="0.#"};
 TextBlock CopyableName(string label,double size,string? family=null){var block=Ui.Text(label,size);block.Cursor=new Cursor(StandardCursorType.Hand);ToolTip.SetTip(block,"点击复制字体名称");block.Tapped+=async(_,_)=>await CopyFontName(family??label);return block;}
 async Task CopyFontName(string name){try{if(Clipboard is {} clipboard){await clipboard.SetTextAsync(name);status.Text="已复制字体名称："+name;}}catch(Exception ex){status.Text="复制失败："+ex.Message;}}
 Control BuildPageTypesetting()
 {
  var inputs=new[]{pageTop,pageBottom,pageInner,pageOuter,pageSize,pageLeading,pageTracking};for(var i=0;i<inputs.Length;i++)inputs[i].Value=(decimal)Settings.PageLayout[i];
  var controls=new WrapPanel{Orientation=Orientation.Horizontal};
  foreach(var pair in new[]{("天头 mm",pageTop),("地脚 mm",pageBottom),("订口 mm",pageInner),("切口 mm",pageOuter),("字号 pt",pageSize),("行距 倍",pageLeading),("字距 pt",pageTracking)}){var field=Ui.Stack(Ui.Text(pair.Item1,10),pair.Item2);field.Spacing=3;field.Margin=new Thickness(0,0,10,8);controls.Children.Add(field);pair.Item2.ValueChanged+=(_,_)=>{if(ready){Settings.PageLayout=inputs.Select(v=>(double)(v.Value??0)).ToArray();SaveSettings();RenderPageTypesetting();}};}
  var layout=Ui.Grid("*","Auto,Auto,*");Ui.Put(layout,controls);Ui.Put(layout,Ui.Text("A5 · 148 × 210 mm · 细框为版心，左右页自动交换订口与切口；文字沿用试排样文。",10),0,1);Ui.Put(layout,new ScrollViewer{Content=bookPages,HorizontalScrollBarVisibility=Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,Margin=new Thickness(0,12,0,0)},0,2);return layout;
 }
 void RenderPageTypesetting()
 {
  bookPages.Children.Clear();if(State.Selected is not {} font)return;
  try{
   var glyph=FontScanner.Resolve(font);const double mm=96/25.4,pt=96/72.0;var top=(double)(pageTop.Value??20)*mm;var bottom=(double)(pageBottom.Value??20)*mm;var inner=(double)(pageInner.Value??22)*mm;var outer=(double)(pageOuter.Value??18)*mm;var width=148*mm;var height=210*mm;var textWidth=width-inner-outer;var textHeight=height-top-bottom;var size=(double)(pageSize.Value??12)*pt;var leading=(double)(pageLeading.Value??1.6m);var tracking=(double)(pageTracking.Value??0)*pt;
   if(textWidth<size||textHeight<size*leading){bookPages.Children.Add(Ui.Text("边距过大，请为版心留出至少一行文字的空间。",13));return;}
   var lines=new List<string>();var line=new System.Text.StringBuilder();double used=0;using var paint=new SKPaint{Typeface=glyph,TextSize=(float)size};
   foreach(var rune in CurrentText().Replace("\r","").EnumerateRunes()){
    if(rune.Value=='\n'){lines.Add(line.ToString());line.Clear();used=0;continue;}
    var advance=glyph.GetGlyph(rune.Value)==0?size:Math.Max(size*.08,paint.MeasureText(rune.ToString()));if(used+advance>textWidth&&line.Length>0){lines.Add(line.ToString());line.Clear();used=0;}line.Append(rune.ToString());used+=Math.Max(1,advance+tracking);
   }
   lines.Add(line.ToString());var perPage=Math.Max(1,(int)Math.Floor((textHeight-size-Math.Max(0,paint.FontMetrics.Descent))/(size*leading))+1);var total=Math.Max(1,(lines.Count+perPage-1)/perPage);
   for(var page=0;page<total;page++){
    var left=page%2==0?inner:outer;var right=page%2==0?outer:inner;
    var preview=new GlyphPreview{Glyph=glyph,Text=string.Join("\n",lines.Skip(page*perPage).Take(perPage)),Size=size,LineSpacing=leading,Tracking=tracking,Width=textWidth};
    var block=new Border{BorderBrush=new SolidColorBrush(Color.Parse("#CBD8D2")),BorderThickness=new Thickness(1),Child=preview,Width=textWidth+2,Height=textHeight+2,ClipToBounds=true,Margin=new Thickness(left,top,right,bottom),VerticalAlignment=VerticalAlignment.Top};
    var sheet=new Grid{Width=width,Height=height,Background=Brushes.White,ClipToBounds=true};sheet.Children.Add(block);sheet.Children.Add(new TextBlock{Text=(page+1).ToString(),FontSize=10,Foreground=Brushes.Gray,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(0,0,0,12)});
    bookPages.Children.Add(new Border{Child=sheet,BorderBrush=new SolidColorBrush(Color.Parse("#DCE5DD")),BorderThickness=new Thickness(1),BoxShadow=new BoxShadows(new BoxShadow{Blur=10,OffsetY=3,Color=Color.Parse("#18000000")})});
   }
  }catch(Exception ex){bookPages.Children.Add(Ui.Text("页面试排未能显示："+ex.Message,12));}
 }
}
