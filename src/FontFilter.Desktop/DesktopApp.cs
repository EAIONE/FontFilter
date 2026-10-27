using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
namespace FontFilter.Desktop;
public sealed class DesktopApp:Application
{
 public override void Initialize(){AvaloniaXamlLoader.Load(this);ThemeService.Apply("#16594D");}
 public override void OnFrameworkInitializationCompleted()
 {
  if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop){
   try{
    Directory.CreateDirectory(FontFilter.App.DataDirectory);
    if(Program.AlreadyRunning){var close=new Avalonia.Controls.Button{Content="知道了"};var content=Ui.Stack(Ui.Heading("字里行间已在运行"),Ui.Text("请切换到已经打开的窗口。"),close);content.Margin=new Thickness(24);var message=Ui.Window("字里行间",content,480,260);close.Click+=(_,_)=>message.Close();desktop.MainWindow=message;base.OnFrameworkInitializationCompleted();return;}
    var window=new MainWindow();desktop.MainWindow=window;
    if(Program.Arguments.Contains("--verify"))window.Opened+=async(_,_)=>{try{await Verification.Run(window);desktop.Shutdown(0);}catch(Exception ex){File.WriteAllText(Path.Combine(FontFilter.App.DataDirectory,"verify-error.txt"),ex.ToString());desktop.Shutdown(1);}};
   }catch(Exception ex){File.WriteAllText(Path.Combine(FontFilter.App.DataDirectory,"startup-error.log"),ex.ToString());desktop.Shutdown(1);}
  }
  base.OnFrameworkInitializationCompleted();
 }
}
public static class ThemeService
{
 public const string DefaultColor="#16594D";
 public static bool IsValid(string? hex)=>hex!=null&&System.Text.RegularExpressions.Regex.IsMatch(hex,"^#[0-9a-fA-F]{6}$");
 public static Color TextColor(Color c){double L(byte b){var x=b/255d;return x<=.04045?x/12.92:Math.Pow((x+.055)/1.055,2.4);}return .2126*L(c.R)+.7152*L(c.G)+.0722*L(c.B)>.179?Colors.Black:Colors.White;}
 public static void Apply(string? hex){var color=Color.Parse(IsValid(hex)?hex!:DefaultColor);var r=Application.Current!.Resources;r["AccentBrush"]=new SolidColorBrush(color);r["SystemAccentColor"]=color;r["SystemAccentColorDark1"]=color;r["SystemAccentColorLight1"]=color;r["HeaderBrush"]=new SolidColorBrush(Color.FromRgb((byte)(color.R*.45),(byte)(color.G*.45),(byte)(color.B*.45)));r["HeaderButtonBrush"]=new SolidColorBrush(Color.FromRgb((byte)(color.R*.75),(byte)(color.G*.75),(byte)(color.B*.75)));r["AccentTextBrush"]=new SolidColorBrush(TextColor(color));Color Mix(double a)=>Color.FromRgb((byte)(color.R*(1-a)+255*a),(byte)(color.G*(1-a)+255*a),(byte)(color.B*(1-a)+255*a));r["SoftBrush"]=new SolidColorBrush(Mix(.91));r["InkBrush"]=new SolidColorBrush(Color.FromRgb((byte)(color.R*.65),(byte)(color.G*.65),(byte)(color.B*.65)));}
}
