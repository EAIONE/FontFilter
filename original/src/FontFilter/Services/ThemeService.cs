using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
namespace FontFilter;
public static class ThemeService
{
 public const string DefaultColor="#16594D";
 public static string CurrentColor{get;private set;}=DefaultColor;
 public static bool IsValid(string? hex)=>hex!=null&&Regex.IsMatch(hex,@"^#[0-9a-fA-F]{6}$");
 public static Color TextColor(Color c){double Linear(byte b){var s=b/255d;return s<=0.04045?s/12.92:Math.Pow((s+0.055)/1.055,2.4);}return 0.2126*Linear(c.R)+0.7152*Linear(c.G)+0.0722*Linear(c.B)>0.179?Colors.Black:Colors.White;}
 static Color Mix(Color c,Color target,double amount)=>Color.FromRgb((byte)(c.R*(1-amount)+target.R*amount),(byte)(c.G*(1-amount)+target.G*amount),(byte)(c.B*(1-amount)+target.B*amount));
 public static void Apply(Window window,string? hex,bool remember=true){hex=IsValid(hex)?hex:DefaultColor;var accent=(Color)ColorConverter.ConvertFromString(hex!);if(remember)CurrentColor=hex!;void Set(string key,Color c)=>window.Resources[key]=new SolidColorBrush(c);Set("AccentBrush",accent);Set("AccentTextBrush",TextColor(accent));Set("AccentInkBrush",Mix(accent,Colors.Black,0.35));Set("SoftBrush",Mix(accent,Colors.White,0.9));Set("SelectionBorderBrush",Mix(accent,Colors.White,0.6));Set("HeaderBrush",Mix(accent,Colors.Black,0.65));Set("HeaderEndBrush",Mix(accent,Colors.Black,0.45));Set("HeaderButtonBrush",Mix(accent,Colors.Black,0.25));Set("HeaderBorderBrush",Mix(accent,Colors.White,0.18));Set("AccentBrightBrush",Mix(accent,Colors.White,0.38));Set("AccentBrightTextBrush",TextColor(Mix(accent,Colors.White,0.38)));Set("ScrollThumbBrush",Mix(accent,Colors.Black,0.25));Set("AccentHintBrush",Mix(accent,TextColor(accent),0.68));}
}
