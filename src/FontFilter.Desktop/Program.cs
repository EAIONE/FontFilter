using Avalonia;
namespace FontFilter.Desktop;
public static class Program
{
 public static bool AlreadyRunning;
 public static string[] Arguments=Array.Empty<string>();
 [STAThread] public static int Main(string[] args)
 {
  Arguments=args;
  IDisposable? instance=null;
  try{try{instance=AppInstance.Acquire(FontFilter.App.DataDirectory);}catch(IOException){AlreadyRunning=true;}return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);}
  catch(Exception ex){Directory.CreateDirectory(FontFilter.App.DataDirectory);File.AppendAllText(Path.Combine(FontFilter.App.DataDirectory,"startup-error.log"),DateTime.Now+" "+ex.GetType().Name+": "+ex.Message+Environment.NewLine);return 1;}
  finally{instance?.Dispose();}
 }
 public static AppBuilder BuildAvaloniaApp()=>AppBuilder.Configure<DesktopApp>().UsePlatformDetect().LogToTrace();
}
