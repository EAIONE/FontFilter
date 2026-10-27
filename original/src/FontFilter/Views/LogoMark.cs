using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace FontFilter;
public sealed class LogoMark:FrameworkElement
{
 public static readonly DependencyProperty AccentProperty=DependencyProperty.Register(nameof(Accent),typeof(Brush),typeof(LogoMark),new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(22,89,77)),FrameworkPropertyMetadataOptions.AffectsRender));
 public Brush Accent{get=>(Brush)GetValue(AccentProperty);set=>SetValue(AccentProperty,value);}
 protected override void OnRender(DrawingContext dc){base.OnRender(dc);var side=Math.Min(ActualWidth,ActualHeight);dc.PushTransform(new ScaleTransform(side/128,side/128));Draw(dc,Accent);dc.Pop();}
 public static void Draw(DrawingContext dc,Brush accent){dc.DrawRoundedRectangle(accent,null,new Rect(0,0,128,128),26,26);var pen=new Pen(new SolidColorBrush(Color.FromArgb(85,255,255,255)),1.25){DashStyle=new DashStyle(new double[]{3,3},0)};dc.DrawRectangle(null,pen,new Rect(16,16,96,96));dc.DrawLine(pen,new Point(64,16),new Point(64,112));dc.DrawLine(pen,new Point(16,64),new Point(112,64));var text=new FormattedText("字",CultureInfo.GetCultureInfo("zh-CN"),FlowDirection.LeftToRight,new Typeface(new FontFamily("Microsoft YaHei"),FontStyles.Normal,FontWeights.Bold,FontStretches.Normal),82,Brushes.White,1);var geometry=text.BuildGeometry(new Point());var bounds=geometry.Bounds;dc.PushTransform(new TranslateTransform((128-bounds.Width)/2-bounds.X,(128-bounds.Height)/2-bounds.Y));dc.DrawGeometry(Brushes.White,null,geometry);dc.Pop();}
 public static BitmapSource Bitmap(int size){var visual=new DrawingVisual();using(var dc=visual.RenderOpen()){dc.PushTransform(new ScaleTransform(size/128d,size/128d));Draw(dc,new SolidColorBrush(Color.FromRgb(22,89,77)));dc.Pop();}var bitmap=new RenderTargetBitmap(size,size,96,96,PixelFormats.Pbgra32);bitmap.Render(visual);return bitmap;}
}
