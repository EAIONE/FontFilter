using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FontFilter;
public static class UpdateTests
{
 public static void Run(Action<string,Action> test,Action<bool,string> check,string dir)
 {
  test("update feed authenticates original bytes and rejects tampering",()=>{
   using var key=ECDsa.Create(ECCurve.NamedCurves.nistP256);var data=Encoding.UTF8.GetBytes("{\"Version\":\"1.2.0\",\"Packages\":[]}");var signature=key.SignData(data,HashAlgorithmName.SHA256);
   check(UpdateTrust.Verify(data,signature,key.ExportSubjectPublicKeyInfoPem()).Version=="1.2.0","signed feed rejected");data[13]=(byte)'9';
   try{UpdateTrust.Verify(data,signature,key.ExportSubjectPublicKeyInfoPem());throw new Exception("tampered feed accepted");}catch(CryptographicException){}
  });
  test("update selection excludes old prerelease wrong architecture and wrong repository",()=>{
   var asset=new UpdateAsset("win-x64","FontFilter-Windows-x64.zip","https://github.com/EAIONE/FontFilter/releases/download/v1.2.0/FontFilter-Windows-x64.zip",new string('a',64),123);
   var feed=new UpdateManifest("1.2.0",new[]{asset});check(UpdatePolicy.Select(feed,"1.1.0","win-x64")!=null,"new version missing");
   check(UpdatePolicy.Select(feed,"1.2.0","win-x64")==null,"same version selected");check(UpdatePolicy.Select(feed,"2.0.0","win-x64")==null,"downgrade selected");
   check(UpdatePolicy.Select(feed,"1.1.0","osx-x64")==null,"wrong architecture selected");check(UpdatePolicy.Select(feed with{Version="1.2.0-beta"},"1.1.0","win-x64")==null,"prerelease selected");
   try{UpdatePolicy.Select(feed with{Packages=new[]{asset with{Url="https://github.com/other/repo/releases/download/v1.2.0/a.zip"}}},"1.1.0","win-x64");throw new Exception("untrusted origin accepted");}catch(InvalidDataException){}
  });
  test("update package hashes and length must both match",()=>{
   var p=Path.Combine(dir,"update-bytes.zip");File.WriteAllText(p,"package");var a=new UpdateAsset("win-x64","x","",Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))),7);UpdatePackage.Verify(p,a);
   try{UpdatePackage.Verify(p,a with{Size=8});throw new Exception("wrong length accepted");}catch(InvalidDataException){}
   try{UpdatePackage.Verify(p,a with{Sha256=new string('0',64)});throw new Exception("wrong hash accepted");}catch(InvalidDataException){}
  });
  test("update extraction rejects traversal and links",()=>{
   foreach(var name in new[]{"../outside.txt","FontFilter/../../outside.txt","FontFilter/evil"}){
    var zip=Path.Combine(dir,Guid.NewGuid()+".zip");using(var z=ZipFile.Open(zip,ZipArchiveMode.Create)){var e=z.CreateEntry(name);if(name.EndsWith("evil"))e.ExternalAttributes=unchecked((int)(0xA1FFu<<16));using var w=new StreamWriter(e.Open());w.Write("bad");}
    try{UpdatePackage.Extract(zip,Path.Combine(dir,Guid.NewGuid().ToString()),"win-x64");throw new Exception("unsafe archive accepted");}catch(InvalidDataException){}
   }
  });
  test("update swap preserves portable data and rollback restores old program",()=>{
   var root=Path.Combine(dir,"更新 中文 path");Directory.CreateDirectory(root);var target=Path.Combine(root,"FontFilter");var candidate=Path.Combine(root,"candidate");
   Directory.CreateDirectory(Path.Combine(target,"Data"));Directory.CreateDirectory(candidate);File.WriteAllText(Path.Combine(target,"FontFilter.dll"),"old");File.WriteAllText(Path.Combine(target,"Data","settings.json"),"saved");File.WriteAllText(Path.Combine(candidate,"FontFilter.dll"),"new");
   var backup=UpdateTransaction.Swap(target,candidate);check(File.ReadAllText(Path.Combine(target,"Data","settings.json"))=="saved","portable settings lost");check(File.ReadAllText(Path.Combine(target,"FontFilter.dll"))=="new","program not replaced");
   UpdateTransaction.Rollback(target,backup);check(File.ReadAllText(Path.Combine(target,"FontFilter.dll"))=="old","rollback failed");
  });
  test("update download falls back from mirror and stages only authenticated bytes",()=>{
   using var key=ECDsa.Create(ECCurve.NamedCurves.nistP256);var payload=Encoding.UTF8.GetBytes("package");var asset=new UpdateAsset("win-x64","FontFilter-Windows-x64.zip","https://github.com/EAIONE/FontFilter/releases/download/v1.2.0/FontFilter-Windows-x64.zip",Convert.ToHexString(SHA256.HashData(payload)),payload.Length);
   var bytes=JsonSerializer.SerializeToUtf8Bytes(new UpdateManifest("1.2.0",new[]{asset}));var signature=Encoding.UTF8.GetBytes(Convert.ToBase64String(key.SignData(bytes,HashAlgorithmName.SHA256)));int mirrors=0;
   using var client=new HttpClient(new ApiTestHandler((req,ct)=>{if(req.RequestUri!.Host=="mirror.example"){mirrors++;return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.BadGateway));}var body=req.RequestUri.AbsolutePath.EndsWith("update.json")?bytes:req.RequestUri.AbsolutePath.EndsWith("update.sig")?signature:payload;return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK){Content=new ByteArrayContent(body)});}));
   var pending=new UpdateClient(client,key.ExportSubjectPublicKeyInfoPem()).PrepareAsync(Path.Combine(dir,"download"),"https://mirror.example/","1.1.0","win-x64",CancellationToken.None).GetAwaiter().GetResult();
   check(pending!=null&&File.ReadAllBytes(pending.PackagePath).SequenceEqual(payload)&&mirrors>=2,"fallback/download failed");
   using var cancelled=new CancellationTokenSource();cancelled.Cancel();try{new UpdateClient(client,key.ExportSubjectPublicKeyInfoPem()).PrepareAsync(Path.Combine(dir,"cancelled"),null,"1.1.0","win-x64",cancelled.Token).GetAwaiter().GetResult();throw new Exception("cancel ignored");}catch(OperationCanceledException){}
  });
  test("launch during update exits old code and requests a fresh launch",()=>{
   var data=Path.Combine(dir,"handoff");Directory.CreateDirectory(data);using var process=System.Diagnostics.Process.GetCurrentProcess();
   File.WriteAllText(Path.Combine(data,"update-in-progress"),JsonSerializer.Serialize(new UpdateProgress(process.Id,process.StartTime.ToUniversalTime().Ticks)));
   check(UpdateHandoff.DeferIfUpdating(data)&&File.Exists(Path.Combine(data,"update-reopen-request")),"launch continued old code");
   File.WriteAllText(Path.Combine(data,"update-in-progress"),JsonSerializer.Serialize(new UpdateProgress(process.Id,0)));check(!UpdateHandoff.DeferIfUpdating(data),"stale marker blocked startup");
  });
  test("update completion and launch request cannot lose the requested launch",()=>{
   var data=Path.Combine(dir,"handoff-race");using var process=System.Diagnostics.Process.GetCurrentProcess();
   for(int i=0;i<100;i++){
    UpdateHandoff.Begin(data,new UpdateProgress(process.Id,process.StartTime.ToUniversalTime().Ticks));
    var requested=Task.Run(()=>UpdateHandoff.DeferIfUpdating(data));var completion=Task.Run(()=>UpdateHandoff.Complete(data,process.Id));Task.WaitAll(requested,completion);
    check(!requested.Result||completion.Result,"launch request lost to completing helper");
   }
   var assembly=typeof(UpdatePolicy).Assembly.Location;check(UpdateHandoff.NeedsFreshLaunch(assembly,"0.0.0"),"replaced assembly would run loaded old code");check(!UpdateHandoff.NeedsFreshLaunch(assembly,UpdatePolicy.CurrentVersion),"current assembly would relaunch forever");
  });
 }
}
