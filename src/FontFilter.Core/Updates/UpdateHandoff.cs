using System.Diagnostics;
using System.Text.Json;
namespace FontFilter;
public sealed record UpdateProgress(int ProcessId,long StartTicks);
public static class UpdateHandoff
{
 static FileStream Gate(string directory){Directory.CreateDirectory(directory);var until=DateTime.UtcNow.AddSeconds(3);while(true){try{return new FileStream(Path.Combine(directory,"update-handoff.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}catch(IOException){if(DateTime.UtcNow>until)throw;Thread.Sleep(10);}}}
 public static void Begin(string directory,UpdateProgress progress){using var gate=Gate(directory);File.WriteAllText(Path.Combine(directory,"update-in-progress"),JsonSerializer.Serialize(progress));}
 public static bool Complete(string directory,int processId)
 {
  using var gate=Gate(directory);var marker=Path.Combine(directory,"update-in-progress");
  var owner=JsonSerializer.Deserialize<UpdateProgress>(File.ReadAllText(marker));if(owner?.ProcessId!=processId)return false;File.Delete(marker);
  var request=Path.Combine(directory,"update-reopen-request");if(!File.Exists(request))return false;File.Delete(request);return true;
 }
 public static bool NeedsFreshLaunch(string assemblyPath,string loadedVersion){try{return System.Reflection.AssemblyName.GetAssemblyName(assemblyPath).Version?.ToString(3)!=loadedVersion;}catch(IOException){return false;}}
 public static bool DeferIfUpdating(string dataDirectory)
 {
  try{
   using var gate=Gate(dataDirectory);
   var progress=JsonSerializer.Deserialize<UpdateProgress>(File.ReadAllText(Path.Combine(dataDirectory,"update-in-progress")));if(progress==null)return false;
   using var updater=Process.GetProcessById(progress.ProcessId);if(updater.HasExited||updater.StartTime.ToUniversalTime().Ticks!=progress.StartTicks)return false;
   File.WriteAllText(Path.Combine(dataDirectory,"update-reopen-request"),DateTimeOffset.UtcNow.ToString("O"));return true;
  }catch(Exception ex)when(ex is IOException or ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception or JsonException or UnauthorizedAccessException){return false;}
 }
}
