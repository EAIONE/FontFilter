using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace FontFilter;
public sealed class PreviewCard:Border
{
 public PreviewCard(FontRecord f,string text,double size,double lineHeight,bool compact=false)
 {
  Background=Brushes.White;BorderBrush=new SolidColorBrush(Color.FromRgb(220,228,233));BorderThickness=new Thickness(1);CornerRadius=new CornerRadius(14);Padding=new Thickness(compact?22:30);MinHeight=compact?0:320;Margin=new Thickness(0,0,0,16);
  var panel=new StackPanel();Child=panel;panel.Children.Add(new TextBlock{Text=compact?f.Label:"段落试排  /  SPECIMEN",FontSize=compact?12:10,FontWeight=FontWeights.Medium,Foreground=new SolidColorBrush(Color.FromRgb(99,117,130)),Margin=new Thickness(0,0,0,24)});
  try{var typeface=PreviewService.Resolve(f);var missing=PreviewService.Missing(typeface,text);
   if(missing.Length>0)panel.Children.Add(new TextBlock{Text="缺字："+missing+"（以下以缺字框呈现，不使用替代字体）",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.DarkOrange,FontSize=12,Margin=new Thickness(0,0,0,12)});
   typeface.TryGetGlyphTypeface(out var glyph);panel.Children.Add(new GlyphParagraph(glyph!,text,size,lineHeight));
  }catch(Exception ex){panel.Children.Add(new TextBlock{Text="预览不可用："+ex.Message,TextWrapping=TextWrapping.Wrap,Foreground=Brushes.Firebrick});}
 }
}
// Direct glyph runs avoid WPF's silent font fallback. Intended for Chinese specimen comparison.
internal sealed class GlyphParagraph:FrameworkElement
{
 readonly GlyphTypeface glyph;readonly string text;readonly double size,lineHeight;
 readonly List<(List<ushort> Indices,List<double> Widths)> lines=new();
 public GlyphParagraph(GlyphTypeface glyph,string text,double size,double lineHeight){this.glyph=glyph;this.text=text;this.size=size;this.lineHeight=lineHeight;}
 protected override Size MeasureOverride(Size available){lines.Clear();double max=double.IsInfinity(available.Width)?800:Math.Max(20,available.Width);var indices=new List<ushort>();var widths=new List<double>();double width=0;
  void Flush(){lines.Add((indices,widths));indices=new();widths=new();width=0;}
  foreach(var rune in text.EnumerateRunes()){if(rune.Value=='\r')continue;if(rune.Value=='\n'){Flush();continue;}var code=rune.Value=='\t'?' ':rune.Value;glyph.CharacterToGlyphMap.TryGetValue(code,out var index);var advance=glyph.AdvanceWidths.TryGetValue(index,out var value)?value*size:size;if(width+advance>max&&indices.Count>0)Flush();indices.Add(index);widths.Add(advance);width+=advance;}if(indices.Count>0||lines.Count==0)Flush();return new Size(max,Math.Max(size*lineHeight,lines.Count*size*lineHeight));
 }
 protected override void OnRender(DrawingContext dc){base.OnRender(dc);double y=glyph.Baseline*size;var dpi=(float)VisualTreeHelper.GetDpi(this).PixelsPerDip;foreach(var line in lines){if(line.Indices.Count>0){var run=new GlyphRun(glyph,0,false,size,dpi,line.Indices,new Point(0,y),line.Widths,null,null,null,null,null,null);dc.DrawGlyphRun(new SolidColorBrush(Color.FromRgb(35,39,46)),run);}y+=size*lineHeight;}}
}
