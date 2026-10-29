using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;
namespace FontFilter.Desktop;
public sealed class GlyphPreview:Control
{
 public SKTypeface? Glyph{get;set;}public string Text{get;set;}="";public double Size{get;set;}=32;public double LineSpacing{get;set;}=1.6;
 public double Tracking{get;set;}public bool PreformattedLines{get;set;}
 double cachedWidth=-1,cachedHeight;PlacedGlyph[] positions=Array.Empty<PlacedGlyph>();
 public string Missing=>Glyph==null?"":GlyphCoverage.Missing(Text,v=>Glyph.GetGlyph(v)!=0);

 protected override Size MeasureOverride(Size availableSize){var width=double.IsFinite(availableSize.Width)?Math.Max(80,availableSize.Width):500;Layout(width);return new Size(width,cachedHeight);}
 void Layout(double width)
 {
  if(Glyph==null){cachedHeight=70;return;}if(cachedWidth==width)return;cachedWidth=width;
  using var paint=new SKPaint{Typeface=Glyph,TextSize=(float)Size,IsAntialias=true};
  float x=0,y=(float)Size,step=(float)(Size*LineSpacing);var list=new List<PlacedGlyph>();
  foreach(var rune in Text.Replace("\r","").EnumerateRunes()){
   if(rune.Value=='\n'){x=0;y+=step;continue;}
   if(rune.Value==0x3000){x+=(float)(Size+Tracking);continue;}
   var text=rune.Value=='\t'?"    ":rune.ToString();var index=Glyph.GetGlyph(rune.Value);var advance=index==0?(float)Size:Math.Max((float)Size*.08f,paint.MeasureText(text));
   if(!PreformattedLines&&x+advance>width&&x>0){x=0;y+=step;}
   list.Add(new(text,x,y,index==0));x+=Math.Max(1,advance+(float)Tracking);
  }
  positions=list.ToArray();cachedHeight=y+step*.45;
 }
 public override void Render(DrawingContext context){base.Render(context);if(Glyph==null)return;Layout(Math.Max(80,Bounds.Width));context.Custom(new GlyphDrawOperation(new Rect(0,0,cachedWidth,cachedHeight),Glyph,(float)Size,positions));}
 readonly record struct PlacedGlyph(string Text,float X,float Y,bool Missing);
 sealed class GlyphDrawOperation:ICustomDrawOperation
 {
  public Rect Bounds{get;}readonly SKPaint paint;readonly float size;readonly PlacedGlyph[] positions;
  public GlyphDrawOperation(Rect bounds,SKTypeface glyph,float size,PlacedGlyph[] positions){Bounds=bounds;this.size=size;this.positions=positions;paint=new SKPaint{Typeface=glyph,TextSize=size,IsAntialias=true,Color=SKColors.Black};}
  public bool HitTest(Point p)=>Bounds.Contains(p);
  public bool Equals(ICustomDrawOperation? other)=>ReferenceEquals(this,other);
  public void Dispose()=>paint.Dispose();
  public void Render(ImmediateDrawingContext context)
  {
   var feature=context.TryGetFeature<ISkiaSharpApiLeaseFeature>();if(feature==null)return;
   using var lease=feature.Lease();var canvas=lease.SkCanvas;var clip=canvas.LocalClipBounds;paint.Color=SKColors.Black.WithAlpha((byte)(255*lease.CurrentOpacity));
   using var missingPaint=new SKPaint{Color=SKColors.IndianRed.WithAlpha((byte)(255*lease.CurrentOpacity)),Style=SKPaintStyle.Stroke,StrokeWidth=1,IsAntialias=true};
   foreach(var g in positions){if(g.Y+size<clip.Top||g.Y-size>clip.Bottom)continue;if(g.Missing)canvas.DrawRect(g.X+2,g.Y-size*.8f,size*.75f,size*.85f,missingPaint);else canvas.DrawText(g.Text,g.X,g.Y,paint);}
  }
 }
}
