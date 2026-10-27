using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
namespace FontFilter.Desktop;
public sealed class GlyphPreview:Control
{
 public IGlyphTypeface? Glyph{get;set;}
 public string Text{get;set;}="";public double Size{get;set;}=32;public double LineSpacing{get;set;}=1.6;
 public string Missing=>Glyph==null?"":GlyphCoverage.Missing(Text,v=>Glyph.GetGlyph((uint)v)!=0);
 protected override Size MeasureOverride(Size availableSize)
 {
  var width=double.IsFinite(availableSize.Width)?Math.Max(80,availableSize.Width):500;
  return new Size(width,Layout(width,null));
 }
 double Layout(double width,DrawingContext? context)
 {
  if(Glyph==null)return 70;var glyph=Glyph;double x=0,y=Size,step=Size*LineSpacing;
  var scale=Size/glyph.Metrics.DesignEmHeight;
  foreach(var rune in Text.Replace("\r","").EnumerateRunes()){
   if(rune.Value=='\n'){x=0;y+=step;continue;}
   var index=glyph.GetGlyph((uint)rune.Value);double advance=Math.Max(index==0?Size:Size*0.08,glyph.GetGlyphAdvance(index)*scale);
   if(x+advance>width&&x>0){x=0;y+=step;}
   if(context!=null){
    if(index==0){context.DrawRectangle(null,new Pen(Brushes.IndianRed,1),new Rect(x+2,y-Size*.8,Size*.75,Size*.85));}
    else{using var run=new GlyphRun(glyph,Size,rune.ToString().AsMemory(),new ushort[]{index},new Point(x,y));context.DrawGlyphRun(Brushes.Black,run);}
   }
   x+=advance;
  }
  return y+step*.45;
 }
 public override void Render(DrawingContext context){base.Render(context);Layout(Math.Max(80,Bounds.Width),context);}
}
