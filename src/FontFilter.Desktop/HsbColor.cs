using Avalonia.Media;
namespace FontFilter.Desktop;
public readonly record struct HsbColor(double H,double S,double B)
{
 public static HsbColor FromColor(Color color){double r=color.R/255d,g=color.G/255d,b=color.B/255d;var max=Math.Max(r,Math.Max(g,b));var min=Math.Min(r,Math.Min(g,b));var delta=max-min;double hue=0;if(delta>0){hue=max==r?60*((g-b)/delta%6):max==g?60*((b-r)/delta+2):60*((r-g)/delta+4);if(hue<0)hue+=360;}return new(hue,max==0?0:delta/max*100,max*100);}
 public static Color ToColor(double hue,double saturation,double brightness){hue=(hue%360+360)%360;var s=Math.Clamp(saturation,0,100)/100;var v=Math.Clamp(brightness,0,100)/100;var c=v*s;var x=c*(1-Math.Abs(hue/60%2-1));var m=v-c;var (r,g,b)=hue switch{<60=>(c,x,0d),<120=>(x,c,0d),<180=>(0d,c,x),<240=>(0d,x,c),<300=>(x,0d,c),_=>(c,0d,x)};return Color.FromRgb((byte)Math.Round((r+m)*255),(byte)Math.Round((g+m)*255),(byte)Math.Round((b+m)*255));}
}
