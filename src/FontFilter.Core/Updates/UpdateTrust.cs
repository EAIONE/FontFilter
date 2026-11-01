using System.Security.Cryptography;
using System.Text.Json;
namespace FontFilter;
public static class UpdateTrust
{
 public static string PublicKey{get{using var stream=typeof(UpdateTrust).Assembly.GetManifestResourceStream("FontFilter.UpdatePublicKey")??throw new InvalidOperationException("更新公钥缺失");using var reader=new StreamReader(stream);return reader.ReadToEnd();}}
 public static UpdateManifest Verify(byte[] bytes,byte[] signature,string? publicKey=null)
 {
  if(bytes.Length>65536||signature.Length!=64)throw new CryptographicException("更新签名无效");
  using var key=ECDsa.Create();key.ImportFromPem(publicKey??PublicKey);
  if(!key.VerifyData(bytes,signature,HashAlgorithmName.SHA256))throw new CryptographicException("更新签名无效");
  return JsonSerializer.Deserialize<UpdateManifest>(bytes)??throw new InvalidDataException("更新信息为空");
 }
 public static (UpdateManifest Feed,UpdateAsset Asset) Read(PendingUpdate pending,string current,string platform)
 {
  var feed=Verify(File.ReadAllBytes(pending.ManifestPath),Convert.FromBase64String(File.ReadAllText(pending.SignaturePath).Trim()));
  var asset=UpdatePolicy.Select(feed,current,platform)??throw new InvalidDataException("没有适用的新版");UpdatePackage.Verify(pending.PackagePath,asset);return(feed,asset);
 }
}
