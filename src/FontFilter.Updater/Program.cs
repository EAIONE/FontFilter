using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using FontFilter;
string? data=null;string? marker=null;string? restartTarget=null;
try{
 if(args.Length!=2||args[0]!="--apply-update")return 2;
 var request=JsonSerializer.Deserialize<UpdateInstallRequest>(File.ReadAllText(args[1]))??throw new InvalidDataException("更新请求缺失");
 data=Path.GetFullPath(request.DataDirectory);var target=Path.GetFullPath(request.TargetDirectory);var platform=UpdatePolicy.Platform;
 if(platform!=request.Platform||platform=="unsupported"||!target.Equals(target.TrimEnd(Path.DirectorySeparatorChar),StringComparison.OrdinalIgnoreCase)||data.StartsWith(target+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("更新路径或平台无效");
 var bin=platform=="win-x64"?target:Path.Combine(target,"Contents","MacOS");if(!File.Exists(Path.Combine(bin,"FontFilter.dll")))throw new InvalidDataException("目标不是字里行间程序目录");
 restartTarget=target;Directory.CreateDirectory(data);marker=Path.Combine(data,"update-in-progress");using(var own=Process.GetCurrentProcess())UpdateHandoff.Begin(data,new UpdateProgress(own.Id,own.StartTime.ToUniversalTime().Ticks));
 using(var parent=GetParent(request)){if(parent!=null&&!parent.WaitForExit(120000))throw new TimeoutException("程序仍在使用，保留当前版本");}
 IDisposable? lease=null;var deadline=DateTime.UtcNow.AddSeconds(30);
 while(lease==null){try{lease=AppInstance.Acquire(data);}catch(IOException){if(DateTime.UtcNow>deadline)throw new TimeoutException("程序重新打开，保留当前版本");Thread.Sleep(250);}}
 using(lease){
  var (feed,asset)=UpdateTrust.Read(request.Pending,UpdatePolicy.CurrentVersion,platform);
  if(AssemblyName.GetAssemblyName(Path.Combine(bin,"FontFilter.dll")).Version>=Version.Parse(feed.Version))return 0;
  var stage=Path.Combine(Path.GetDirectoryName(target)!,".fontfilter-update-"+Guid.NewGuid().ToString("N"));var candidate=UpdatePackage.Extract(request.Pending.PackagePath,stage,platform);var candidateBin=platform=="win-x64"?candidate:Path.Combine(candidate,"Contents","MacOS");
  if(AssemblyName.GetAssemblyName(Path.Combine(candidateBin,"FontFilter.dll")).Version?.ToString(3)!=feed.Version)throw new InvalidDataException("程序版本与签名信息不一致");
  if(OperatingSystem.IsMacOS()){
   var check=new ProcessStartInfo("/usr/bin/codesign"){UseShellExecute=false,CreateNoWindow=true};foreach(var arg in new[]{"--verify","--deep","--strict",candidate})check.ArgumentList.Add(arg);using var codesign=Process.Start(check)??throw new IOException("签名检查无法启动");if(!codesign.WaitForExit(30000)||codesign.ExitCode!=0)throw new InvalidDataException("Mac 程序签名无效");
  }
  string? backup=null;var swapDeadline=DateTime.UtcNow.AddSeconds(30);
  while(backup==null){try{backup=UpdateTransaction.Swap(target,candidate);}catch(IOException){if(DateTime.UtcNow>swapDeadline)throw;Thread.Sleep(250);}}
  // Do not reopen after an intentional user exit. Next normal launch runs the new version.
  File.WriteAllText(Path.Combine(data,"last-update.json"),JsonSerializer.Serialize(new{Version=feed.Version,Backup=backup,InstalledUtc=DateTimeOffset.UtcNow}));
 }
 return 0;
}catch(Exception ex){if(data!=null)try{File.WriteAllText(Path.Combine(data,"update-error.txt"),DateTimeOffset.UtcNow+" "+ex.GetType().Name+": "+ex.Message);}catch{}return 1;}
finally{
 bool reopen=false;if(marker!=null&&data!=null)try{reopen=UpdateHandoff.Complete(data,Environment.ProcessId);}catch{}
 if(reopen&&restartTarget!=null)try{
  var start=new ProcessStartInfo{UseShellExecute=false,CreateNoWindow=true};
  if(OperatingSystem.IsMacOS()){start.FileName="/usr/bin/open";start.ArgumentList.Add(restartTarget);}else{start.FileName=Path.Combine(restartTarget,"FontFilter.exe");start.WorkingDirectory=restartTarget;}Process.Start(start)?.Dispose();
 }catch{}
}
static Process? GetParent(UpdateInstallRequest request){try{var p=Process.GetProcessById(request.ParentProcessId);if(p.StartTime.ToUniversalTime().Ticks==request.ParentStartTicks)return p;p.Dispose();return null;}catch(ArgumentException){return null;}}
