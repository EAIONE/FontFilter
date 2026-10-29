using FontFilter;
public static class ImportTests
{
 public static void Run(Action<string,Action> test,Action<bool,string> check,string root)
 {
  test("closed WAL database imports without sidecar files",()=>{var data=Path.Combine(root,"closed-wal");Directory.CreateDirectory(data);using(var old=new FontStore(Path.Combine(data,"fonts.db"))){old.Upsert(new("old","离线字体","Regular","old",""));old.SetManual(new[]{"old"},"古籍");}File.Delete(Path.Combine(data,"fonts.db-wal"));File.Delete(Path.Combine(data,"fonts.db-shm"));using var target=new FontStore(Path.Combine(root,"closed-target.db"));target.Upsert(new("new","离线字体","Regular","new",""));var result=LegacyImport.Import(data,target,Path.Combine(root,"closed-history"));check(result.Linked==1&&target.All().Single().ManualTags=="古籍","closed WAL import failed");});
  test("database snapshots support relative paths below Unicode working directory",()=>{using var source=new FontStore(Path.Combine(root,"relative-source.db"));source.Upsert(new("1","快照字体","Regular","x",""));var destination=Path.Combine(root,"相对路径快照.db");source.Backup(Path.GetRelativePath(Environment.CurrentDirectory,destination));using var copy=new FontStore(destination);check(copy.All().Single().Family=="快照字体","relative snapshot lost data");});
  test("legacy import reads WAL and associates unique family/style",()=>{
   var data=Path.Combine(root,"old");Directory.CreateDirectory(data);
   using var old=new FontStore(Path.Combine(data,"fonts.db"));old.Upsert(new("old","跨平台字体","Regular","file:///C:/Fonts/a.ttf",""));old.SetManual(new[]{"old"},"古籍");old.SetFavorite(new[]{"跨平台字体"},true);old.SaveMetadata("old","手工整理介绍，中宫宽阔","");
   using var target=new FontStore(Path.Combine(root,"target.db"));target.Upsert(new("new","跨平台字体","Regular","file:///Library/Fonts/a.ttf",""));target.SetManual(new[]{"new"},"清雅");
   var report=LegacyImport.Import(data,target,Path.Combine(root,"imports"));var f=target.All().Single();
   check(report.Linked==1&&f.ManualTags.Contains("古籍")&&f.ManualTags.Contains("清雅")&&f.IsFavorite&&f.Summary=="手工整理介绍，中宫宽阔","data lost or wrong match");
   check(f.AutoTags.Contains("中宫宽松"),"imported summary rules not refreshed");
   check(File.Exists(report.BackupPath),"missing destination backup");
  });
  test("ambiguous legacy records remain unresolved and survive another import",()=>{
   var data=Path.Combine(root,"ambiguous");Directory.CreateDirectory(data);
   using var old=new FontStore(Path.Combine(data,"fonts.db"));old.Upsert(new("old","同名字体","Regular","C:/a.ttf",""));old.SetManual(new[]{"old"},"古籍");
   using var target=new FontStore(Path.Combine(root,"ambiguous-target.db"));target.Upsert(new("1","同名字体","Regular","/a.ttf",""));target.Upsert(new("2","同名字体","Regular","/b.ttf",""));
   var report=LegacyImport.Import(data,target,Path.Combine(root,"imports"));check(report.Unresolved==1&&target.All().All(f=>f.ManualTags==""),"ambiguous name linked");check(System.Text.Json.JsonSerializer.Deserialize<List<FontRecord>>(File.ReadAllText(report.UnresolvedPath))!.Single().ManualTags=="古籍","unresolved annotations lost");
  });
  test("legacy import preserves nonempty metadata and rejects corrupt source",()=>{
   var data=Path.Combine(root,"invalid");Directory.CreateDirectory(data);File.WriteAllText(Path.Combine(data,"fonts.db"),"not a database");using var target=new FontStore(Path.Combine(root,"invalid-target.db"));target.Upsert(new("1","保留","Regular","a",""));target.SetManual(new[]{"1"},"人工");
   try{LegacyImport.Import(data,target,Path.Combine(root,"imports"));throw new Exception("invalid accepted");}catch(InvalidOperationException){}
   check(target.All().Single().ManualTags=="人工","corrupt source damaged current data");
  });
 }
}
