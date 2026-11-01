using System.Net.Http;
using System.Text;
namespace FontFilter;
public sealed class UpdateClient(HttpClient http,string? publicKey=null)
{
 public async Task<PendingUpdate?> PrepareAsync(string directory,string? proxy,string current,string platform,CancellationToken cancellation)
 {
  cancellation.ThrowIfCancellationRequested();byte[]? manifest=null;byte[]? signature=null;UpdateManifest? feed=null;Exception? failure=null;
  foreach(var route in UpdatePolicy.Routes(UpdatePolicy.FeedUrl,proxy)){
   try{
    manifest=await Small(route,65536,cancellation);var signatureText=await Small(new Uri(route.AbsoluteUri[..^"update.json".Length]+"update.sig"),4096,cancellation);signature=Convert.FromBase64String(Encoding.UTF8.GetString(signatureText).Trim());feed=UpdateTrust.Verify(manifest,signature,publicKey);break;
   }catch(OperationCanceledException)when(cancellation.IsCancellationRequested){throw;}catch(Exception ex)when(ex is HttpRequestException or IOException or FormatException or System.Security.Cryptography.CryptographicException or System.Text.Json.JsonException or OperationCanceledException){failure=ex;}
  }
  if(feed==null)throw new IOException("暂时无法检查更新",failure);
  var asset=UpdatePolicy.Select(feed,current,platform);if(asset==null)return null;
  var versionDirectory=Path.Combine(directory,feed.Version);Directory.CreateDirectory(versionDirectory);var zip=Path.Combine(versionDirectory,asset.FileName);
  bool valid=false;try{UpdatePackage.Verify(zip,asset);valid=true;}catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){}
  if(!valid){
   failure=null;bool downloaded=false;
   foreach(var route in UpdatePolicy.Routes(asset.Url,proxy)){
    try{
     using var timeout=CancellationTokenSource.CreateLinkedTokenSource(cancellation);timeout.CancelAfter(TimeSpan.FromMinutes(20));using var request=Request(route);using var response=await http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,timeout.Token);response.EnsureSuccessStatusCode();
     if(response.Content.Headers.ContentLength is long length&&length!=asset.Size)throw new InvalidDataException("更新包长度不匹配");
     var temp=zip+".part";await using(var source=await response.Content.ReadAsStreamAsync(timeout.Token))await using(var output=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None,81920,true)){
      var buffer=new byte[81920];long total=0;int read;while((read=await source.ReadAsync(buffer,timeout.Token))!=0){total+=read;if(total>asset.Size)throw new InvalidDataException("更新包过大");await output.WriteAsync(buffer.AsMemory(0,read),timeout.Token);}
     }
     UpdatePackage.Verify(temp,asset);File.Move(temp,zip,true);downloaded=true;break;
    }catch(OperationCanceledException)when(cancellation.IsCancellationRequested){throw;}catch(Exception ex)when(ex is HttpRequestException or IOException or OperationCanceledException){failure=ex;}
   }
   if(!downloaded)throw new IOException("暂时无法下载更新",failure);
  }
  cancellation.ThrowIfCancellationRequested();var manifestPath=Path.Combine(versionDirectory,"update.json");var signaturePath=Path.Combine(versionDirectory,"update.sig");
  SettingsStore.AtomicWrite(manifestPath,Encoding.UTF8.GetString(manifest!));SettingsStore.AtomicWrite(signaturePath,Convert.ToBase64String(signature!));return new PendingUpdate(manifestPath,signaturePath,zip);
 }
 static HttpRequestMessage Request(Uri uri){var request=new HttpRequestMessage(HttpMethod.Get,uri);request.Headers.UserAgent.ParseAdd("FontFilter/"+UpdatePolicy.CurrentVersion);return request;}
 async Task<byte[]> Small(Uri uri,int limit,CancellationToken cancellation)
 {
  using var timeout=CancellationTokenSource.CreateLinkedTokenSource(cancellation);timeout.CancelAfter(TimeSpan.FromSeconds(12));using var request=Request(uri);using var response=await http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,timeout.Token);response.EnsureSuccessStatusCode();
  await using var source=await response.Content.ReadAsStreamAsync(timeout.Token);using var bytes=new MemoryStream();var buffer=new byte[4096];int read;while((read=await source.ReadAsync(buffer,timeout.Token))!=0){if(bytes.Length+read>limit)throw new InvalidDataException("更新信息过大");bytes.Write(buffer,0,read);}return bytes.ToArray();
 }
}
