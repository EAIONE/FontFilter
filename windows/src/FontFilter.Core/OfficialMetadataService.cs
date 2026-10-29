using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
namespace FontFilter;
public sealed record MetadataResult(string Summary,string Url);
public static class OfficialMetadataService
{
 static readonly string[] Domains={"foundertype.com","hanyi.com.cn","hanyi.com"};
 static readonly HttpClient Client=new(new HttpClientHandler{AllowAutoRedirect=false,AutomaticDecompression=DecompressionMethods.All}){Timeout=TimeSpan.FromSeconds(20)};
 public static bool IsAllowed(Uri u)=>u.IsAbsoluteUri&&u.Scheme=="https"&&u.IsDefaultPort&&u.UserInfo.Length==0&&Domains.Any(d=>u.Host.Equals(d,StringComparison.OrdinalIgnoreCase)||u.Host.EndsWith("."+d,StringComparison.OrdinalIgnoreCase));
 static string Replace(string s,string pattern,string value)=>Regex.Replace(s,pattern,value,RegexOptions.IgnoreCase|RegexOptions.Singleline,TimeSpan.FromSeconds(1));
 public static string ExtractHtml(string html)
 {
  var s=Replace(html,@"<(script|style|nav|header|footer)\b[^>]*>.*?</\1>"," ");
  var main=Regex.Match(s,@"<main\b[^>]*>(.*?)</main>",RegexOptions.IgnoreCase|RegexOptions.Singleline,TimeSpan.FromSeconds(1));if(main.Success)s=main.Groups[1].Value;
  s=Replace(s,@"<[^>]+>"," ");s=WebUtility.HtmlDecode(s);return Regex.Replace(s,@"\s+"," ").Trim();
 }
 static async Task<byte[]> ReadBodyAsync(HttpResponseMessage response,CancellationToken ct){await using var input=await response.Content.ReadAsStreamAsync(ct);using var output=new MemoryStream();var buffer=new byte[16384];int n;while((n=await input.ReadAsync(buffer,ct))>0){if(output.Length+n>2_000_000)throw new IOException("页面超过2MB，请改用粘贴介绍。");output.Write(buffer,0,n);}return output.ToArray();}
 public static async Task<byte[]> DownloadAsync(HttpClient client,Uri uri,TimeSpan timeout,CancellationToken ct){using var bounded=CancellationTokenSource.CreateLinkedTokenSource(ct);bounded.CancelAfter(timeout);using var response=await client.GetAsync(uri,HttpCompletionOption.ResponseHeadersRead,bounded.Token);response.EnsureSuccessStatusCode();return await ReadBodyAsync(response,bounded.Token);}
 public static async Task<MetadataResult> FetchAsync(Uri uri,CancellationToken ct)
 {
  using var bounded=CancellationTokenSource.CreateLinkedTokenSource(ct);bounded.CancelAfter(TimeSpan.FromSeconds(20));ct=bounded.Token;
  for(int redirects=0;redirects<4;redirects++){
   if(!IsAllowed(uri))throw new InvalidOperationException("仅支持方正、汉仪官方HTTPS页面。");
   using var request=new HttpRequestMessage(HttpMethod.Get,uri);request.Headers.UserAgent.ParseAdd("FontFilter/0.1 (personal font metadata viewer)");
   using var response=await Client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,ct);
   if((int)response.StatusCode>=300&&(int)response.StatusCode<400){uri=new Uri(uri,response.Headers.Location??throw new IOException("网站重定向没有目标。"));continue;}
   response.EnsureSuccessStatusCode();var media=response.Content.Headers.ContentType?.MediaType??"";if(!media.Contains("html",StringComparison.OrdinalIgnoreCase))throw new IOException("该地址不是HTML介绍页面。");
   var bytes=await ReadBodyAsync(response,ct);
   Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);var charset=response.Content.Headers.ContentType?.CharSet?.Trim('"')??"utf-8";Encoding encoding;try{encoding=Encoding.GetEncoding(charset);}catch{encoding=Encoding.UTF8;}
   var html=encoding.GetString(bytes);var metas=Regex.Matches(html,@"<meta\b[^>]*>",RegexOptions.IgnoreCase,TimeSpan.FromSeconds(1));string? summary=null;
   foreach(Match m in metas){if(!Regex.IsMatch(m.Value,@"(?:name|property)\s*=\s*['"" ](?:description|og:description)['"" ]",RegexOptions.IgnoreCase))continue;var value=Regex.Match(m.Value,@"content\s*=\s*(['""])(.*?)\1",RegexOptions.IgnoreCase|RegexOptions.Singleline,TimeSpan.FromSeconds(1));if(value.Success){summary=ExtractHtml(value.Groups[2].Value);break;}}
   summary=string.IsNullOrWhiteSpace(summary)?ExtractHtml(html):summary;if(summary.Length<15)throw new IOException("页面没有可提取介绍，可能需要动态加载；请手动粘贴。");
   return new MetadataResult(summary[..Math.Min(summary.Length,1600)],uri.AbsoluteUri);
  }
  throw new IOException("重定向次数过多。");
 }
}
