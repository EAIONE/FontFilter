using Avalonia;
using Avalonia.Media;
namespace FontFilter.Desktop;
public static class ThemeService
{
 public const string DefaultColor=ThemePalette.DefaultPrimary;
 public static bool IsValid(string? hex)=>ThemePalette.IsValid(hex);
 public static Color TextColor(Color c){double L(byte b){var x=b/255d;return x<=.04045?x/12.92:Math.Pow((x+.055)/1.055,2.4);}return .2126*L(c.R)+.7152*L(c.G)+.0722*L(c.B)>.179?Colors.Black:Colors.White;}
 public static void Apply(UserSettings settings)
 {
  var palette=ThemePalette.From(settings);var primary=Color.Parse(palette.Primary);var secondary=Color.Parse(palette.Secondary);var accent=Color.Parse(palette.Accent);var header=Color.Parse(palette.Header);var r=Application.Current!.Resources;
  void Brush(string key,Color color)=>r[key]=new SolidColorBrush(color);
  Brush("AccentBrush",accent);Brush("AccentTextBrush",TextColor(accent));Brush("HeaderBrush",header);Brush("HeaderTextBrush",TextColor(header));
  var button=Color.FromRgb((byte)(header.R*.94),(byte)(header.G*.94),(byte)(header.B*.94));Brush("HeaderButtonBrush",button);Brush("HeaderButtonTextBrush",TextColor(button));
  Brush("SoftBrush",secondary);Brush("SoftTextBrush",TextColor(secondary));Brush("InkBrush",Color.FromRgb((byte)(primary.R*.4),(byte)(primary.G*.4),(byte)(primary.B*.4)));Brush("ScrollThumbBrush",TextColor(primary)==Colors.Black?Color.FromRgb((byte)(primary.R*.55),(byte)(primary.G*.55),(byte)(primary.B*.55)):primary);Brush("ScrollTrackBrush",Color.FromRgb((byte)(secondary.R*.25+255*.75),(byte)(secondary.G*.25+255*.75),(byte)(secondary.B*.25+255*.75)));
  foreach(var key in new[]{"SystemAccentColor","SystemAccentColorDark1","SystemAccentColorDark2","SystemAccentColorDark3","SystemAccentColorLight1","SystemAccentColorLight2","SystemAccentColorLight3"})r[key]=accent;
 }
}
