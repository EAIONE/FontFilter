using Avalonia;
namespace FontFilter.Desktop;
public static class Program
{
 public static bool AlreadyRunning;
 public static string[] Arguments=Array.Empty<string>();
 [STAThread] public static int Main(string[] args)
 {
  Arguments=args;
  if(UpdateHandoff.DeferIfUpdating(FontFilter.App.DataDirectory))return 0;
  if(UpdateHandoff.NeedsFreshLaunch(Path.Combine(AppContext.BaseDirectory,"FontFilter.dll"),typeof(Program).Assembly.GetName().Version!.ToString(3))){
   var start=new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=false,CreateNoWindow=true};foreach(var arg in args)start.ArgumentList.Add(arg);System.Diagnostics.Process.Start(start)?.Dispose();return 0;
  }
  IDisposable? instance=null;
  try{try{instance=AppInstance.Acquire(FontFilter.App.DataDirectory);}catch(IOException){if(UpdateHandoff.DeferIfUpdating(FontFilter.App.DataDirectory))return 0;AlreadyRunning=true;}return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);}
  catch(Exception ex){Directory.CreateDirectory(FontFilter.App.DataDirectory);File.AppendAllText(Path.Combine(FontFilter.App.DataDirectory,"startup-error.log"),DateTime.Now+" "+ex.GetType().Name+": "+ex.Message+Environment.NewLine);return 1;}
  finally{instance?.Dispose();}
 }
 public static AppBuilder BuildAvaloniaApp()=>AppBuilder.Configure<DesktopApp>().UsePlatformDetect().LogToTrace();
}
