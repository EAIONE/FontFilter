using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
namespace FontFilter.Desktop;
public sealed partial class MainWindow
{
 readonly CancellationTokenSource updateCancellation=new();PendingUpdate? pendingUpdate;bool updateCheckRunning;
 void ConfigureUpdates()
 {
  if(Program.Arguments.Any(a=>a.StartsWith("--verify",StringComparison.Ordinal))&&!Program.Arguments.Contains("--verify-stage-update"))return;
  Opened+=async(_,_)=>{if(!Settings.AutomaticUpdates)return;try{await Task.Delay(TimeSpan.FromSeconds(12),updateCancellation.Token);await CheckUpdatesSilently();}catch(OperationCanceledException){}};
  Closed+=(_,_)=>{updateCancellation.Cancel();if(pendingUpdate!=null)try{ScheduleUpdate(pendingUpdate);}catch(Exception ex){WriteUpdateError(ex);}updateCancellation.Dispose();};
 }
 async Task CheckUpdatesSilently()
 {
  if(updateCheckRunning||UpdatePolicy.Platform=="unsupported"||RuntimeInformation.ProcessArchitecture!=Architecture.X64)return;updateCheckRunning=true;
  try{
   var token = Environment.GetEnvironmentVariable("FONTFILTER_GITEE_TOKEN")?.Trim();
   if (string.IsNullOrEmpty(token))
    throw new IOException("请设置具有 Gitee 私有仓库读取权限的 FONTFILTER_GITEE_TOKEN。");
   using var http = new HttpClient(new GiteeUpdateTransport(token)) { Timeout = Timeout.InfiniteTimeSpan };
   var pending = await new UpdateClient(http).PrepareAsync(Path.Combine(FontFilter.App.DataDirectory, "Updates"), null,
       UpdatePolicy.CurrentVersion, UpdatePolicy.Platform, updateCancellation.Token);
   if(updateCancellation.IsCancellationRequested)return;pendingUpdate=pending;if(pending!=null)status.Text="新版已准备好，下次打开自动使用";
  }catch(OperationCanceledException){}catch(Exception ex){WriteUpdateError(ex);}finally{updateCheckRunning=false;}
 }
 static void WriteUpdateError(Exception ex){try{File.WriteAllText(Path.Combine(FontFilter.App.DataDirectory,"update-check.txt"),DateTimeOffset.UtcNow+" "+ex.GetType().Name+": "+ex.Message);}catch{}}
 static void ScheduleUpdate(PendingUpdate pending)
 {
  UpdateTrust.Read(pending,UpdatePolicy.CurrentVersion,UpdatePolicy.Platform);
  var executable=OperatingSystem.IsWindows()?"FontFilter.Updater.exe":"FontFilter.Updater";var installed=Path.Combine(AppContext.BaseDirectory,executable);if(!File.Exists(installed))throw new FileNotFoundException("更新助手缺失",installed);
  var helperDirectory=Path.Combine(FontFilter.App.DataDirectory,"Updates","helper-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(helperDirectory);var helper=Path.Combine(helperDirectory,executable);File.Copy(installed,helper);
  if(!OperatingSystem.IsWindows())File.SetUnixFileMode(helper,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);
  var target=Path.GetFullPath(AppContext.BaseDirectory).TrimEnd(Path.DirectorySeparatorChar);
  if(OperatingSystem.IsMacOS()){var macos=new DirectoryInfo(target);if(macos.Name!="MacOS"||macos.Parent?.Name!="Contents"||macos.Parent.Parent?.Extension!=".app")throw new InvalidDataException("当前不是已安装的 Mac 应用");target=macos.Parent.Parent.FullName;}
  using var process=Process.GetCurrentProcess();var request=new UpdateInstallRequest(pending,target,FontFilter.App.DataDirectory,Environment.ProcessId,process.StartTime.ToUniversalTime().Ticks,UpdatePolicy.Platform);var requestPath=Path.Combine(helperDirectory,"install.json");File.WriteAllText(requestPath,JsonSerializer.Serialize(request));
  var start=new ProcessStartInfo(helper){UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=helperDirectory};start.ArgumentList.Add("--apply-update");start.ArgumentList.Add(requestPath);using var updater=Process.Start(start)??throw new IOException("更新助手无法启动");UpdateHandoff.Begin(FontFilter.App.DataDirectory,new UpdateProgress(updater.Id,updater.StartTime.ToUniversalTime().Ticks));
 }
 public async Task VerifyStagedUpdate(string folder)
 {
  var pending=new PendingUpdate(Path.Combine(folder,"update.json"),Path.Combine(folder,"update.sig"),Path.Combine(folder,"FontFilter-Windows-x64.zip"));UpdateTrust.Read(pending,UpdatePolicy.CurrentVersion,UpdatePolicy.Platform);
  pendingUpdate=pending;var path=Path.Combine(AppContext.BaseDirectory,"FontFilter.dll");var before=System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path));await Task.Delay(700);
  if(OwnedWindows.Any()||!before.SequenceEqual(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path))))throw new Exception("Update interrupted the active window");
  File.WriteAllText(Path.Combine(FontFilter.App.DataDirectory,"silent-before-close-pass.txt"),"Pending update did not replace files or open dialogs while app was in use.");Close();
 }
}
