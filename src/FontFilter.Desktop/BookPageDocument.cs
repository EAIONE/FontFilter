using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using SkiaSharp;
namespace FontFilter.Desktop;
public sealed record BookPageDocument(SKTypeface Glyph,string Text,double Width,double Height,double TextWidth,double TextHeight,double Left,double Top,double Right,double Bottom,double Size,double Leading,double Tracking)
{
 public Grid CreateSheet(bool guides)
 {
  var preview=new GlyphPreview{Glyph=Glyph,Text=Text,Size=Size,LineSpacing=Leading,Tracking=Tracking,Width=TextWidth,PreformattedLines=true};
  var block=new Border{Name="BookTextBlock",BorderBrush=new SolidColorBrush(Color.Parse("#CBD8D2")),BorderThickness=new Thickness(guides?1:0),Padding=new Thickness(guides?0:1),Child=preview,Width=TextWidth+2,Height=TextHeight+2,ClipToBounds=true,Margin=new Thickness(Left,Top,Right,Bottom),VerticalAlignment=VerticalAlignment.Top};
  var sheet=new Grid{Name="BookSheet",Width=Width,Height=Height,Background=Brushes.White,ClipToBounds=true};sheet.Children.Add(block);sheet.Children.Add(new TextBlock{Text="1",FontSize=10,Foreground=Brushes.Gray,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(0,0,0,12)});return sheet;
 }
}
