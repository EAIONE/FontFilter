using System.Text.RegularExpressions;
namespace FontFilter;
public sealed record UpdateAsset(string Platform,string FileName,string Url,string Sha256,long Size);
public sealed record UpdateManifest(string Version,UpdateAsset[] Packages);
public sealed record PendingUpdate(string ManifestPath,string SignaturePath,string PackagePath);
public sealed record UpdateInstallRequest(PendingUpdate Pending,string TargetDirectory,string DataDirectory,int ParentProcessId,long ParentStartTicks,string Platform);
public static class UpdatePolicy
{
 public static string CurrentVersion=>typeof(UpdatePolicy).Assembly.GetName().Version?.ToString(3)??"1.1.0";
 public const string FeedUrl="https://github.com/EAIONE/FontFilter/releases/latest/download/update.json";
 public const string DefaultProxy="https://gh-proxy.org/";
 public static string Platform=>OperatingSystem.IsWindows()?"win-x64":OperatingSystem.IsMacOS()?"osx-x64":"unsupported";
 public static bool IsVersion(string? version)=>version!=null&&Regex.IsMatch(version,@"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$")&&Version.TryParse(version,out _);
 public static UpdateAsset? Select(UpdateManifest feed,string current,string platform)
 {
  if(!IsVersion(feed.Version)||!IsVersion(current)||Version.Parse(feed.Version)<=Version.Parse(current))return null;
  var matches=(feed.Packages??Array.Empty<UpdateAsset>()).Where(a=>a.Platform==platform).ToArray();if(matches.Length!=1)return null;
  var a=matches[0];var expected=platform=="win-x64"?"FontFilter-Windows-x64.zip":platform=="osx-x64"?"FontFilter-macOS-Intel.zip":"";
  var prefix="https://github.com/EAIONE/FontFilter/releases/download/";
  if(expected.Length==0||a.FileName!=expected||!Uri.TryCreate(a.Url,UriKind.Absolute,out var uri)||uri.UserInfo.Length>0||uri.Query.Length>0||uri.Fragment.Length>0||!a.Url.StartsWith(prefix,StringComparison.Ordinal)||!a.Url.EndsWith("/"+expected,StringComparison.Ordinal)||uri.AbsolutePath.Split('/').Length!=7||!Regex.IsMatch(a.Sha256??"",@"\A[0-9a-fA-F]{64}\z")||a.Size<=0||a.Size>512L*1024*1024)throw new InvalidDataException("更新信息格式不正确");
  return a;
 }
 public static IEnumerable<Uri> Routes(string url,string? proxy)
 {
  if(!string.IsNullOrWhiteSpace(proxy)&&Uri.TryCreate(proxy,UriKind.Absolute,out var p)&&p.Scheme=="https"&&p.UserInfo.Length==0&&p.Query.Length==0&&p.Fragment.Length==0)yield return new Uri(p.AbsoluteUri.TrimEnd('/')+"/"+url);
  yield return new Uri(url);
 }
}
