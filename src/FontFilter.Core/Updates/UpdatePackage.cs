using System.IO.Compression;
using System.Security.Cryptography;
namespace FontFilter;
public static class UpdatePackage
{
 public static void Verify(string path,UpdateAsset asset)
 {
  using var file=File.OpenRead(path);if(file.Length!=asset.Size||!Convert.ToHexString(SHA256.HashData(file)).Equals(asset.Sha256,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("更新包校验失败");
 }
 public static string Extract(string archive,string destination,string platform)
 {
  var root=Path.GetFullPath(destination);Directory.CreateDirectory(root);var prefix=root+Path.DirectorySeparatorChar;
  using var zip=ZipFile.OpenRead(archive);if(zip.Entries.Count>3000||zip.Entries.Sum(e=>e.Length)>1536L*1024*1024)throw new InvalidDataException("更新包过大");
  var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  foreach(var entry in zip.Entries){
   var name=entry.FullName;if(name.Contains('\\')||name.Contains(':')||name.StartsWith('/')||name.Split('/').Any(p=>p is ".." or ".")||((entry.ExternalAttributes>>16)&0xF000)==0xA000)throw new InvalidDataException("更新包含非法路径");
   var path=Path.GetFullPath(Path.Combine(root,name));if(!path.StartsWith(prefix,StringComparison.OrdinalIgnoreCase)||!seen.Add(path))throw new InvalidDataException("更新包路径重复或越界");
   if(name.EndsWith('/')){Directory.CreateDirectory(path);continue;}
   Directory.CreateDirectory(Path.GetDirectoryName(path)!);entry.ExtractToFile(path,false);
   if(!OperatingSystem.IsWindows()){var mode=(UnixFileMode)((entry.ExternalAttributes>>16)&0x1FF);File.SetUnixFileMode(path,mode|UnixFileMode.UserRead|UnixFileMode.UserWrite);}
  }
  var app=Path.Combine(root,platform=="win-x64"?"FontFilter":"字里行间.app");var bin=platform=="win-x64"?app:Path.Combine(app,"Contents","MacOS");
  foreach(var name in new[]{"FontFilter.dll","FontFilter.Core.dll",platform=="win-x64"?"FontFilter.exe":"FontFilter",platform=="win-x64"?"FontFilter.Updater.exe":"FontFilter.Updater"})if(!File.Exists(Path.Combine(bin,name)))throw new InvalidDataException("更新包缺少程序文件");
  return app;
 }
}
