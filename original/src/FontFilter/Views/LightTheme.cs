using System.Windows;
using System.Windows.Media;
namespace FontFilter;
public static class LightTheme
{
 public static void Apply(Window window){window.Resources.MergedDictionaries.Add(new ResourceDictionary{Source=new Uri("/FontFilter;component/Themes/Light.xaml",UriKind.Relative)});window.Background=new SolidColorBrush(Color.FromRgb(246,245,241));window.Foreground=new SolidColorBrush(Color.FromRgb(37,53,47));window.FontFamily=new FontFamily("Microsoft YaHei UI");window.FontSize=12;ThemeService.Apply(window,ThemeService.CurrentColor,false);}
}
