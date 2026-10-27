using System.IO;
using System.Windows;
namespace FontFilter;
public partial class App:Application
{
 public static string DataDirectory=>Environment.GetEnvironmentVariable("FONTFILTER_DATA")??Path.Combine(AppContext.BaseDirectory,"Data");
 Mutex? mutex;
 protected override void OnStartup(StartupEventArgs e){base.OnStartup(e);mutex=new Mutex(true,"Local\\FontFilterDesktop",out var first);if(!first){MessageBox.Show("字里行间已在运行。");Shutdown();return;}try{Directory.CreateDirectory(DataDirectory);new MainWindow().Show();}catch(Exception ex){MessageBox.Show("启动失败："+ex.Message);Shutdown(1);}}
 protected override void OnExit(ExitEventArgs e){mutex?.Dispose();base.OnExit(e);}
}
