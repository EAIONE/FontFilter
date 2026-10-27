using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
namespace FontFilter.Desktop;
public sealed class DesktopApp:Application
{
 public override void Initialize(){AvaloniaXamlLoader.Load(this);ThemeService.Apply(new UserSettings());}
 public override void OnFrameworkInitializationCompleted()
 {
  if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop){
   try{
    Directory.CreateDirectory(FontFilter.App.DataDirectory);
    if(Program.AlreadyRunning){var close=new Avalonia.Controls.Button{Content="知道了"};var content=Ui.Stack(Ui.Heading("字里行间已在运行"),Ui.Text("请切换到已经打开的窗口。"),close);content.Margin=new Thickness(24);var message=Ui.Window("字里行间",content,480,260);close.Click+=(_,_)=>message.Close();desktop.MainWindow=message;base.OnFrameworkInitializationCompleted();return;}
    var window=new MainWindow();desktop.MainWindow=window;
    if(Program.Arguments.Contains("--verify")||Program.Arguments.Contains("--verify-theme"))window.Opened+=async(_,_)=>{try{if(Program.Arguments.Contains("--verify-theme"))await window.VerifyLiveTheme();else await Verification.Run(window);desktop.Shutdown(0);}catch(Exception ex){File.WriteAllText(Path.Combine(FontFilter.App.DataDirectory,"verify-error.txt"),ex.ToString());desktop.Shutdown(1);}};
   }catch(Exception ex){File.WriteAllText(Path.Combine(FontFilter.App.DataDirectory,"startup-error.log"),ex.ToString());desktop.Shutdown(1);}
  }
  base.OnFrameworkInitializationCompleted();
 }
}
